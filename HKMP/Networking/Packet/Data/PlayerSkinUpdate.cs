namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data for client-bound player skin update.
/// </summary>
internal class ClientPlayerSkinUpdate : GenericClientData {
    /// <summary>
    /// Whether the skin update is for the player receiving this packet.
    /// </summary>
    public bool Self { get; set; }

    /// <summary>
    /// The ID of the skin.
    /// </summary>
    public byte SkinId { get; set; }

    /// <summary>
    /// Construct the player skin update data.
    /// </summary>
    public ClientPlayerSkinUpdate() {
        IsReliable = true;
        DropReliableDataIfNewerExists = false;
    }

    /// <inheritdoc />
    public override void WriteData(IPacket packet) {
        packet.Write(Self);

        if (!Self) {
            packet.Write(Id);
        }

        packet.Write(SkinId);
    }

    /// <inheritdoc />
    public override void ReadData(IPacket packet) {
        Self = packet.ReadBool();

        if (!Self) {
            Id = packet.ReadUShort();
        }

        SkinId = packet.ReadByte();
    }
}
