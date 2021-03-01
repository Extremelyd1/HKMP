namespace HKMP.Networking.Packet.Custom {
    public class ServerPlayerDeathPacket : Packet, IPacket {

        public ServerPlayerDeathPacket() {
        }
        
        public ServerPlayerDeathPacket(Packet packet) : base(packet) {
        }
        
        public void CreatePacket() {
            Reset();

            Write(PacketId.ServerPlayerDeath);
            
            WriteLength();
        }

        public void ReadPacket() {
        }
    }
}