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

                if (prop.PropertyType == typeof(bool)) {
                    Write((bool) prop.GetValue(GameSettings, null));
                } else if (prop.PropertyType == typeof(int)) {
                    Write((int) prop.GetValue(GameSettings, null));
                } else {
                    Logger.Warn(this, $"No write handler for property type: {prop.GetType()}");
                }
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
                } else if (prop.PropertyType == typeof(int)) {
                    prop.SetValue(GameSettings, ReadInt(), null);
                } else {
                    Logger.Warn(this, $"No read handler for property type: {prop.GetType()}");
                }
            }

        }
    }
}