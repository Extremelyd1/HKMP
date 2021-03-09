namespace HKMP.Networking.Packet.Custom {
    public class ServerHeartBeatPacket : Packet, IPacket {

        public ServerHeartBeatPacket() {
        }
        
        public ServerHeartBeatPacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();

            Write(PacketId.ServerHeartBeat);
            
            WriteLength();

            return this;
        }

        public void ReadPacket() {
        }
    }
}