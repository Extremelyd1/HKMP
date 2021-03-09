namespace HKMP.Networking.Packet.Custom {
    public class ClientHeartBeatPacket : Packet, IPacket {

        public ClientHeartBeatPacket() {
        }
        
        public ClientHeartBeatPacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();

            Write(PacketId.ClientHeartBeat);
            
            WriteLength();

            return this;
        }

        public void ReadPacket() {
        }
    }
}