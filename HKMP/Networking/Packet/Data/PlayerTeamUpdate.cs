using Hkmp.Game;

namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data of the client-bound player team update.
/// </summary>
internal class ClientPlayerTeamUpdate : GenericClientData {
    /// <summary>
    /// Whether the team update is for the player receiving the packet.
    /// </summary>
    public bool Self { get; set; }

    /// <summary>
    /// The team of the player.
    /// </summary>
    public Team Team { get; set; }

    public ClientPlayerTeamUpdate() {
        IsReliable = true;
        DropReliableDataIfNewerExists = false;
    }

    /// <inheritdoc />
    public override void WriteData(IPacket packet) {
        packet.Write(Self);

        if (!Self) {
            packet.Write(Id);
        }

        packet.Write((byte) Team);
    }

    /// <inheritdoc />
    public override void ReadData(IPacket packet) {
        Self = packet.ReadBool();

        if (!Self) {
            Id = packet.ReadUShort();
        }

        Team = (Team) packet.ReadByte();
    }
}
