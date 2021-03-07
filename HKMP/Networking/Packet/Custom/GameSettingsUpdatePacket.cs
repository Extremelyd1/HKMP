namespace HKMP.Networking.Packet.Custom {
    public class GameSettingsUpdatePacket : Packet, IPacket {

        public Game.Settings.GameSettings GameSettings { get; set; }

        public GameSettingsUpdatePacket() {
        }

        public GameSettingsUpdatePacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();

            Write(PacketId.GameSettingsUpdated);

            // Use reflection to loop over all properties and write their values to the packet
            foreach (var prop in GameSettings.GetType().GetProperties()) {
                if (!prop.CanRead) {
                    continue;
                }

                Write((bool) prop.GetValue(GameSettings, null));
            }
            
            WriteLength();

            return this;
        }

        public void ReadPacket() {
            GameSettings = new Game.Settings.GameSettings();

            // Use reflection to loop over all properties and set their value by reading from the packet
            foreach (var prop in GameSettings.GetType().GetProperties()) {
                if (!prop.CanWrite) {
                    continue;
                }

                // ReSharper disable once OperatorIsCanBeUsed
                if (prop.PropertyType == typeof(bool)) {
                    prop.SetValue(GameSettings, ReadBool(), null);
                } else {
                    Logger.Warn(this, $"No handler for property type: {prop.GetType()}");
                }
            }

        }
    }
}