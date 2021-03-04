namespace HKMP.Networking.Packet.Custom {
    public class PlayerDisconnectPacket : Packet, IPacket {

        public PlayerDisconnectPacket() {
        }
        
        public PlayerDisconnectPacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();
            
            Write(PacketId.PlayerDisconnect);
            
            WriteLength();

            return this;
        }

        public void ReadPacket() {
        }
    }
}