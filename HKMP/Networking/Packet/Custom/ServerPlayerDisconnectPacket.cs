namespace HKMP.Networking.Packet.Custom {
    public class ServerPlayerDisconnectPacket : GenericServerPacket {

        public ServerPlayerDisconnectPacket() : base(PacketId.PlayerDisconnect) {
        }
        
        public ServerPlayerDisconnectPacket(Packet packet) : base(PacketId.PlayerDisconnect, packet) {
        }
    }
}