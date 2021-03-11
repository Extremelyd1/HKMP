namespace HKMP.Networking.Packet.Custom {
    public class ServerDreamshieldDespawnPacket : GenericServerPacket {
        
        public ServerDreamshieldDespawnPacket() : base(PacketId.DreamshieldDespawn) {
        }

        public ServerDreamshieldDespawnPacket(Packet packet) : base(PacketId.DreamshieldDespawn, packet) {
        }
    }
}