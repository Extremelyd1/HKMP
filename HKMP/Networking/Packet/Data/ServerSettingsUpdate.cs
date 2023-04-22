using Hkmp.Game.Settings;
using Hkmp.Logging;

namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data for a server settings update.
/// </summary>
internal class ServerSettingsUpdate : IPacketData {
    // TODO: optimize this by only sending the values that actually changed

    /// <inheritdoc />
    public bool IsReliable => true;

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists => true;

    /// <summary>
    /// The server settings instance.
    /// </summary>
    public ServerSettings ServerSettings { get; set; }

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
        // Use reflection to loop over all properties and write their values to the packet
        foreach (var prop in ServerSettings.GetType().GetProperties()) {
            if (!prop.CanRead) {
                continue;
            }

            if (prop.PropertyType == typeof(bool)) {
                packet.Write((bool) prop.GetValue(ServerSettings, null));
            } else if (prop.PropertyType == typeof(byte)) {
                packet.Write((byte) prop.GetValue(ServerSettings, null));
            } else {
                Logger.Error($"No write handler for property type: {prop.GetType()}");
            }
        }
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        ServerSettings = new ServerSettings();

        // Use reflection to loop over all properties and set their value by reading from the packet
        foreach (var prop in ServerSettings.GetType().GetProperties()) {
            if (!prop.CanWrite) {
                continue;
            }

            // ReSharper disable once OperatorIsCanBeUsed
            if (prop.PropertyType == typeof(bool)) {
                prop.SetValue(ServerSettings, packet.ReadBool(), null);
            } else if (prop.PropertyType == typeof(byte)) {
                prop.SetValue(ServerSettings, packet.ReadByte(), null);
            } else {
                Logger.Error($"No read handler for property type: {prop.GetType()}");
            }
        }
    }
}
