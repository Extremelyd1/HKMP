namespace Hkmp.Networking.Packet.Data {
    public class ClientPlayerDisconnect : GenericClientData {
        public string Username { get; set; }
        public bool TimedOut { get; set; }
        public bool SceneHost { get; set; }

        public ClientPlayerDisconnect() {
            IsReliable = true;
            DropReliableDataIfNewerExists = false;
        }
        
        public override void WriteData(Packet packet) {
            packet.Write(Id);
            packet.Write(Username);
            packet.Write(TimedOut);
            packet.Write(SceneHost);
        }

        public override void ReadData(Packet packet) {
            Id = packet.ReadUShort();
            Username = packet.ReadString();
            TimedOut = packet.ReadBool();
            SceneHost = packet.ReadBool();
        }
    }
}