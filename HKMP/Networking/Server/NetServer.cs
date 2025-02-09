using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Hkmp.Api.Server;
using Hkmp.Api.Server.Networking;
using Hkmp.Logging;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using Hkmp.Networking.Packet.Update;

namespace Hkmp.Networking.Server;

/// <summary>
/// Server that manages connection with clients.
/// </summary>
internal class NetServer : INetServer {
    /// <summary>
    /// The time to throttle a client after they were rejected connection in milliseconds.
    /// </summary>
    private const int ThrottleTime = 2500;

    /// <summary>
    /// The packet manager instance.
    /// </summary>
    private readonly PacketManager _packetManager;
    
    /// <summary>
    /// Underlying DTLS server instance.
    /// </summary>
    private readonly DtlsServer _dtlsServer;

    /// <summary>
    /// Dictionary mapping IP end-points to net server clients.
    /// </summary>
    private readonly ConcurrentDictionary<IPEndPoint, NetServerClient> _clientsByEndPoint;

    /// <summary>
    /// Dictionary mapping client IDs to net server clients.
    /// </summary>
    private readonly ConcurrentDictionary<ushort, NetServerClient> _clientsById;

    /// <summary>
    /// Dictionary for the IP addresses of clients that have their connection throttled mapped to a stopwatch
    /// that keeps track of their last connection attempt. The client may use different local ports to establish
    /// connection so we only register the address and not the port as with established clients.
    /// </summary>
    private readonly ConcurrentDictionary<IPAddress, Stopwatch> _throttledClients;

    private readonly ConcurrentQueue<ReceivedData> _receivedQueue;

    /// <summary>
    /// Byte array containing leftover data that was not processed as a packet yet.
    /// </summary>
    private byte[] _leftoverData;

    /// <summary>
    /// Cancellation token source for all threads of the server.
    /// </summary>
    private CancellationTokenSource _taskTokenSource;

    /// <summary>
    /// Wait handle for inter-thread signalling when new data is ready to be processed.
    /// </summary>
    private AutoResetEvent _processingWaitHandle;

    /// <summary>
    /// Event that is called when a client times out.
    /// </summary>
    public event Action<ushort> ClientTimeoutEvent;

    /// <summary>
    /// Event that is called when the server shuts down.
    /// </summary>
    public event Action ShutdownEvent;

    /// <summary>
    /// Event that is called when a new client wants to connect.
    /// </summary>
    public event Action<NetServerClient, ClientInfo, ServerInfo> ConnectionRequestEvent;

    /// <inheritdoc />
    public bool IsStarted { get; private set; }

    public NetServer(PacketManager packetManager) {
        _packetManager = packetManager;

        _dtlsServer = new DtlsServer();

        _clientsByEndPoint = new ConcurrentDictionary<IPEndPoint, NetServerClient>();
        _clientsById = new ConcurrentDictionary<ushort, NetServerClient>();
        _throttledClients = new ConcurrentDictionary<IPAddress, Stopwatch>();

        _receivedQueue = new ConcurrentQueue<ReceivedData>();
    }

    /// <summary>
    /// Starts the server on the given port.
    /// </summary>
    /// <param name="port">The networking port.</param>
    public void Start(int port) {
        if (IsStarted) {
            Stop();
        }
        
        Logger.Info($"Starting NetServer on port {port}");
        IsStarted = true;
        
        _dtlsServer.Start(port);

        _processingWaitHandle = new AutoResetEvent(false);

        // Create a cancellation token source for the tasks that we are creating
        _taskTokenSource = new CancellationTokenSource();

        // Start a thread for handling the processing of received data
        new Thread(() => StartProcessing(_taskTokenSource.Token)).Start();

        // Start a thread for sending updates to clients
        new Thread(() => StartClientUpdates(_taskTokenSource.Token)).Start();

        _dtlsServer.DataReceivedEvent += (dtlsServerClient, buffer, length) => {
            _receivedQueue.Enqueue(new ReceivedData {
                DtlsServerClient = dtlsServerClient,
                Buffer = buffer,
                NumReceived = length
            });
            _processingWaitHandle.Set();
        };
    }

