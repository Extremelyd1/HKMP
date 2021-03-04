namespace HKMP.Networking.Packet.Custom {
    public class ServerPlayerDeathPacket : Packet, IPacket {

        public ServerPlayerDeathPacket() {
        }
        
        public ServerPlayerDeathPacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();

            Write(PacketId.ServerPlayerDeath);
            
            WriteLength();

            return this;
        }

        public void ReadPacket() {
        }
    }
}