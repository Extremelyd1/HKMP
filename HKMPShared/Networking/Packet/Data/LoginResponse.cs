namespace Hkmp.Networking.Packet.Data {
    public class LoginResponse : IPacketData {
        public bool IsReliable => true;
        
        public bool DropReliableDataIfNewerExists => true;
        
        public LoginResponseStatus LoginResponseStatus { get; set; }
        
        public void WriteData(Packet packet) {
            packet.Write((byte) LoginResponseStatus);
        }

        public void ReadData(Packet packet) {
            LoginResponseStatus = (LoginResponseStatus) packet.ReadByte();
        }
    }
    
    public enum LoginResponseStatus {
        // When the request has been approved and connection is a success
        Success = 0,
    }
}