    /// <summary>
    /// Starts processing queued network data.
    /// </summary>
    /// <param name="token">The cancellation token for checking whether this task is requested to cancel.</param>
    private void StartProcessing(CancellationToken token) {
        while (!token.IsCancellationRequested) {
            _processingWaitHandle.WaitOne();

            while (_receivedQueue.TryDequeue(out var receivedData)) {
                var packets = PacketManager.HandleReceivedData(
                    receivedData.Buffer,
                    receivedData.NumReceived,
                    ref _leftoverData
                );

                var dtlsServerClient = receivedData.DtlsServerClient;
                var endPoint = dtlsServerClient.EndPoint;

                if (!_clientsByEndPoint.TryGetValue(endPoint, out var client)) {
                    // If the client is throttled, check their stopwatch for how long still
                    if (_throttledClients.TryGetValue(endPoint.Address, out var clientStopwatch)) {
                        if (clientStopwatch.ElapsedMilliseconds < ThrottleTime) {
                            // Reset stopwatch and ignore packets so the client times out
                            clientStopwatch.Restart();
                            continue;
                        }

                        // Stopwatch exceeds max throttle time so we remove the client from the dict
                        _throttledClients.TryRemove(endPoint.Address, out _);
                    }

                    Logger.Info(
                        $"Received packet from unknown client with address: {endPoint.Address}:{endPoint.Port}, creating new client");

                    // We didn't find a client with the given address, so we assume it is a new client
                    // that wants to connect
                    client = CreateNewClient(dtlsServerClient);
                }

                HandleClientPackets(client, packets);
            }
        }
    }

    /// <summary>
    /// Create a new client and start sending UDP updates and registering the timeout event.
    /// </summary>
    /// <param name="dtlsServerClient">The DTLS server client to create the client from.</param>
    /// <returns>A new net server client instance.</returns>
    private NetServerClient CreateNewClient(DtlsServerClient dtlsServerClient) {
        var netServerClient = new NetServerClient(dtlsServerClient.DtlsTransport, _packetManager, dtlsServerClient.EndPoint);
        
        netServerClient.ChunkSender.Start();

        netServerClient.ConnectionManager.ConnectionRequestEvent += OnConnectionRequest;
        netServerClient.ConnectionManager.ConnectionTimeoutEvent += () => HandleClientTimeout(netServerClient);
        netServerClient.ConnectionManager.StartAcceptingConnection();

        netServerClient.UpdateManager.TimeoutEvent += () => HandleClientTimeout(netServerClient);
        netServerClient.UpdateManager.StartUpdates();

        _clientsByEndPoint.TryAdd(dtlsServerClient.EndPoint, netServerClient);
        _clientsById.TryAdd(netServerClient.Id, netServerClient);

        return netServerClient;
    }

    /// <summary>
    /// Start updating clients with packets.
    /// </summary>
    /// <param name="token">The cancellation token for checking whether this task is requested to cancel.</param>
    private void StartClientUpdates(CancellationToken token) {
        while (!token.IsCancellationRequested) {
            foreach (var client in _clientsByEndPoint.Values) {
                client.UpdateManager.ProcessUpdate();
            }

            // TODO: figure out a good way to get rid of the sleep here
            // some way to signal when clients should be updated again would suffice
            // also see NetClient#Connect
            Thread.Sleep(5);
        }
    }

    /// <summary>
    /// Handles the event when a client times out. Disconnects the UDP client and cleans up any references
    /// to the client.
    /// </summary>
    /// <param name="client">The client that timed out.</param>
    private void HandleClientTimeout(NetServerClient client) {
        var id = client.Id;

        // Only execute the client timeout callback if the client is registered and thus has an ID
        if (client.IsRegistered) {
            ClientTimeoutEvent?.Invoke(id);
        }

        client.Disconnect();
        _dtlsServer.DisconnectClient(client.EndPoint);
        _clientsByEndPoint.TryRemove(client.EndPoint, out _);
        _clientsById.TryRemove(id, out _);

        Logger.Info($"Client {id} timed out");
    }

