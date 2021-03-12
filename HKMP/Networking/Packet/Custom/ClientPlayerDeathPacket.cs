namespace HKMP.Networking.Packet.Custom {
    public class ClientPlayerDeathPacket : GenericClientPacket {
        
        public ClientPlayerDeathPacket() : base(PacketId.PlayerDeath) {
        }
        
        public ClientPlayerDeathPacket(Packet packet) : base(PacketId.PlayerDeath, packet) {
        }
    }
}