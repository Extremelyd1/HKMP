using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Hkmp.Api.Server;
using Hkmp.Api.Server.Networking;
using Hkmp.Logging;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Server;

/// <summary>
/// Delegate for handling login requests.
/// </summary>
internal delegate bool LoginRequestHandler(
    ushort id,
    IPEndPoint ip,
    LoginRequest loginRequest,
    ServerUpdateManager updateManager
);

/// <summary>
/// Server that manages connection with clients.
/// </summary>
internal class NetServer : INetServer {
    /// <inheritdoc cref="UdpNetClient.MaxUdpPacketSize"/>
    private const int MaxUdpPacketSize = 65527;

    /// <summary>
    /// The time to throttle a client after they were rejected connection in milliseconds.
    /// </summary>
    private const int ThrottleTime = 2500;

    /// <summary>
    /// The packet manager instance.
    /// </summary>
    private readonly PacketManager _packetManager;

    /// <summary>
    /// Object to lock asynchronous access when dealing with clients.
    /// </summary>
    private readonly object _clientLock = new object();

    /// <summary>
    /// Dictionary mapping client IDs to net server clients.
    /// </summary>
    private readonly ConcurrentDictionary<ushort, NetServerClient> _registeredClients;

    /// <summary>
    /// Dictionary mapping IP end-points to net server clients for all clients.
    /// </summary>
    private readonly ConcurrentDictionary<IPEndPoint, NetServerClient> _clients;

    /// <summary>
    /// Dictionary for the IP addresses of clients that have their connection throttled mapped to a stopwatch
    /// that keeps track of their last connection attempt. The client may use different local ports to establish
    /// connection so we only register the address and not the port as with established clients.
    /// </summary>
    private readonly ConcurrentDictionary<IPAddress, Stopwatch> _throttledClients;

    /// <summary>
    /// The underlying UDP socket.
    /// </summary>
    private Socket _udpSocket;

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
    private ManualResetEventSlim _processingWaitHandle;

    /// <summary>
    /// Event that is called when a client times out.
    /// </summary>
    public event Action<ushort> ClientTimeoutEvent;

    /// <summary>
    /// Event that is called when the server shuts down.
    /// </summary>
    public event Action ShutdownEvent;

    // TODO: expose to API to allow addons to reject connections
    /// <summary>
    /// Event that is called when a new client wants to login.
    /// </summary>
    public event LoginRequestHandler LoginRequestEvent;

    /// <inheritdoc />
    public bool IsStarted { get; private set; }

    public NetServer(PacketManager packetManager) {
        _packetManager = packetManager;

        _registeredClients = new ConcurrentDictionary<ushort, NetServerClient>();
        _clients = new ConcurrentDictionary<IPEndPoint, NetServerClient>();
        _throttledClients = new ConcurrentDictionary<IPAddress, Stopwatch>();

        _receivedQueue = new ConcurrentQueue<ReceivedData>();
    }

    /// <summary>
    /// Starts the server on the given port.
    /// </summary>
    /// <param name="port">The networking port.</param>
    public void Start(int port) {
        Logger.Info($"Starting NetServer on port {port}");
        IsStarted = true;

        // Initialize the UDP socket
        _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        // Bind the socket to the given port and allow incoming packets on any address
        _udpSocket.Bind(new IPEndPoint(IPAddress.Any, port));

        _processingWaitHandle = new ManualResetEventSlim();

        // Create a cancellation token source for the tasks that we are creating
        _taskTokenSource = new CancellationTokenSource();

        // Start a thread for handling the processing of received data
        new Thread(() => StartProcessing(_taskTokenSource.Token)).Start();

        // Start a thread for sending updates to clients
        new Thread(() => StartClientUpdates(_taskTokenSource.Token)).Start();

        // Start a thread to receive network data from the socket
        new Thread(() => ReceiveData(_taskTokenSource.Token)).Start();
    }

    /// <summary>
    /// Continuously receive network UDP data and queue it for processing.
    /// </summary>
    /// <param name="token">The cancellation token for checking whether this method is requested to cancel.</param>
    private void ReceiveData(CancellationToken token) {
        // Take advantage of pre-pinned memory here using pinned object heap
        // var buffer = GC.AllocateArray<byte>(65527, true);
        // var bufferMem = buffer.AsMemory();

        EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);

