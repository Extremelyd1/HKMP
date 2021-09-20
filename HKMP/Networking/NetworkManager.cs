using Hkmp.Networking.Client;
using Hkmp.Networking.Packet;

namespace Hkmp.Networking {
    public class NetworkManager {
        private readonly NetClient _netClient;
        private readonly NetServer _netServer;

        public NetworkManager(PacketManager packetManager) {
            _netClient = new NetClient(packetManager);
            _netServer = new NetServer(packetManager);
        }

        public NetClient GetInternalNetClient() {
            return _netClient;
        }

        public NetServer GetInternalNetServer() {
            return _netServer;
        }
    }
}