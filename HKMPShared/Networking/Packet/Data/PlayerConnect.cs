namespace Hkmp.Networking.Packet.Data {
    public class PlayerConnect : IPacketData {
        public ushort Id { get; set; }
        public string Username { get; set; }

        public void WriteData(Packet packet) {
            packet.Write(Id);
            packet.Write(Username);
        }

        public void ReadData(Packet packet) {
            Id = packet.ReadUShort();
            Username = packet.ReadString();
        }
    }
}