namespace HKMP.Networking.Packet.Custom {
    public class ClientHeartBeatPacket : GenericClientPacket {

        public ClientHeartBeatPacket() : base(PacketId.HeartBeat) {
        }
        
        public ClientHeartBeatPacket(Packet packet) : base(PacketId.HeartBeat, packet) {
        }
    }
}