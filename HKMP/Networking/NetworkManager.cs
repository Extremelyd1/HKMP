using HKMP.Networking.Client;
using HKMP.Networking.Packet;
using HKMP.Networking.Server;
using HKMP.ServerKnights;

namespace HKMP.Networking {
    public class NetworkManager {
        public const int LocalUdpPort = 26951;

        private readonly NetClient _netClient;
        private readonly NetServer _netServer;

        public NetworkManager(PacketManager packetManager,ServerKnightsManager serverKnightsManager) {
            _netClient = new NetClient(packetManager);
            _netServer = new NetServer(packetManager,serverKnightsManager);
        }

        public NetClient GetNetClient() {
            return _netClient;
        }

        public NetServer GetNetServer() {
            return _netServer;
        }
    }
}