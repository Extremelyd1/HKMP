namespace Hkmp.Networking.Packet.Data {
    public class GenericClientData : IPacketData {
        public bool IsReliable { get; protected set; }
        
        public bool DropReliableDataIfNewerExists { get; protected set; }
        
        public ushort Id { get; set; }

        public virtual void WriteData(Packet packet) {
            packet.Write(Id);
        }

        public virtual void ReadData(Packet packet) {
            Id = packet.ReadUShort();
        }
    }
}