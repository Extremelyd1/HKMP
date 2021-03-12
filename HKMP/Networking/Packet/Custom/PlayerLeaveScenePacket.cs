namespace HKMP.Networking.Packet.Custom {
    public class PlayerLeaveScenePacket : GenericClientPacket {

        public PlayerLeaveScenePacket() : base(PacketId.PlayerLeaveScene) {
        }
        
        public PlayerLeaveScenePacket(Packet packet) : base(PacketId.PlayerLeaveScene, packet) {
        }
    }
}