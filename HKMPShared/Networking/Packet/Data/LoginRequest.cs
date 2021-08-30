namespace Hkmp.Networking.Packet.Data {
    public class LoginRequest : IPacketData {
        public bool IsReliable => true;
        
        public bool DropReliableDataIfNewerExists => true;
        
        public string Username { get; set; }
        
        public void WriteData(Packet packet) {
            packet.Write(Username);
        }

        public void ReadData(Packet packet) {
            Username = packet.ReadString();
        }
    }
}