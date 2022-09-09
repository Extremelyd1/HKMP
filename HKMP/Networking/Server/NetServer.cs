using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Hkmp.Api.Server;
using Hkmp.Api.Server.Networking;
using Hkmp.Concurrency;
using Hkmp.Logging;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Server {
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
        /// List containing all net server clients.
        /// </summary>
        private readonly ConcurrentList<NetServerClient> _clients;

        /// <summary>
        /// The underlying UDP client.
        /// </summary>
        private UdpClient _udpClient;

        /// <summary>
        /// Object to lock asynchronous access to the leftover data array.
        /// </summary>
        private readonly object _leftoverDataLock = new object();

        /// <summary>
        /// Byte array containing leftover data that was not processed as a packet yet.
        /// </summary>
        private byte[] _leftoverData;

        /// <summary>
        /// Event that is called when a client times out.
        /// </summary>
        public event Action<ushort> ClientTimeoutEvent;

        /// <summary>
        /// Event that is called when the server shuts down.
        /// </summary>
        public event Action ShutdownEvent;

        /// <summary>
        /// Event that is called when a new client wants to login.
        /// </summary>
        public event LoginRequestHandler LoginRequestEvent;

        /// <inheritdoc />
        public bool IsStarted { get; private set; }

        public NetServer(PacketManager packetManager) {
            _packetManager = packetManager;

            _registeredClients = new ConcurrentDictionary<ushort, NetServerClient>();
            _clients = new ConcurrentList<NetServerClient>();
        }

        /// <summary>
        /// Starts the server on the given port.
        /// </summary>
        /// <param name="port">The networking port.</param>
        public void Start(int port) {
            Logger.Info($"Starting NetServer on port {port}");
            IsStarted = true;

            // Initialize the UDP client on the given port
            _udpClient = new UdpClient(port);

            // Start and begin receiving data on both protocols
            _udpClient.BeginReceive(OnUdpReceive, null);
        }

        /// <summary>
        /// Callback for when UDP traffic is received.
        /// </summary>
        /// <param name="result">The async result.</param>
        private void OnUdpReceive(IAsyncResult result) {
            // Initialize default IPEndPoint for reference in data receive method
            var endPoint = new IPEndPoint(IPAddress.Any, 0);

            byte[] receivedData;
            try {
                receivedData = _udpClient.EndReceive(result, ref endPoint);
            } catch {
                // Logger.Info($"UDP EndReceive exception: {e.GetType()}, message: {e.Message}");
                // Return if an exception was caught, since there's no need to handle the packets then
                return;
            } finally {
                // Immediately start receiving data again regardless of whether there was an exception or not.
                // But we do this in a loop since it can throw an exception in some cases.
                var tries = 10;
                while (tries > 0) {
                    try {
                        _udpClient.BeginReceive(OnUdpReceive, null);
                        break;
                    } catch (Exception e) {
                        Logger.Info($"UDP BeginReceive exception: {e.GetType()}, message: {e.Message}");
                    }

                    tries--;
                }

                // If we ran out of tries while starting the UDP receive again, we stop the server entirely
                if (tries == 0) {
                    Logger.Info("Could not successfully call BeginReceive, stopping server");
                    Stop();
                }
            }

            List<Packet.Packet> packets;

            // Lock the leftover data array for synchronous data handling
            // This makes sure that from another asynchronous receive callback we don't
            // read/write to it in different places
            lock (_leftoverDataLock) {
                packets = PacketManager.HandleReceivedData(receivedData, ref _leftoverData);
            }

            var isRegisteredClient = false;
            NetServerClient client = null;

            // We lock while we are searching for the client that corresponds to this endpoint
            lock (_clientLock) {
                // Figure out which client this data is from or if it is a new client
                foreach (var existingClient in _clients.GetCopy()) {
                    if (existingClient.HasAddress(endPoint)) {
                        isRegisteredClient = existingClient.IsRegistered;
                        client = existingClient;

                        break;
                    }
                }

                // If an existing client could not be found, we stay in the lock while creating a new client
                if (client == null) {
                    Logger.Info(
                        $"Received packet from unknown client with address: {endPoint.Address}:{endPoint.Port}, creating new client");

                    // We didn't find a client with the given address, so we assume it is a new client
                    // that wants to connect
                    client = CreateNewClient(endPoint);
                }
            }

            // Outside of the lock we can handle the packets for the found/created client
            if (isRegisteredClient) {
                HandlePacketsRegisteredClient(client, packets);
            } else {
                HandlePacketsUnregisteredClient(client, packets);
            }
        }

        /// <summary>
        /// Create a new client and start sending UDP updates and registering the timeout event.
        /// </summary>
        /// <param name="endPoint">The endpoint of the new client.</param>
        /// <returns>A new net server client instance.</returns>
        private NetServerClient CreateNewClient(IPEndPoint endPoint) {
            var netServerClient = new NetServerClient(_udpClient, endPoint);
            netServerClient.UpdateManager.StartUdpUpdates();
            netServerClient.UpdateManager.OnTimeout += () => HandleClientTimeout(netServerClient);

            _clients.Add(netServerClient);

            return netServerClient;
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
            _registeredClients.Remove(id);
            _clients.Remove(client);

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
                    // If ReadPacket returns false, we received a malformed packet, which we simply ignore for now
                    // Logger.Info("Received malformed packet, ignoring");
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

                // Invoke the handler of the login request and decide what to do with the client based on the result
                var allowClient = LoginRequestEvent?.Invoke(
                    client.Id,
                    client.EndPoint,
                    loginRequest,
                    client.UpdateManager
                );
                if (!allowClient.HasValue) {
                    Logger.Info("Login request has no handler");
                    return;
                }

                if (allowClient.Value) {
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
                    _clients.Remove(client);
                }

                break;
            }
        }

        /// <summary>
        /// Stops the server and cleans up everything.
        /// </summary>
        public void Stop() {
            // Clean up existing clients
            foreach (var client in _clients.GetCopy()) {
                client.Disconnect();
            }

            _clients.Clear();
            _registeredClients.Clear();

            _udpClient.Close();

            _udpClient = null;
            _leftoverData = null;

            IsStarted = false;

            // Invoke the shutdown event to notify all registered parties of the shutdown
            ShutdownEvent?.Invoke();
        }

        /// <summary>
        /// Callback method for when a client disconnects from the server.
        /// </summary>
        /// <param name="id">The ID of the client.</param>
        public void OnClientDisconnect(ushort id) {
            if (!_registeredClients.TryGetValue(id, out var client)) {
                Logger.Info($"Handling disconnect from ID {id}, but there's no matching client");
                return;
            }

            client.Disconnect();
            _registeredClients.Remove(id);
            _clients.Remove(client);

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
            foreach (var netServerClient in _registeredClients.GetCopy().Values) {
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
}
