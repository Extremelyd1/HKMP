namespace Hkmp.Networking.Packet.Data {
    public class ClientPlayerLeaveScene : IPacketData {
        
        public ushort Id { get; set; }
        
        // Whether the player receiving this packet becomes the scene host
        // due to this player leaving
        public bool SceneHost { get; set; }
        
        public void WriteData(Packet packet) {
            packet.Write(Id);
            packet.Write(SceneHost);
        }

        public void ReadData(Packet packet) {
            Id = packet.ReadUShort();
            SceneHost = packet.ReadBool();
        }
    }
}