        while (!token.IsCancellationRequested) {
            var buffer = new byte[MaxUdpPacketSize];

            try {
                // This will block until data is available
                _udpSocket.ReceiveFrom(
                    buffer,
                    SocketFlags.None,
                    ref endPoint
                );
            } catch (SocketException e) {
                Logger.Error($"UDP Socket exception:\n{e}");
            }

            _receivedQueue.Enqueue(new ReceivedData {
                Data = buffer,
                EndPoint = endPoint as IPEndPoint
            });
            _processingWaitHandle.Set();
        }
    }

    /// <summary>
    /// Starts processing queued network data.
    /// </summary>
    /// <param name="token">The cancellation token for checking whether this task is requested to cancel.</param>
    private void StartProcessing(CancellationToken token) {
        while (!token.IsCancellationRequested) {
            try {
                _processingWaitHandle.Wait(token);
            } catch (OperationCanceledException) {
                return;
            }

            _processingWaitHandle.Reset();

            while (_receivedQueue.TryDequeue(out var receivedData)) {
                var packets = PacketManager.HandleReceivedData(
                    receivedData.Data,
                    ref _leftoverData
                );

                var endPoint = receivedData.EndPoint;

                if (!_clients.TryGetValue(endPoint, out var client)) {
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
                    client = CreateNewClient(endPoint);

                    HandlePacketsUnregisteredClient(client, packets);
                } else {
                    HandlePacketsRegisteredClient(client, packets);
                }
            }
        }
    }

    /// <summary>
    /// Create a new client and start sending UDP updates and registering the timeout event.
    /// </summary>
    /// <param name="endPoint">The endpoint of the new client.</param>
    /// <returns>A new net server client instance.</returns>
    private NetServerClient CreateNewClient(IPEndPoint endPoint) {
        var netServerClient = new NetServerClient(_udpSocket, endPoint);
        netServerClient.UpdateManager.OnTimeout += () => HandleClientTimeout(netServerClient);
        netServerClient.UpdateManager.StartUpdates();

        _clients.TryAdd(endPoint, netServerClient);

        return netServerClient;
    }

    /// <summary>
    /// Start updating clients with packets.
    /// </summary>
    /// <param name="token">The cancellation token for checking whether this task is requested to cancel.</param>
    private void StartClientUpdates(CancellationToken token) {
        while (!token.IsCancellationRequested) {
            foreach (var client in _clients.Values) {
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
        _registeredClients.TryRemove(id, out _);
        _clients.TryRemove(client.EndPoint, out _);

        Logger.Info($"Client {id} timed out");
    }

    /// <summary>
    /// Handle a list of packets from a registered client.
    /// </summary>
    /// <param name="client">The registered client.</param>
    /// <param name="packets">The list of packets to handle.</param>
    private void HandlePacketsRegisteredClient(NetServerClient client, List<Packet.Packet> packets) {
        var id = client.Id;

        foreach (var packet in packets) {
            // Create a server update packet from the raw packet instance
            var serverUpdatePacket = new ServerUpdatePacket(packet);
            if (!serverUpdatePacket.ReadPacket()) {
                // If ReadPacket returns false, we received a malformed packet, which we simply ignore for now
                continue;
            }

            client.UpdateManager.OnReceivePacket<ServerUpdatePacket, ServerPacketId>(serverUpdatePacket);

            // Let the packet manager handle the received data
            _packetManager.HandleServerPacket(id, serverUpdatePacket);
        }
    }

    /// <summary>
    /// Handle a list of packets from an unregistered client.
    /// </summary>
    /// <param name="client">The unregistered client.</param>
    /// <param name="packets">The list of packets to handle.</param>
    private void HandlePacketsUnregisteredClient(NetServerClient client, List<Packet.Packet> packets) {
        for (var i = 0; i < packets.Count; i++) {
            var packet = packets[i];

            // Create a server update packet from the raw packet instance
            var serverUpdatePacket = new ServerUpdatePacket(packet);
            if (!serverUpdatePacket.ReadPacket()) {
                // If ReadPacket returns false, we received a malformed packet
                Logger.Debug($"Received malformed packet from client with IP: {client.EndPoint}");

                // We throttle the client, because chances are that they are using an outdated version of the
                // networking protocol, and keeping connection will potentially never time them out
                _throttledClients[client.EndPoint.Address] = Stopwatch.StartNew();

                continue;
            }

            client.UpdateManager.OnReceivePacket<ServerUpdatePacket, ServerPacketId>(serverUpdatePacket);

            if (!serverUpdatePacket.GetPacketData().TryGetValue(
                    ServerPacketId.LoginRequest,
                    out var packetData
                )) {
                continue;
            }

            var loginRequest = (LoginRequest) packetData;

            Logger.Info($"Received login request from '{loginRequest.Username}'");

            // Check if we actually have a login request handler
            if (LoginRequestEvent == null) {
                Logger.Error("Login request has no handler");
                return;
            }

            // Invoke the handler of the login request and decide what to do with the client based on the result
            var allowClient = LoginRequestEvent.Invoke(
                client.Id,
                client.EndPoint,
                loginRequest,
                client.UpdateManager
            );

            if (allowClient) {
                // Logger.Info($"Login request from '{loginRequest.Username}' approved");
                // client.UpdateManager.SetLoginResponseData(LoginResponseStatus.Success);

                // Register the client and add them to the dictionary
                client.IsRegistered = true;
                _registeredClients[client.Id] = client;

                // Now that the client is registered, we forward the rest of the packets to the other handler
                var leftoverPackets = packets.GetRange(
                    i + 1,
                    packets.Count - i - 1
                );

                HandlePacketsRegisteredClient(client, leftoverPackets);
            } else {
                client.Disconnect();
                _clients.TryRemove(client.EndPoint, out _);

                // Throttle the client by adding their IP address without port to the dict
                _throttledClients[client.EndPoint.Address] = Stopwatch.StartNew();

                Logger.Debug($"Throttling connection for client with IP: {client.EndPoint.Address}");
            }

            break;
        }
    }

    /// <summary>
    /// Stops the server and cleans up everything.
    /// </summary>
    public void Stop() {
        // Clean up existing clients
        foreach (var client in _clients.Values) {
            client.Disconnect();
        }

        _clients.Clear();
        _registeredClients.Clear();
        _throttledClients.Clear();

        _udpSocket.Close();

        _leftoverData = null;

        IsStarted = false;

        // Request cancellation for the tasks that are still running
        _taskTokenSource.Cancel();

        _processingWaitHandle.Dispose();

        // Invoke the shutdown event to notify all registered parties of the shutdown
        ShutdownEvent?.Invoke();
    }

    /// <summary>
    /// Callback method for when a client disconnects from the server.
    /// </summary>
    /// <param name="id">The ID of the client.</param>
    public void OnClientDisconnect(ushort id) {
        if (!_registeredClients.TryGetValue(id, out var client)) {
            Logger.Warn($"Handling disconnect from ID {id}, but there's no matching client");
            return;
        }

        client.Disconnect();
        _registeredClients.TryRemove(id, out _);
        _clients.TryRemove(client.EndPoint, out _);

        Logger.Info($"Client {id} disconnected");
    }

    /// <summary>
    /// Get the update manager for the client with the given ID.
    /// </summary>
    /// <param name="id">The ID of the client.</param>
    /// <returns>The update manager for the client, or null if there does not exist a client with the
    /// given ID.</returns>
    public ServerUpdateManager GetUpdateManagerForClient(ushort id) {
        if (!_registeredClients.TryGetValue(id, out var netServerClient)) {
            return null;
        }

        return netServerClient.UpdateManager;
    }

    /// <summary>
    /// Execute a given action for the update manager of all connected clients.
    /// </summary>
    /// <param name="dataAction">The action to execute with each update manager.</param>
    public void SetDataForAllClients(Action<ServerUpdateManager> dataAction) {
        foreach (var netServerClient in _registeredClients.Values) {
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
    /// <summary>
    /// Byte array of received data.
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// The IP end-point of the client from which we received the data.
    /// </summary>
    public IPEndPoint EndPoint { get; set; }
}
