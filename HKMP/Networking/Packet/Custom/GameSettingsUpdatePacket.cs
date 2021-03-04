namespace HKMP.Networking.Packet.Custom {
    public class GameSettingsUpdatePacket : Packet, IPacket {

        public Game.Settings.GameSettings GameSettings { get; set; }

        public GameSettingsUpdatePacket() {
        }

        public GameSettingsUpdatePacket(Packet packet) : base(packet) {
        }
        
        public void CreatePacket() {
            Reset();

            Write(PacketId.GameSettingsUpdated);

            Write(GameSettings.IsPvpEnabled);
            Write(GameSettings.IsBodyDamageEnabled);
            
            WriteLength();
        }

        public void ReadPacket() {
            GameSettings = new Game.Settings.GameSettings {
                IsPvpEnabled = ReadBool(),
                IsBodyDamageEnabled = ReadBool()
            };

        }
    }
}