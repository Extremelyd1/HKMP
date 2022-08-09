using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Networking.Packet.Data {
    /// <summary>
    /// Packet data for a game settings update.
    /// </summary>
    internal class GameSettingsUpdate : IPacketData {
        // TODO: optimize this by only sending the values that actually changed

        /// <inheritdoc />
        public bool IsReliable => true;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => true;

        /// <summary>
        /// The game settings instance.
        /// </summary>
        public Game.Settings.GameSettings GameSettings { get; set; }

        /// <inheritdoc />
        public void WriteData(IPacket packet) {
            // Use reflection to loop over all properties and write their values to the packet
            foreach (var prop in GameSettings.GetType().GetProperties()) {
                if (!prop.CanRead) {
                    continue;
                }

                if (prop.PropertyType == typeof(bool)) {
                    packet.Write((bool)prop.GetValue(GameSettings, null));
                } else if (prop.PropertyType == typeof(byte)) {
                    packet.Write((byte)prop.GetValue(GameSettings, null));
                } else {
                    Logger.Info($"No write handler for property type: {prop.GetType()}");
                }
            }
        }

        /// <inheritdoc />
        public void ReadData(IPacket packet) {
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
                    Logger.Info($"No read handler for property type: {prop.GetType()}");
                }
            }
        }
    }
}