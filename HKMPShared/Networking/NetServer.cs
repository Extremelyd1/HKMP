using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Hkmp.Concurrency;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking {
    /**
     * Server that manages connection with clients 
     */
    public class NetServer {
        private readonly object _lock = new object();

        private readonly PacketManager _packetManager;

        private readonly ConcurrentDictionary<ushort, NetServerClient> _registeredClients;
        private readonly ConcurrentList<NetServerClient> _clients;

        private UdpClient _udpClient;

        private byte[] _leftoverData;

        private event Action<ushort> OnClientTimeout;
        private event Action OnShutdownEvent;

        public bool IsStarted { get; private set; }

        public NetServer(PacketManager packetManager) {
            _packetManager = packetManager;

            _registeredClients = new ConcurrentDictionary<ushort, NetServerClient>();
            _clients = new ConcurrentList<NetServerClient>();
        }

        public void RegisterOnClientTimeout(Action<ushort> onClientTimeout) {
            OnClientTimeout += onClientTimeout;
        }

        public void RegisterOnShutdown(Action onShutdown) {
            OnShutdownEvent += onShutdown;
        }

        /**
         * Starts the server on the given port
         */
        public void Start(int port) {
            Logger.Get().Info(this, $"Starting NetServer on port {port}");
            IsStarted = true;

            // Initialize the UDP client on the given port
            _udpClient = new UdpClient(port);

            // Start and begin receiving data on both protocols
            _udpClient.BeginReceive(OnUdpReceive, null);
        }

        /**
         * Callback for when UDP traffic is received
         */
        private void OnUdpReceive(IAsyncResult result) {
            // Initialize default IPEndPoint for reference in data receive method
            var endPoint = new IPEndPoint(IPAddress.Any, 0);

            byte[] receivedData;
            try {
                receivedData = _udpClient.EndReceive(result, ref endPoint);
            } catch (Exception e) {
                Logger.Get().Warn(this, $"UDP Receive exception: {e.Message}");
                // Return if an exception was caught, since there's no need to handle the packets then
                return;
            } finally {
                // Immediately start receiving data again regardless of whether there was an exception or not
                _udpClient.BeginReceive(OnUdpReceive, null);
            }
            
            List<Packet.Packet> packets;

            // Lock the leftover data array for synchronous data handling
            // This makes sure that from another asynchronous receive callback we don't
            // read/write to it in different places
            lock (_lock) {
                packets = PacketManager.HandleReceivedData(receivedData, ref _leftoverData);
            }

            // Figure out which client this data is from or if it is a new client
            foreach (var client in _clients.GetCopy()) {
                if (client.HasAddress(endPoint)) {
                    if (client.IsRegistered) {
                        HandlePacketsRegisteredClient(client, packets);
                    } else {
                        HandlePacketsUnregisteredClient(client, packets);
                    }

                    return;
                }
            }

            Logger.Get().Info(this,
                $"Received packet from unknown client with address: {endPoint.Address}:{endPoint.Port}, creating new client");

            // We didn't find a client with the given address, so we assume it is a new client
            // that wants to connect
            var newClient = CreateNewClient(endPoint);

            HandlePacketsUnregisteredClient(newClient, packets);
        }

        /**
         * Create a new client and start sending UDP updates and registering the timeout event
         */
        private NetServerClient CreateNewClient(IPEndPoint endPoint) {
            var netServerClient = new NetServerClient(_udpClient, endPoint);
            netServerClient.UpdateManager.StartUdpUpdates();
            netServerClient.UpdateManager.OnTimeout += () => HandleClientTimeout(netServerClient);

            _clients.Add(netServerClient);

            return netServerClient;
        }

        /**
         * Handles the event when a client times out. Disconnects the UDP client and cleans up any references to
         * the client
         */
        private void HandleClientTimeout(NetServerClient client) {
            var id = client.Id;
                
            // Only execute the client timeout callback if the client is registered and thus has an ID
            if (client.IsRegistered) {
                OnClientTimeout?.Invoke(id);

                _registeredClients.Remove(id);
            }
                
            client.Disconnect();
            _clients.Remove(client);

            Logger.Get().Info(this, $"Client {id} timed out");
        }

        private void HandlePacketsRegisteredClient(NetServerClient client, List<Packet.Packet> packets) {
            var id = client.Id;
            
            foreach (var packet in packets) {
                // Create a server update packet from the raw packet instance
                var serverUpdatePacket = new ServerUpdatePacket(packet);
                serverUpdatePacket.ReadPacket();
                
                client.UpdateManager.OnReceivePacket(serverUpdatePacket);

                // Let the packet manager handle the received data
                _packetManager.HandleServerPacket(id, serverUpdatePacket);
            }
        }

        private void HandlePacketsUnregisteredClient(NetServerClient client, List<Packet.Packet> packets) {
            for (var i = 0; i < packets.Count; i++) {
                var packet = packets[i];

                // Create a server update packet from the raw packet instance
                var serverUpdatePacket = new ServerUpdatePacket(packet);
                serverUpdatePacket.ReadPacket();

                client.UpdateManager.OnReceivePacket(serverUpdatePacket);

                if (!serverUpdatePacket.PacketData.TryGetValue(
                    ServerPacketId.LoginRequest,
                    out var packetData
                )) {
                    continue;
                }

                var loginRequest = (LoginRequest) packetData;

                Logger.Get().Info(this, $"Received login request from '{loginRequest.Username}'");
                
                // For now we accept every client, but this could change if whitelisting/max capacity is implemented
                Logger.Get().Info(this, $"Login request from '{loginRequest.Username}' approved");
                client.UpdateManager.SetLoginResponseData(LoginResponseStatus.Success);
                
                // Register the client, which assigns an ID and add them to the dictionary
                client.Register();
                _registeredClients[client.Id] = client;

                // Now that the client is registered, we forward the rest of the packets to the other handler
                var leftoverPackets = packets.GetRange(
                    i + 1, 
                    packets.Count - i - 1
                );

                HandlePacketsRegisteredClient(client, leftoverPackets);
                
                break;
            }
        }

        /**
         * Stops the server
         */
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
            OnShutdownEvent?.Invoke();
        }

        public void OnClientDisconnect(ushort id) {
            if (!_registeredClients.TryGetValue(id, out var client)) {
                Logger.Get().Warn(this, $"Handling disconnect from ID {id}, but there's no matching client");
                return;
            }

            client.Disconnect();
            _registeredClients.Remove(id);
            _clients.Remove(client);

            Logger.Get().Info(this, $"Client {id} disconnected");
        }

        public ServerUpdateManager GetUpdateManagerForClient(ushort id) {
            if (!_registeredClients.TryGetValue(id, out var netServerClient)) {
                return null;
            }

            return netServerClient.UpdateManager;
        }

        public void SetDataForAllClients(Action<ServerUpdateManager> dataAction) {
            foreach (var netServerClient in _registeredClients.GetCopy().Values) {
                dataAction(netServerClient.UpdateManager);
            }
        }
    }
}