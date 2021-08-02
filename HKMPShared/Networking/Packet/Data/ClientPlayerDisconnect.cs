namespace Hkmp.Networking.Packet.Data {
    public class ClientPlayerDisconnect : GenericClientData {
        public string Username { get; set; }

        public ClientPlayerDisconnect() {
            IsReliable = true;
            DropReliableDataIfNewerExists = false;
        }
        
        public override void WriteData(Packet packet) {
            packet.Write(Id);
            packet.Write(Username);
        }

        public override void ReadData(Packet packet) {
            Id = packet.ReadUShort();
            Username = packet.ReadString();
        }
    }
}