    /// <summary>
    /// Handle a list of packets from a registered client.
    /// </summary>
    /// <param name="client">The registered client.</param>
    /// <param name="packets">The list of packets to handle.</param>
    private void HandleClientPackets(NetServerClient client, List<Packet.Packet> packets) {
        var id = client.Id;

        foreach (var packet in packets) {
            // Create a server update packet from the raw packet instance
            var serverUpdatePacket = new ServerUpdatePacket();
            if (!serverUpdatePacket.ReadPacket(packet)) {
                // If ReadPacket returns false, we received a malformed packet
                if (client.IsRegistered) {
                    // Since the client is registered already, we simply ignore the packet
                    continue;
                }

                // If the client is not yet registered, we log the malformed packet, and throttle the client
                Logger.Debug($"Received malformed packet from client with IP: {client.EndPoint}");

                // We throttle the client, because chances are that they are using an outdated version of the
                // networking protocol, and keeping connection will potentially never time them out
                _throttledClients[client.EndPoint.Address] = Stopwatch.StartNew();

                continue;
            }

            client.UpdateManager.OnReceivePacket<ServerUpdatePacket, ServerUpdatePacketId>(serverUpdatePacket);

            // First process slice or slice ack data if it exists and pass it onto the chunk sender or chunk receiver
            var packetData = serverUpdatePacket.GetPacketData();
            if (packetData.TryGetValue(ServerUpdatePacketId.Slice, out var sliceData)) {
                packetData.Remove(ServerUpdatePacketId.Slice);
                client.ChunkReceiver.ProcessReceivedData((SliceData) sliceData);
            }

            if (packetData.TryGetValue(ServerUpdatePacketId.SliceAck, out var sliceAckData)) {
                packetData.Remove(ServerUpdatePacketId.SliceAck);
                client.ChunkSender.ProcessReceivedData((SliceAckData) sliceAckData);
            }
            
            // Then, if the client is registered, we let the packet manager handle the rest of the data
            if (client.IsRegistered) {
                // Let the packet manager handle the received data
                _packetManager.HandleServerUpdatePacket(id, serverUpdatePacket);
            }
        }
    }

    private void OnConnectionRequest(ushort clientId, ClientInfo clientInfo, ServerInfo serverInfo) {
        if (!_clientsById.TryGetValue(clientId, out var client)) {
            Logger.Error($"Connection request for client without known ID: {clientId}");
            serverInfo.ConnectionResult = ServerConnectionResult.RejectedOther;
            serverInfo.ConnectionRejectedMessage = "Unknown client";

            return;
        }
        
        // Invoke the connection request event ourselves first, then check the result
        ConnectionRequestEvent?.Invoke(client, clientInfo, serverInfo);

        if (serverInfo.ConnectionResult == ServerConnectionResult.Accepted) {
            Logger.Debug($"Connection request for client ID {clientId} was accepted, finishing connection sends, then registering client");

            client.ConnectionManager.FinishConnection(() => {
                Logger.Debug("Connection has finished sending data, registering client");
                
                client.IsRegistered = true;
                client.ConnectionManager.StopAcceptingConnection();
            });
        } else {
            Logger.Debug($"Connection request for client ID {clientId} was rejected, finishing connections sends, then throttling connection");

            client.ConnectionManager.FinishConnection(() => {
                Logger.Debug("Connection has finished sending data, disconnecting client and throttling");

                OnClientDisconnect(clientId);

                _throttledClients[client.EndPoint.Address] = Stopwatch.StartNew();
            });
        }
    }

    /// <summary>
    /// Stops the server and cleans up everything.
    /// </summary>
    public void Stop() {
        Logger.Info("Stopping NetServer");
        
        // Clean up existing clients
        foreach (var client in _clientsByEndPoint.Values) {
            client.Disconnect();
            _dtlsServer.DisconnectClient(client.EndPoint);
        }

        _clientsByEndPoint.Clear();
        _clientsById.Clear();
        _throttledClients.Clear();
        
        _dtlsServer.Stop();

        _leftoverData = null;

        IsStarted = false;

        // Request cancellation for the tasks that are still running
        _taskTokenSource.Cancel();

        _processingWaitHandle?.Dispose();

        // Invoke the shutdown event to notify all registered parties of the shutdown
        ShutdownEvent?.Invoke();
    }

    /// <summary>
    /// Callback method for when a client disconnects from the server.
    /// </summary>
    /// <param name="id">The ID of the client.</param>
    public void OnClientDisconnect(ushort id) {
        if (!_clientsById.TryGetValue(id, out var client)) {
            Logger.Warn($"Handling disconnect from ID {id}, but there's no matching client");
            return;
        }

        client.Disconnect();
        _dtlsServer.DisconnectClient(client.EndPoint);
        _clientsByEndPoint.TryRemove(client.EndPoint, out _);
        _clientsById.TryRemove(id, out _);

        Logger.Info($"Client {id} disconnected");
    }

