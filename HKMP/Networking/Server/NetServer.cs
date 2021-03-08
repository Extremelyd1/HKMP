using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using HKMP.Networking.Packet;

namespace HKMP.Networking.Server {
    /**
     * Server that manages connection with clients 
     */
    public class NetServer {
        private readonly object _lock = new object();
        
        private readonly PacketManager _packetManager;
        private readonly Dictionary<int, NetServerClient> _clients;

        private TcpListener _tcpListener;
        private UdpClient _udpClient;

        private byte[] _leftoverData;

        private event Action OnShutdownEvent;

        public bool IsStarted { get; private set; }

        public NetServer(PacketManager packetManager) {
            _packetManager = packetManager;

            _clients = new Dictionary<int, NetServerClient>();
        }

        public void RegisterOnShutdown(Action onShutdown) {
            OnShutdownEvent += onShutdown;
        }

        /**
         * Starts the server on the given port
         */
        public void Start(int port) {
            Logger.Info(this, $"Starting NetServer on port {port}");

            IsStarted = true;

            // Initialize TCP listener and UDP client
            _tcpListener = new TcpListener(IPAddress.Any, port);
            _udpClient = new UdpClient(port);

            // Start and begin receiving data on both protocols
            _tcpListener.Start();
            _tcpListener.BeginAcceptTcpClient(OnTcpConnection, null);
            _udpClient.BeginReceive(OnUdpReceive, null);
        }

        /**
         * Callback for when a TCP connection is accepted
         */
        private void OnTcpConnection(IAsyncResult result) {
            // Retrieve the TCP client from the incoming connection
            var tcpClient = _tcpListener.EndAcceptTcpClient(result);

            // Check whether  there already exists a client with the given IP and store its ID
            var id = -1;
            foreach (var clientPair in _clients) {
                var netServerClient = clientPair.Value;

                if (netServerClient.HasAddress((IPEndPoint) tcpClient.Client.RemoteEndPoint)) {
                    Logger.Info(this, "A client with the same IP already exists, overwriting NetServerClient");

                    // Since it already exists, we now have to disconnect the old one
                    netServerClient.Disconnect();

                    id = clientPair.Key;
                    break;
                }
            }

            // Create client and register TCP receive callback
            // If we found an existing ID for the incoming IP, we use that existing ID and overwrite the old one
            NetServerClient newClient;
            if (id == -1) {
                newClient = new NetServerClient(tcpClient);
            } else {
                newClient = new NetServerClient(id, tcpClient);
            }

            newClient.RegisterOnTcpReceive(OnTcpReceive);
            _clients[newClient.GetId()] = newClient;

            Logger.Info(this,
                $"Accepted TCP connection from {tcpClient.Client.RemoteEndPoint}, assigned ID {newClient.GetId()}");

            // Start listening for new clients again
            _tcpListener.BeginAcceptTcpClient(OnTcpConnection, null);
        }

        /**
         * Callback for when TCP traffic is received
         */
        private void OnTcpReceive(int id, List<Packet.Packet> packets) {
            _packetManager.HandleServerPackets(id, packets);
        }

        /**
         * Callback for when UDP traffic is received
         */
        private void OnUdpReceive(IAsyncResult result) {
            // Initialize default IPEndPoint for reference in data receive method
            var endPoint = new IPEndPoint(IPAddress.Any, 0);

            byte[] receivedData = { };
            try {
                receivedData = _udpClient.EndReceive(result, ref endPoint);
            } catch (Exception e) {
                Logger.Warn(this, $"UDP Receive exception: {e.Message}");
            }
            
            // Immediately start receiving data again
            _udpClient.BeginReceive(OnUdpReceive, null);

            // Figure out which client ID this data is from
            var id = -1;
            foreach (var client in _clients.Values) {
                if (client.HasAddress(endPoint)) {
                    id = client.GetId();
                    break;
                }
            }

            if (id == -1) {
                Logger.Warn(this,
                    $"Received UDP data from {endPoint.Address}, but there was no matching known client");

                return;
            }
            
            List<Packet.Packet> packets;

            // Lock the leftover data array for synchronous data handling
            // This makes sure that from another asynchronous receive callback we don't
            // read/write to it in different places
            lock (_lock) {
                packets = PacketManager.HandleReceivedData(receivedData, ref _leftoverData);
            }

            // Let the packet manager handle the received data
            _packetManager.HandleServerPackets(id, packets);
        }

        /**
         * Sends a packet to the client with the given ID over TCP
         */
        public void SendTcp(int id, Packet.Packet packet) {
            if (!_clients.ContainsKey(id)) {
                Logger.Info(this, $"Could not find ID {id} in clients, could not send TCP packet");
                return;
            }

            // Make sure that we use a clean packet object every time
            var newPacket = new Packet.Packet(packet.ToArray());
            // Send the newly constructed packet to the client
            _clients[id].SendTcp(newPacket);
        }

        /**
         * Sends a packet to the client with the given ID over UDP
         */
        public void SendUdp(int id, Packet.Packet packet) {
            if (!_clients.ContainsKey(id)) {
                Logger.Info(this, $"Could not find ID {id} in clients, could not send UDP packet");
                return;
            }

            // Make sure that we use a clean packet object every time
            var newPacket = new Packet.Packet(packet.ToArray());
            // Send the newly constructed packet to the client
            _clients[id].SendUdp(_udpClient, newPacket);
        }

        /**
         * Sends a packet to all connected clients over TCP
         */
        public void BroadcastTcp(Packet.Packet packet) {
            foreach (var idClientPair in _clients) {
                // Make sure that we use a clean packet object every time
                var newPacket = new Packet.Packet(packet.ToArray());
                // Send the newly constructed packet to the client
                idClientPair.Value.SendTcp(newPacket);
            }
        }

        /**
         * Sends a packet to all connected clients over UDP
         */
        public void BroadcastUdp(Packet.Packet packet) {
            foreach (var client in _clients.Values) {
                // Make sure that we use a clean packet object every time
                var newPacket = new Packet.Packet(packet.ToArray());
                // Send the newly constructed packet to the client
                client.SendUdp(_udpClient, newPacket);
            }
        }

        /**
         * Stops the server
         */
        public void Stop() {
            _tcpListener.Stop();
            _udpClient.Close();

            _tcpListener = null;
            _udpClient = null;

            // Clean up existing clients
            foreach (var idClientPair in _clients) {
                idClientPair.Value.Disconnect();
            }

            _clients.Clear();

            IsStarted = false;

            // Invoke the shutdown event to notify all registered parties of the shutdown
            OnShutdownEvent?.Invoke();
        }

        public void OnClientDisconnect(int id) {
            if (!_clients.ContainsKey(id)) {
                Logger.Warn(this, $"Disconnect packet received from ID {id}, but client is not in client list");
                return;
            }

            _clients[id].Disconnect();
            _clients.Remove(id);

            Logger.Info(this, $"Client {id} disconnected");
        }
    }
}