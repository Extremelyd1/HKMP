using System;
using HKMP.Networking.Packet;

namespace HKMP.Networking.Client {
    public delegate void OnReceive(byte[] receivedData);
    
    /**
     * The networking client that manages both a TCP and UDP client for sending and receiving data.
     * This only manages client side networking, e.g. sending to and receiving from the server.
     */
    public class NetClient {
        private PacketManager _packetManager;
        
        private TcpNetClient _tcpNetClient;
        private UdpNetClient _udpNetClient;
        
        public bool IsConnected { get; private set; }
        
        public NetClient(PacketManager packetManager) {
            _packetManager = packetManager;
            
            _tcpNetClient = new TcpNetClient();
            _udpNetClient = new UdpNetClient();

            // Register the same function for both TCP and UDP receive callbacks
            _tcpNetClient.RegisterOnReceive(OnReceiveData);
            _udpNetClient.RegisterOnReceive(OnReceiveData);
        }

        public void RegisterOnConnect(Action onConnect) {
            _tcpNetClient.RegisterOnConnect(onConnect);
        }

        private void OnReceiveData(byte[] receivedData) {
            Logger.Info(this, "Received data, passing to packet manager");
            _packetManager.HandleClientData(receivedData);
        }

        public void Connect(string host, int port) {
            IsConnected = true;
                
            _tcpNetClient.Connect(host, port);
            // For now use the same local port as the remote port
            _udpNetClient.Connect(host, port, NetworkManager.LocalUdpPort);
        }

        public void SendTcp(Packet.Packet packet) {
            _tcpNetClient.Send(packet);
        }

        public void SendUdp(Packet.Packet packet) {
            _udpNetClient.Send(packet);
        }

        public void Disconnect() {
            _tcpNetClient.Disconnect();
            _udpNetClient.Disconnect();
            
            IsConnected = false;
        }

    }
}