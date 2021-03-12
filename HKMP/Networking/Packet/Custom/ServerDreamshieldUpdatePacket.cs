namespace HKMP.Networking.Packet.Custom {
    public class ServerDreamshieldUpdatePacket : Packet, IPacket {
        
        // Whether we blocked a projectile that didn't disable the shield
        public bool BlockEffect { get; set; }
        // Whether the dreamshield is breaks due to an enemy attack
        public bool BreakEffect { get; set; }
        // Whether the dreamshield is reformed after a break
        public bool ReformEffect { get; set; }
        

        public ServerDreamshieldUpdatePacket() {
        }

        public ServerDreamshieldUpdatePacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();

            Write(PacketId.DreamshieldUpdate);

            Write(BlockEffect);
            Write(BreakEffect);
            Write(ReformEffect);
            
            WriteLength();

            return this;
        }

        public void ReadPacket() {
            BlockEffect = ReadBool();
            BreakEffect = ReadBool();
            ReformEffect = ReadBool();
        }
    }
}