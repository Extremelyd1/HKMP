namespace HKMP.Networking.Packet.Custom {
    public class ServerPlayerDeathPacket : GenericServerPacket {

        public ServerPlayerDeathPacket() : base(PacketId.PlayerDeath) {
        }
        
        public ServerPlayerDeathPacket(Packet packet) : base(PacketId.PlayerDeath, packet) {
        }
    }
}