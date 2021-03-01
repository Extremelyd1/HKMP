namespace HKMP.Networking.Packet.Custom {
    public class ClientPlayerDeathPacket : Packet, IPacket {
        
        public int Id { get; set; }

        public ClientPlayerDeathPacket() {
        }
        
        public ClientPlayerDeathPacket(Packet packet) : base(packet) {
        }
        
        public void CreatePacket() {
            Reset();

            Write(PacketId.ClientPlayerDeath);
            
            Write(Id);

            WriteLength();
        }

        public void ReadPacket() {
            Id = ReadInt();
        }
    }
}