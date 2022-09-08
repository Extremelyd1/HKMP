namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data for the player map update.
/// </summary>
internal class PlayerMapUpdate : GenericClientData {
    /// <summary>
    /// Whether the player has an active icon. If false, then there is no map position
    /// transmit anymore.
    /// </summary>
    public bool HasIcon { get; set; }

    public PlayerMapUpdate() {
        IsReliable = true;
        DropReliableDataIfNewerExists = true;
    }

    /// <inheritdoc />
    public override void WriteData(IPacket packet) {
        packet.Write(Id);

        packet.Write(HasIcon);
    }

    /// <inheritdoc />
    public override void ReadData(IPacket packet) {
        Id = packet.ReadUShort();

        HasIcon = packet.ReadBool();
    }
}
