namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data for generic data at least containing a client ID.
/// </summary>
internal class GenericClientData : IPacketData {
    /// <inheritdoc />
    public bool IsReliable { get; protected set; }

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists { get; protected set; }

    /// <summary>
    /// The ID of the client.
    /// </summary>
    public ushort Id { get; set; }

    /// <inheritdoc />
    public virtual void WriteData(IPacket packet) {
        packet.Write(Id);
    }

    /// <inheritdoc />
    public virtual void ReadData(IPacket packet) {
        Id = packet.ReadUShort();
    }
}
