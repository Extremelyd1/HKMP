namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data for client-bound player skin update.
/// </summary>
internal class ClientPlayerSkinUpdate : GenericClientData {
    /// <summary>
    /// The ID of the skin.
    /// </summary>
    public byte SkinId { get; set; }

    /// <summary>
    /// Construct the player skin update data.
    /// </summary>
    public ClientPlayerSkinUpdate() {
        IsReliable = true;
        DropReliableDataIfNewerExists = true;
    }

    /// <inheritdoc />
    public override void WriteData(IPacket packet) {
        packet.Write(Id);
        packet.Write(SkinId);
    }

    /// <inheritdoc />
    public override void ReadData(IPacket packet) {
        Id = packet.ReadUShort();
        SkinId = packet.ReadByte();
    }
}

/// <summary>
/// Packet data for the server-bound player skin update.
/// </summary>
internal class ServerPlayerSkinUpdate : IPacketData {
    /// <inheritdoc />
    public bool IsReliable => true;

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists => true;

    /// <summary>
    /// The ID of the skin.
    /// </summary>
    public byte SkinId { get; set; }

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
        packet.Write(SkinId);
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        SkinId = packet.ReadByte();
    }
}
