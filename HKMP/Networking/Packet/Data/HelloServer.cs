using Hkmp.Math;

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

    /// <summary>
    /// The name of the current scene of the player. 
    /// </summary>
    public string SceneName { get; set; }

    /// <summary>
    /// The position of the player.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// The scale of the player.
    /// </summary>
    public bool Scale { get; set; }

    /// <summary>
    /// The animation clip ID of the player.
    /// </summary>
    public ushort AnimationClipId { get; set; }

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
        packet.Write(Username);
        packet.Write(SceneName);

        packet.Write(Position);
        packet.Write(Scale);

        packet.Write(AnimationClipId);
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        Username = packet.ReadString();
        SceneName = packet.ReadString();

        Position = packet.ReadVector2();
        Scale = packet.ReadBool();

        AnimationClipId = packet.ReadUShort();
    }
}
