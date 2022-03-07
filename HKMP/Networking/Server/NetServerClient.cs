using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Hkmp.Networking.Server {
    /**
     * A client managed by the server.
     * This is only used for communication from server to client.
     */
    public class NetServerClient {
        private static readonly HashSet<ushort> UsedIds = new HashSet<ushort>();
        private static ushort _lastId = 0;

        public ushort Id { get; }

        public bool IsRegistered { get; set; }

        public ServerUpdateManager UpdateManager { get; }

        private readonly IPEndPoint _endPoint;

        public NetServerClient(UdpClient udpClient, IPEndPoint endPoint) {
            // Also store endpoint with TCP address and TCP port
            _endPoint = endPoint;

            Id = GetId();
            UpdateManager = new ServerUpdateManager(udpClient, _endPoint);
        }

        public bool HasAddress(IPEndPoint endPoint) {
            return _endPoint.Address.Equals(endPoint.Address) && _endPoint.Port == endPoint.Port;
        }

        public void Disconnect() {
            UsedIds.Remove(Id);
            
            UpdateManager.StopUdpUpdates();
        }

        private static ushort GetId() {
            ushort newId;
            do {
                newId = _lastId++;
            } while (UsedIds.Contains(newId));

            UsedIds.Add(newId);
            return newId;
        }
    }
}