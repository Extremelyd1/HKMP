namespace Hkmp.Networking.Packet.Data {
    public class GenericClientData : IPacketData {
        public bool IsReliable { get; protected set; }
        
        public bool DropReliableDataIfNewerExists { get; protected set; }
        
        public ushort Id { get; set; }

        public virtual void WriteData(IPacket packet) {
            packet.Write(Id);
        }

        public virtual void ReadData(IPacket packet) {
            Id = packet.ReadUShort();
        }
    }
}