namespace Hkmp.Networking.Packet.Data {
    public class ClientPlayerLeaveScene : GenericClientData {
        
        // Whether the player receiving this packet becomes the scene host
        // due to this player leaving
        public bool SceneHost { get; set; }
        
        public override void WriteData(Packet packet) {
            packet.Write(Id);
            packet.Write(SceneHost);
        }

        public override void ReadData(Packet packet) {
            Id = packet.ReadUShort();
            SceneHost = packet.ReadBool();
        }
    }
}