namespace HKMP.Networking.Packet.Custom {
    public class ServerHeartBeatPacket : GenericServerPacket {

        public ServerHeartBeatPacket() : base(PacketId.HeartBeat) {
        }
        
        public ServerHeartBeatPacket(Packet packet) : base(PacketId.HeartBeat, packet) {
        }
    }
}