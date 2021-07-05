namespace Hkmp.Networking.Packet.Data {
    public class ClientPlayerSkinUpdate : IPacketData {
        public ushort Id { get; set; }

        public byte SkinId { get; set; }

        public void WriteData(Packet packet) {
            packet.Write(Id);
            packet.Write(SkinId);
        }

        public void ReadData(Packet packet) {
            Id = packet.ReadUShort();
            SkinId = packet.ReadByte();
        }
    }

    public class ServerPlayerSkinUpdate : IPacketData {
        public byte SkinId { get; set; }

        public void WriteData(Packet packet) {
            packet.Write(SkinId);
        }

        public void ReadData(Packet packet) {
            SkinId = packet.ReadByte();
        }
    }
}