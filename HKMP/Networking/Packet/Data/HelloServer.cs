namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data for the hello server data.
/// </summary>
internal class HelloServer : IPacketData {
    /// <inheritdoc />
    public bool IsReliable => true;

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists => true;

    /// <summary>
    /// The username of the player.
    /// </summary>
    public string Username { get; set; }

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
        packet.Write(Username);
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        Username = packet.ReadString();
    }
}
