namespace Hkmp.Networking.Packet.Data {
    public class LoginResponse : IPacketData {
        public bool IsReliable => true;
        
        public bool DropReliableDataIfNewerExists => true;
        
        public void WriteData(Packet packet) {
            throw new System.NotImplementedException();
        }

        public void ReadData(Packet packet) {
            throw new System.NotImplementedException();
        }
    }
}