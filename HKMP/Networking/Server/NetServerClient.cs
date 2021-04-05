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
        public ServerUpdateManager UpdateManager { get; }

        private readonly IPEndPoint _endPoint;

        public NetServerClient(TcpClient tcpClient, UdpClient udpClient) : this(_lastId++, tcpClient, udpClient) {
        }

        public NetServerClient(ushort id, TcpClient tcpClient, UdpClient udpClient) {
            _id = id;

            // Also store endpoint with TCP address and TCP port
            _endPoint = (IPEndPoint) tcpClient.Client.RemoteEndPoint;

            _tcpNetClient = new TcpNetClient();
            _tcpNetClient.InitializeWithClient(tcpClient);

            UpdateManager = new ServerUpdateManager(udpClient, _endPoint);
        }

        public bool HasAddress(IPEndPoint endPoint) {
            return _endPoint.Address.Equals(endPoint.Address) && _endPoint.Port == endPoint.Port;
        }

        public ushort GetId() {
            return _id;
        }

        public void Disconnect() {
            UpdateManager.StopUdpUpdates();
            _tcpNetClient?.Disconnect();
        }

    }
}