namespace Hkmp.Networking.Packet.Data {
    public class GenericClientData : IPacketData {
        public ushort Id { get; set; }

        public void WriteData(Packet packet) {
            packet.Write(Id);
        }

        public void ReadData(Packet packet) {
            Id = packet.ReadUShort();
        }
    }
}