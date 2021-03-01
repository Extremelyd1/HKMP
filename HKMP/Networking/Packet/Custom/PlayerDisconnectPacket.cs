namespace HKMP.Networking.Packet.Custom {
    public class PlayerDisconnectPacket : Packet, IPacket {

        public PlayerDisconnectPacket() {
        }
        
        public PlayerDisconnectPacket(Packet packet) : base(packet) {
        }
        
        public void CreatePacket() {
            Reset();
            
            Write(PacketId.PlayerDisconnect);
            
            WriteLength();
        }

        public void ReadPacket() {
        }
    }
}