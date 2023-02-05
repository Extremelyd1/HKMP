using System.Collections.Generic;
using Hkmp.Game;
using Hkmp.Math;

namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data for the client-bound player enter scene data.
/// </summary>
internal class ClientPlayerEnterScene : GenericClientData {
    /// <summary>
    /// The username of the player.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// The position of the player.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// The scale of the player.
    /// </summary>
    public bool Scale { get; set; }

    /// <summary>
    /// The team of the player.
    /// </summary>
    public Team Team { get; set; }

    /// <summary>
    /// The ID of the skin of the player.
    /// </summary>
    public byte SkinId { get; set; }

    /// <summary>
    /// The ID of the animation clip of the player.
    /// </summary>
    public ushort AnimationClipId { get; set; }

    /// <summary>
    /// Construct the client player enter scene data.
    /// </summary>
    public ClientPlayerEnterScene() {
        IsReliable = true;
        DropReliableDataIfNewerExists = false;
    }

    /// <inheritdoc />
    public override void WriteData(IPacket packet) {
        packet.Write(Id);
        packet.Write(Username);

        packet.Write(Position);
        packet.Write(Scale);
        packet.Write((byte) Team);
        packet.Write(SkinId);

        packet.Write(AnimationClipId);
    }

    /// <inheritdoc />
    public override void ReadData(IPacket packet) {
        Id = packet.ReadUShort();
        Username = packet.ReadString();

        Position = packet.ReadVector2();
        Scale = packet.ReadBool();
        Team = (Team) packet.ReadByte();
        SkinId = packet.ReadByte();
        AnimationClipId = packet.ReadUShort();
    }
}

/// <summary>
/// Packet data for the client player already in scene data.
/// </summary>
internal class ClientPlayerAlreadyInScene : IPacketData {
    /// <inheritdoc />
    public bool IsReliable => true;

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists => false;

    /// <summary>
    /// List of client player enter scene data instances.
    /// </summary>
    public List<ClientPlayerEnterScene> PlayerEnterSceneList { get; }

    /// <summary>
    /// Whether the receiving player is scene host.
    /// </summary>
    public bool SceneHost { get; set; }

    /// <summary>
    /// Construct the client player already in scene data.
    /// </summary>
    public ClientPlayerAlreadyInScene() {
        PlayerEnterSceneList = new List<ClientPlayerEnterScene>();
    }

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
        var length = (byte) System.Math.Min(byte.MaxValue, PlayerEnterSceneList.Count);

        packet.Write(length);

        for (var i = 0; i < length; i++) {
            PlayerEnterSceneList[i].WriteData(packet);
        }

        packet.Write(SceneHost);
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        var length = packet.ReadByte();

        for (var i = 0; i < length; i++) {
            // Create new instance of generic type
            var instance = new ClientPlayerEnterScene();

            // Read the packet data into the instance
            instance.ReadData(packet);

            // And add it to our already initialized list
            PlayerEnterSceneList.Add(instance);
        }

        SceneHost = packet.ReadBool();
    }
}

/// <summary>
/// Packet data for the server-bound player enter scene data.
/// </summary>
internal class ServerPlayerEnterScene : IPacketData {
    /// <inheritdoc />
    public bool IsReliable => true;

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists => false;

    /// <summary>
    /// The new scene name that the player entered.
    /// </summary>
    public string NewSceneName { get; set; }

    /// <summary>
    /// The position of the player.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// The scale of the player.
    /// </summary>
    public bool Scale { get; set; }

    /// <summary>
    /// The ID of the animation clip of the player.
    /// </summary>
    public ushort AnimationClipId { get; set; }

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
        packet.Write(NewSceneName);

        packet.Write(Position);
        packet.Write(Scale);

        packet.Write(AnimationClipId);
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        NewSceneName = packet.ReadString();

        Position = packet.ReadVector2();
        Scale = packet.ReadBool();
        AnimationClipId = packet.ReadUShort();
    }
}
