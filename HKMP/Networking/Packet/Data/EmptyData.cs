namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data for empty data.
/// </summary>
public sealed class EmptyData : IPacketData {
    /// <inheritdoc />
    public bool IsReliable => false;

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists => false;

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
    }
}

/// <summary>
/// Packet data for empty data that should be reliable.
/// </summary>
public sealed class ReliableEmptyData : IPacketData {
    /// <inheritdoc />
    public bool IsReliable => true;

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists => true;

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
    }
}
