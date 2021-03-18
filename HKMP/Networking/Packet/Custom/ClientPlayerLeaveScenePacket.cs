namespace HKMP.Networking.Packet.Custom {
    public class ClientPlayerLeaveScenePacket : GenericClientPacket {

        public ClientPlayerLeaveScenePacket() : base(PacketId.PlayerLeaveScene) {
        }
        
        public ClientPlayerLeaveScenePacket(Packet packet) : base(PacketId.PlayerLeaveScene, packet) {
        }
    }

    public class ServerPlayerLeaveScenePacket : GenericServerPacket {
        public ServerPlayerLeaveScenePacket() : base(PacketId.PlayerLeaveScene) {
        }

        public ServerPlayerLeaveScenePacket(Packet packet) : base(PacketId.PlayerLeaveScene, packet) {
        }
    }
}