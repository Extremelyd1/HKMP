namespace HKMP.Networking.Packet.Custom {
    public class ClientPlayerDeathPacket : Packet, IPacket {
        
        public int Id { get; set; }

        public ClientPlayerDeathPacket() {
        }
        
        public ClientPlayerDeathPacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();

            Write(PacketId.ClientPlayerDeath);
            
            Write(Id);

            WriteLength();

            return this;
        }

        public void ReadPacket() {
            Id = ReadInt();
        }
    }
}