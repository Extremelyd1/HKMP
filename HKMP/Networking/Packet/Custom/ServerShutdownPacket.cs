namespace HKMP.Networking.Packet.Custom {
    public class ServerShutdownPacket : GenericServerPacket {

        public ServerShutdownPacket() : base(PacketId.ServerShutdown) {
        }

        public ServerShutdownPacket(Packet packet) : base(PacketId.ServerShutdown, packet) {
        }
    }
}