namespace HKMP.Networking.Packet.Custom {
    public class ServerShutdownPacket : Packet, IPacket {

        public ServerShutdownPacket() {
        }

        public ServerShutdownPacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();

            Write(PacketId.ServerShutdown);
            
            WriteLength();

            return this;
        }

        public void ReadPacket() {
        }
    }
}