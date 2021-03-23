using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace HKMP.Networking.Server {
    public delegate void OnReceive(ushort id, List<Packet.Packet> packets);
    /**
     * A client managed by the server.
     * This is only used for communication from server to client.
     */
    public class NetServerClient {
        private static ushort _lastId = 0;

        private readonly ushort _id;
        private readonly TcpNetClient _tcpNetClient;

        private readonly IPEndPoint _endPoint;

        private event OnReceive OnReceiveEvent;

        public NetServerClient(TcpClient tcpClient) : this(_lastId++, tcpClient) {
        }

        public NetServerClient(ushort id, TcpClient tcpClient) {
            _id = id;

            // Also store endpoint with TCP address and TCP port
            _endPoint = (IPEndPoint) tcpClient.Client.RemoteEndPoint;

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

        private void OnReceiveData(List<Packet.Packet> packets) {
            OnReceiveEvent?.Invoke(_id, packets);
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
            udpClient.BeginSend(packet.ToArray(), packet.Length(), _endPoint, null, null);
        }

        public bool HasAddress(IPEndPoint endPoint) {
            return _endPoint.Address.Equals(endPoint.Address) && _endPoint.Port == endPoint.Port;
        }

        public ushort GetId() {
            return _id;
        }

        public void Disconnect() {
            _tcpNetClient?.Disconnect();
        }

    }
}