    /// <summary>
    /// Get the update manager for the client with the given ID.
    /// </summary>
    /// <param name="id">The ID of the client.</param>
    /// <returns>The update manager for the client, or null if there does not exist a client with the
    /// given ID.</returns>
    public ServerUpdateManager GetUpdateManagerForClient(ushort id) {
        if (!_clientsById.TryGetValue(id, out var netServerClient)) {
            return null;
        }

        return netServerClient.UpdateManager;
    }

    /// <summary>
    /// Execute a given action for the update manager of all connected clients.
    /// </summary>
    /// <param name="dataAction">The action to execute with each update manager.</param>
    public void SetDataForAllClients(Action<ServerUpdateManager> dataAction) {
        foreach (var netServerClient in _clientsById.Values) {
            dataAction(netServerClient.UpdateManager);
        }
    }

    /// <inheritdoc />
    public IServerAddonNetworkSender<TPacketId> GetNetworkSender<TPacketId>(
        ServerAddon addon
    ) where TPacketId : Enum {
        if (addon == null) {
            throw new ArgumentException("Parameter 'addon' cannot be null");
        }

        // Check whether this addon has actually requested network access through their property
        // We check this otherwise an ID has not been assigned and it can't send network data
        if (!addon.NeedsNetwork) {
            throw new InvalidOperationException("Addon has not requested network access through property");
        }

        // Check whether there already is a network sender for the given addon
        if (addon.NetworkSender != null) {
            if (!(addon.NetworkSender is IServerAddonNetworkSender<TPacketId> addonNetworkSender)) {
                throw new InvalidOperationException(
                    "Cannot request network senders with differing generic parameters");
            }

            return addonNetworkSender;
        }

        // Otherwise create one, store it and return it
        var newAddonNetworkSender = new ServerAddonNetworkSender<TPacketId>(this, addon);
        addon.NetworkSender = newAddonNetworkSender;

        return newAddonNetworkSender;
    }

    /// <inheritdoc />
    public IServerAddonNetworkReceiver<TPacketId> GetNetworkReceiver<TPacketId>(
        ServerAddon addon,
        Func<TPacketId, IPacketData> packetInstantiator
    ) where TPacketId : Enum {
        if (addon == null) {
            throw new ArgumentException("Parameter 'addon' cannot be null");
        }

        if (packetInstantiator == null) {
            throw new ArgumentException("Parameter 'packetInstantiator' cannot be null");
        }

        // Check whether this addon has actually requested network access through their property
        // We check this otherwise an ID has not been assigned and it can't send network data
        if (!addon.NeedsNetwork) {
            throw new InvalidOperationException("Addon has not requested network access through property");
        }

        if (!addon.Id.HasValue) {
            throw new InvalidOperationException("Addon has no ID assigned");
        }

        ServerAddonNetworkReceiver<TPacketId> networkReceiver = null;

        // Check whether an existing network receiver exists
        if (addon.NetworkReceiver == null) {
            networkReceiver = new ServerAddonNetworkReceiver<TPacketId>(addon, _packetManager);
            addon.NetworkReceiver = networkReceiver;
        } else if (!(addon.NetworkReceiver is IServerAddonNetworkReceiver<TPacketId>)) {
            throw new InvalidOperationException(
                "Cannot request network receivers with differing generic parameters");
        }

        // After we know that this call did not use a different generic, we can update packet info
        ServerUpdatePacket.AddonPacketInfoDict[addon.Id.Value] = new AddonPacketInfo(
            // Transform the packet instantiator function from a TPacketId as parameter to byte
            networkReceiver?.TransformPacketInstantiator(packetInstantiator),
            (byte) Enum.GetValues(typeof(TPacketId)).Length
        );

        return addon.NetworkReceiver as IServerAddonNetworkReceiver<TPacketId>;
    }
}

/// <summary>
/// Data class for storing received data from a given IP end-point.
/// </summary>
internal class ReceivedData {
    public DtlsServerClient DtlsServerClient { get; init; }
    
    /// <summary>
    /// Byte array of the buffer containing received data.
    /// </summary>
    public byte[] Buffer { get; init; }
    
    /// <summary>
    /// The number of bytes in the buffer that were received. The rest of the buffer is empty.
    /// </summary>
    public int NumReceived { get; init; }
}
