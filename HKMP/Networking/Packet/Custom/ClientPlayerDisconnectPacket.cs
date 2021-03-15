namespace HKMP.Networking.Packet.Custom {
    public class ClientPlayerDisconnectPacket : GenericClientPacket {
        public ClientPlayerDisconnectPacket() : base(PacketId.PlayerDisconnect) {
        }

        public ClientPlayerDisconnectPacket(Packet packet) : base(PacketId.PlayerDisconnect, packet) {
        }
    }
}