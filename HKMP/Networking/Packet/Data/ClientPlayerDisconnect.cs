namespace Hkmp.Networking.Packet.Data {
    public class ClientPlayerDisconnect : GenericClientData {
        public string Username { get; set; }
        public bool TimedOut { get; set; }

        public ClientPlayerDisconnect() {
            IsReliable = true;
            DropReliableDataIfNewerExists = false;
        }
        
        public override void WriteData(IPacket packet) {
            packet.Write(Id);
            packet.Write(Username);
            packet.Write(TimedOut);
        }

        public override void ReadData(IPacket packet) {
            Id = packet.ReadUShort();
            Username = packet.ReadString();
            TimedOut = packet.ReadBool();
        }
    }
}