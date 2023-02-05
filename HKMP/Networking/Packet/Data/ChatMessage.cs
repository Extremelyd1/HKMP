namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data for a chat message.
/// </summary>
internal class ChatMessage : IPacketData {
    /// <summary>
    /// The maximum length of a chat message.
    /// </summary>
    public const byte MaxMessageLength = byte.MaxValue;

    /// <inheritdoc />
    public bool IsReliable => true;

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists => false;

    /// <summary>
    /// The message string.
    /// </summary>
    public string Message { get; set; }

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
        packet.Write(Message);
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        Message = packet.ReadString();
    }
}
