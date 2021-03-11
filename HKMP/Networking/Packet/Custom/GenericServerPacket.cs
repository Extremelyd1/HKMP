namespace HKMP.Networking.Packet.Custom {
    public class GenericServerPacket : Packet, IPacket {

        private readonly PacketId _packetId;

        protected GenericServerPacket(PacketId packetId) {
            _packetId = packetId;
        }

        protected GenericServerPacket(PacketId packetId, Packet packet) : base(packet) {
            _packetId = packetId;
        }
        
        public Packet CreatePacket() {
            Reset();

            Write(_packetId);
            
            WriteLength();

            return this;
        }

        public void ReadPacket() {
            
        }
    }
}