namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data for the player connect data.
/// </summary>
internal class PlayerConnect : GenericClientData {
    /// <summary>
    /// The username of the connecting player.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Construct the player connect data.
    /// </summary>
    public PlayerConnect() {
        IsReliable = true;
        DropReliableDataIfNewerExists = false;
    }

    /// <inheritdoc />
    public override void WriteData(IPacket packet) {
        packet.Write(Id);
        packet.Write(Username);
    }

    /// <inheritdoc />
    public override void ReadData(IPacket packet) {
        Id = packet.ReadUShort();
        Username = packet.ReadString();
    }
}
