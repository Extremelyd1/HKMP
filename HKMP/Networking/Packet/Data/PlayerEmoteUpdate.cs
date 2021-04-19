namespace HKMP.Networking.Packet.Data {
    public class ClientPlayerEmoteUpdate : IPacketData {
        
        public ushort Id { get; set; }
        
        public byte EmoteId { get; set; }
        
        public void WriteData(Packet packet) {
            packet.Write(Id);
            packet.Write(EmoteId);
        }

        public void ReadData(Packet packet) {
            Id = packet.ReadUShort();
            EmoteId = packet.ReadByte();
        }
    }
    
    public class ServerPlayerEmoteUpdate : IPacketData {
        
        public byte EmoteId { get; set; }
        
        public void WriteData(Packet packet) {
            packet.Write(EmoteId);
        }

        public void ReadData(Packet packet) {
            EmoteId = packet.ReadByte();
        }
    }
}