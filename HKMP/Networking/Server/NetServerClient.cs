using System.Net;
using System.Net.Sockets;

namespace HKMP.Networking.Server {
    public delegate void OnReceive(int id, byte[] receivedData);
    /**
     * A client managed by the server.
     * This is only used for communication from server to client.
     */
    public class NetServerClient {
        private static int _lastId = 0;

        // TODO: make sure TCP client is closed if not in use anymore
        private readonly int _id;
        private readonly TcpNetClient _tcpNetClient;

        private readonly IPEndPoint _endPoint;

        private event OnReceive OnReceiveEvent;

        public NetServerClient(TcpClient tcpClient) {
            _id = _lastId++;

            // Create UDP endpoint with TCP address and UDP port
            _endPoint = new IPEndPoint(
                ((IPEndPoint) tcpClient.Client.RemoteEndPoint).Address, 
                NetworkManager.LocalUdpPort
            );

            _tcpNetClient = new TcpNetClient();
            _tcpNetClient.InitializeWithClient(tcpClient);
            _tcpNetClient.RegisterOnReceive(OnReceiveData);
        }

        /**
         * Register a callback for when TCP traffic is received from this client
         */
        public void RegisterOnTcpReceive(OnReceive onReceive) {
            OnReceiveEvent += onReceive;
        }

        private void OnReceiveData(byte[] receivedData) {
            OnReceiveEvent?.Invoke(_id, receivedData);
        }
        
        /**
         * Sends a packet over TCP to this specific client
         */
        public void SendTcp(Packet.Packet packet) {
            _tcpNetClient.Send(packet);
        }

        /**
         * Sends a packet over UDP to this specific client
         */
        public void SendUdp(UdpClient udpClient, Packet.Packet packet) {
            // Make sure that the packet contains its length at the front before sending
            packet.WriteLength();
            
            udpClient.BeginSend(packet.ToArray(), packet.Length(), _endPoint, null, null);

            Logger.Info(this, $"Sending UDP packet to ID {_id} on endpoint {_endPoint}");
        }

        public bool HasAddress(IPEndPoint endPoint) {
            return _endPoint.Address.Equals(endPoint.Address);
        }

        public int GetId() {
            return _id;
        }

        public void Disconnect() {
            _tcpNetClient?.Disconnect();
        }

    }
}