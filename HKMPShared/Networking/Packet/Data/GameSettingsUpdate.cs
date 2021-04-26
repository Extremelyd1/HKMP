namespace HKMP.Networking.Packet.Data {
    public class GameSettingsUpdate : IPacketData {
        // TODO: optimize this by only sending the values that actually changed

        public Game.Settings.GameSettings GameSettings { get; set; }

        public void WriteData(Packet packet) {
            // Use reflection to loop over all properties and write their values to the packet
            foreach (var prop in GameSettings.GetType().GetProperties()) {
                if (!prop.CanRead) {
                    continue;
                }

                if (prop.PropertyType == typeof(bool)) {
                    packet.Write((bool) prop.GetValue(GameSettings, null));
                } else if (prop.PropertyType == typeof(byte)) {
                    packet.Write((byte) prop.GetValue(GameSettings, null));
                } else {
                    Logger.Get().Warn(this, $"No write handler for property type: {prop.GetType()}");
                }
            }
        }

        public void ReadData(Packet packet) {
            GameSettings = new Game.Settings.GameSettings();

            // Use reflection to loop over all properties and set their value by reading from the packet
            foreach (var prop in GameSettings.GetType().GetProperties()) {
                if (!prop.CanWrite) {
                    continue;
                }

                // ReSharper disable once OperatorIsCanBeUsed
                if (prop.PropertyType == typeof(bool)) {
                    prop.SetValue(GameSettings, packet.ReadBool(), null);
                } else if (prop.PropertyType == typeof(byte)) {
                    prop.SetValue(GameSettings, packet.ReadByte(), null);
                } else {
                    Logger.Get().Warn(this, $"No read handler for property type: {prop.GetType()}");
                }
            }
        }
    }
}