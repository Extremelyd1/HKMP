namespace HKMP.Networking.Packet.Custom {
    public class GenericClientPacket : Packet, IPacket {

        private readonly PacketId _packetId;
        public ushort Id { get; set; }

        protected GenericClientPacket(PacketId packetId) {
            _packetId = packetId;
        }

        protected GenericClientPacket(PacketId packetId, Packet packet) : base(packet) {
            _packetId = packetId;
        }

        public Packet CreatePacket() {
            Reset();

            Write(_packetId);
            
            Write(Id);

            WriteLength();

            return this;
        }

        public void ReadPacket() {
            Id = ReadUShort();
        }
    }
}