namespace HKMP.Networking.Packet.Custom {
    public class ServerShutdownPacket : Packet, IPacket {

        public ServerShutdownPacket() {
        }

        public ServerShutdownPacket(Packet packet) : base(packet) {
        }
        
        public void CreatePacket() {
            Reset();
            
            WriteLength();
        }

        public void ReadPacket() {
        }
    }
}