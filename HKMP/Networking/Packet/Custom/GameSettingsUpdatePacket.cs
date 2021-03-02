namespace HKMP.Networking.Packet.Custom {
    public class GameSettingsUpdatePacket : Packet, IPacket {

        public bool IsPvpEnabled { get; set; }

        public GameSettingsUpdatePacket() {
        }

        public GameSettingsUpdatePacket(Packet packet) : base(packet) {
        }
        
        public void CreatePacket() {
            Reset();

            Write(PacketId.GameSettingsUpdated);

            Write(IsPvpEnabled);
            
            WriteLength();
        }

        public void ReadPacket() {
            IsPvpEnabled = ReadBool();
        }
    }
}