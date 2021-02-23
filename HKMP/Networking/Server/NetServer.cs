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
        private readonly List<NetServerClient> _clients;

        private TcpListener _tcpListener;
        private UdpClient _udpClient;
        
        public bool IsStarted { get; private set; }

        public NetServer(PacketManager packetManager) {
            _packetManager = packetManager;
            
            _clients = new List<NetServerClient>();
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
            _clients.Add(newClient);
            
            Logger.Info(this, $"Accepted TCP connection from {tcpClient.Client.RemoteEndPoint}, assigned ID {newClient.GetId()}");

            // Start listening for new clients again
            _tcpListener.BeginAcceptTcpClient(OnTcpConnection, null);
        }

        /**
         * Callback for when TCP traffic is received
         */
        private void OnTcpReceive(int id, byte[] receivedData) {
            Logger.Info(this, $"Received TCP data from ID {id}");
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
            foreach (var client in _clients) {
                if (client.HasAddress(endPoint)) {
                    id = client.GetId();
                    break;
                }
            }

            if (id == -1) {
                Logger.Info(this, $"Received UDP data from {endPoint.Address}, but there was no matching known client");
            } else {
                Logger.Info(this, $"Received UDP data from {id}");

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
            foreach (var client in _clients) {
                if (client.GetId() == id) {
                        client.SendTcp(packet);
                }
            }
        }
        
        /**
         * Sends a packet to the client with the given ID over UDP
         */
        public void SendUdp(int id, Packet.Packet packet) {
            foreach (var client in _clients) {
                if (client.GetId() == id) {
                    client.SendUdp(_udpClient, packet);
                    return;
                }
            }

            Logger.Info(this, $"Could not find ID {id} in clients, could not send UDP packet");
        }

        /**
         * Sends a packet to all connected clients over TCP
         */
        public void BroadcastTcp(Packet.Packet packet) {
            foreach (var client in _clients) {
                client.SendTcp(packet);
            }
        }
        
        /**
         * Sends a packet to all connected clients over UDP
         */
        public void Broadcast(bool tcp, Packet.Packet packet) {
            foreach (var client in _clients) {
                client.SendUdp(_udpClient, packet);
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
            
            IsStarted = false;
        }
        
    }
}