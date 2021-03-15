namespace HKMP.Networking.Packet.Custom {
    public class AcknowledgePacket : Packet, IPacket {
        
        public ushort SequenceNumber { get; set; }

        public AcknowledgePacket() {
        }

        public AcknowledgePacket(Packet packet) : base(packet) {
        }

        public Packet CreatePacket() {
            Reset();

            Write(PacketId.Acknowledge);
            
            Write(SequenceNumber);
            
            WriteLength();

            return this;
        }

        public void ReadPacket() {
        }
    }
}