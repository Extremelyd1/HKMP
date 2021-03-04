using System;
using HKMP.Networking.Client;
using HKMP.Networking.Packet;
using HKMP.Networking.Packet.Custom;
using HKMP.Networking.Server;

namespace HKMP.Networking {
    public class NetworkManager {
        public const int LocalUdpPort = 26951;

        private readonly NetClient _netClient;
        private readonly NetServer _netServer;

        public NetworkManager(PacketManager packetManager) {
            _netClient = new NetClient(packetManager);
            _netServer = new NetServer(packetManager);
        }

        public NetClient GetNetClient() {
            return _netClient;
        }

        public NetServer GetNetServer() {
            return _netServer;
        }
    }
}