using System.Net;
using System.Net.Sockets;
using Hkmp.Networking;

namespace Hkmp {
    /**
     * A client managed by the server.
     * This is only used for communication from server to client.
     */
    public class NetServerClient {
        private static ushort _lastId = 0;

        public ushort Id { get; private set; }

        public bool IsRegistered { get; private set; }

        public ServerUpdateManager UpdateManager { get; }

        private readonly IPEndPoint _endPoint;

        public NetServerClient(UdpClient udpClient, IPEndPoint endPoint) {
            // Also store endpoint with TCP address and TCP port
            _endPoint = endPoint;

            UpdateManager = new ServerUpdateManager(udpClient, _endPoint);
        }

        public void Register() {
            Id = _lastId++;
            IsRegistered = true;
        }

        public bool HasAddress(IPEndPoint endPoint) {
            return _endPoint.Address.Equals(endPoint.Address) && _endPoint.Port == endPoint.Port;
        }

        public void Disconnect() {
            UpdateManager.StopUdpUpdates();
        }
    }
}