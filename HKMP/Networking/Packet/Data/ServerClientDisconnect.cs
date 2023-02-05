namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data from server to client to tell the client that they are disconnected (kicked, banned,
/// server shutdown, etc).
/// </summary>
internal class ServerClientDisconnect : IPacketData {
    /// <inheritdoc/>
    public bool IsReliable => true;

    /// <inheritdoc/>
    public bool DropReliableDataIfNewerExists => false;

    /// <summary>
    /// The reason for the disconnect.
    /// </summary>
    public DisconnectReason Reason { get; set; }

    /// <inheritdoc/>
    public void WriteData(IPacket packet) {
        packet.Write((byte) Reason);
    }

    /// <inheritdoc/>
    public void ReadData(IPacket packet) {
        Reason = (DisconnectReason) packet.ReadByte();
    }
}

/// <summary>
/// The reason for the disconnect from the server.
/// </summary>
public enum DisconnectReason {
    /// <summary>
    /// When the server is shut down and clients need to properly disconnected.
    /// </summary>
    Shutdown = 0,

    /// <summary>
    /// When the client is kicked from the server.
    /// </summary>
    Kicked,

    /// <summary>
    /// When the client is banned from the server.
    /// </summary>
    Banned
}
