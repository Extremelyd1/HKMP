namespace HKMP.Networking.Packet.Custom {
    public class PlayerLeaveScenePacket : Packet, IPacket {

        public int Id { get; set; }

        public PlayerLeaveScenePacket() {
        }
        
        public PlayerLeaveScenePacket(Packet packet) : base(packet) {
        }
        
        public void CreatePacket() {
            Reset();

            Write(PacketId.PlayerLeaveScene);

            Write(Id);
            
            WriteLength();
        }

        public void ReadPacket() {
            Id = ReadInt();
        }
    }
}