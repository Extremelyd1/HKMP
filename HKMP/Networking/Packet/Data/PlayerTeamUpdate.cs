using Hkmp.Game;

namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data of the client-bound player team update.
/// </summary>
internal class ClientPlayerTeamUpdate : GenericClientData {
    /// <summary>
    /// The username of the player.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// The team of the player.
    /// </summary>
    public Team Team { get; set; }

    /// <summary>
    /// Construct the player team update data.
    /// </summary>
    public ClientPlayerTeamUpdate() {
        IsReliable = true;
        DropReliableDataIfNewerExists = true;
    }

    /// <inheritdoc />
    public override void WriteData(IPacket packet) {
        packet.Write(Id);
        packet.Write(Username);
        packet.Write((byte) Team);
    }

    /// <inheritdoc />
    public override void ReadData(IPacket packet) {
        Id = packet.ReadUShort();
        Username = packet.ReadString();
        Team = (Team) packet.ReadByte();
    }
}

/// <summary>
/// Packet data for the server-bound player team update.
/// </summary>
internal class ServerPlayerTeamUpdate : IPacketData {
    /// <inheritdoc />
    public bool IsReliable => true;

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists => true;

    /// <summary>
    /// The team of the player.
    /// </summary>
    public Team Team { get; set; }

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
        packet.Write((byte) Team);
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        Team = (Team) packet.ReadByte();
    }
}
