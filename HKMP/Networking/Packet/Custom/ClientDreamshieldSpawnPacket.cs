namespace HKMP.Networking.Packet.Custom {
    public class ClientDreamshieldSpawnPacket : GenericClientPacket {
        
        public ClientDreamshieldSpawnPacket() : base(PacketId.DreamshieldSpawn) {
        }

        public ClientDreamshieldSpawnPacket(Packet packet) : base(PacketId.DreamshieldSpawn, packet) {
        }
    }
}