namespace HKMP.Networking.Packet.Custom {
    public class ClientDreamshieldUpdatePacket : Packet, IPacket {
        
        public ushort Id { get; set; }
        
        // Whether we blocked a projectile that didn't disable the shield
        public bool BlockEffect { get; set; }
        // Whether the dreamshield is breaks due to an enemy attack
        public bool BreakEffect { get; set; }
        // Whether the dreamshield is reformed after a break
        public bool ReformEffect { get; set; }
        

        public ClientDreamshieldUpdatePacket() {
        }

        public ClientDreamshieldUpdatePacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();

            Write(PacketId.DreamshieldUpdate);

            Write(Id);

            Write(BlockEffect);
            Write(BreakEffect);
            Write(ReformEffect);
            
            WriteLength();

            return this;
        }

        public void ReadPacket() {
            Id = ReadUShort();
            BlockEffect = ReadBool();
            BreakEffect = ReadBool();
            ReformEffect = ReadBool();
        }
    }
}