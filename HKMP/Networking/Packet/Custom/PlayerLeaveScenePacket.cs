namespace HKMP.Networking.Packet.Custom {
    public class PlayerLeaveScenePacket : Packet, IPacket {

        public int Id { get; set; }

        public PlayerLeaveScenePacket() {
        }
        
        public PlayerLeaveScenePacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();

            Write(PacketId.PlayerLeaveScene);

            Write(Id);
            
            WriteLength();

            return this;
        }

        public void ReadPacket() {
            Id = ReadInt();
        }
    }
}