namespace HKMP.Networking.Packet.Custom {
    public class ClientDreamshieldDespawnPacket : GenericClientPacket {
        
        public ClientDreamshieldDespawnPacket() : base(PacketId.DreamshieldDespawn) {
        }

        public ClientDreamshieldDespawnPacket(Packet packet) : base(PacketId.DreamshieldDespawn, packet) {
        }
    }
}