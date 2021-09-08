namespace Hkmp.Networking.Packet.Data {
    public class ClientPlayerSkinUpdate : GenericClientData {

        public byte SkinId { get; set; }

        public ClientPlayerSkinUpdate() {
            IsReliable = true;
            DropReliableDataIfNewerExists = true;
        }
        
        public override void WriteData(Packet packet) {
            packet.Write(Id);
            packet.Write(SkinId);
        }

        public override void ReadData(Packet packet) {
            Id = packet.ReadUShort();
            SkinId = packet.ReadByte();
        }
    }

    public class ServerPlayerSkinUpdate : IPacketData {
        public bool IsReliable => true;
        
        public bool DropReliableDataIfNewerExists => true;
        
        public byte SkinId { get; set; }

        public void WriteData(Packet packet) {
            packet.Write(SkinId);
        }

        public void ReadData(Packet packet) {
            SkinId = packet.ReadByte();
        }
    }
}