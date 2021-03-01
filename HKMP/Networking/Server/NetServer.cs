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
        private readonly PacketManager _packetManager;
        private readonly Dictionary<int, NetServerClient> _clients;

        private TcpListener _tcpListener;
        private UdpClient _udpClient;

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

            // Create client and register TCP receive callback
            var newClient = new NetServerClient(tcpClient);
            newClient.RegisterOnTcpReceive(OnTcpReceive);
            _clients[newClient.GetId()] = newClient;
            
            Logger.Info(this, $"Accepted TCP connection from {tcpClient.Client.RemoteEndPoint}, assigned ID {newClient.GetId()}");

            // Start listening for new clients again
            _tcpListener.BeginAcceptTcpClient(OnTcpConnection, null);
        }

        /**
         * Callback for when TCP traffic is received
         */
        private void OnTcpReceive(int id, byte[] receivedData) {
            _packetManager.HandleServerData(id, receivedData);
        }
        
        /**
         * Callback for when UDP traffic is received
         */
        private void OnUdpReceive(IAsyncResult result) {
            // Initialize default IPEndPoint for reference in data receive method
            var endPoint = new IPEndPoint(IPAddress.Any, 0);
            var receivedData = _udpClient.EndReceive(result, ref endPoint);
            
            // Figure out which client ID this data is from
            int id = -1;
            foreach (var client in _clients.Values) {
                if (client.HasAddress(endPoint)) {
                    id = client.GetId();
                    break;
                }
            }

            if (id == -1) {
                Logger.Warn(this, $"Received UDP data from {endPoint.Address}, but there was no matching known client");
            } else {
                // Let the packet manager handle the received data
                _packetManager.HandleServerData(id, receivedData);
            }

            // Start receiving data again
            _udpClient.BeginReceive(OnUdpReceive, null);
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