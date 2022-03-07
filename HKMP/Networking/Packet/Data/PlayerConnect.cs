namespace Hkmp.Networking.Packet.Data {
    public class PlayerConnect : GenericClientData {
        public string Username { get; set; }

        public PlayerConnect() {
            IsReliable = true;
            DropReliableDataIfNewerExists = false;
        }
        
        public override void WriteData(IPacket packet) {
            packet.Write(Id);
            packet.Write(Username);
        }

        public override void ReadData(IPacket packet) {
            Id = packet.ReadUShort();
            Username = packet.ReadString();
        }
    }
}