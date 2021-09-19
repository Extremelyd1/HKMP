namespace Hkmp.Networking.Packet.Data {
    public class EmptyData : IPacketData {
        public bool IsReliable => false;
        public bool DropReliableDataIfNewerExists => false;
        public void WriteData(Packet packet) {
        }

        public void ReadData(Packet packet) {
        }
    }

    public class ReliableEmptyData : IPacketData {
        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => true;
        public void WriteData(Packet packet) {
        }

        public void ReadData(Packet packet) {
        }
    }
}