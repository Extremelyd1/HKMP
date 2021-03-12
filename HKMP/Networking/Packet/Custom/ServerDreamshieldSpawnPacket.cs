namespace HKMP.Networking.Packet.Custom {
    public class ServerDreamshieldSpawnPacket : GenericServerPacket {
        
        public ServerDreamshieldSpawnPacket() : base(PacketId.DreamshieldSpawn) {
        }

        public ServerDreamshieldSpawnPacket(Packet packet) : base(PacketId.DreamshieldSpawn, packet) {
        }
    }
}