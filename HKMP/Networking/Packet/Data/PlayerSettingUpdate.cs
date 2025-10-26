using System.Collections.Generic;
using Hkmp.Game;

namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data for client-bound player setting update.
/// </summary>
internal class ClientPlayerSettingUpdate : GenericClientData {
    /// <summary>
    /// Whether the update is for the player receiving this packet.
    /// </summary>
    public bool Self { get; set; }

    /// <summary>
    /// Set of the types of updates that this packet contains. For example, only a skin update, or a skin and a team
    /// update.
    /// </summary>
    public ISet<PlayerSettingUpdateType> UpdateTypes { get; set; }

    /// <summary>
    /// The team of the player.
    /// </summary>
    public Team Team { get; set; }

    /// <summary>
    /// The ID of the skin.
    /// </summary>
    public byte SkinId { get; set; }

    public ClientPlayerSettingUpdate() {
        UpdateTypes = new HashSet<PlayerSettingUpdateType>();
    }

    /// <inheritdoc />
    public override void WriteData(IPacket packet) {
        packet.Write(Self);

        if (!Self) {
            packet.Write(Id);
        }

        packet.WriteBitFlag(UpdateTypes);

        if (UpdateTypes.Contains(PlayerSettingUpdateType.Team)) {
            packet.Write((byte) Team);
        }

        if (UpdateTypes.Contains(PlayerSettingUpdateType.Skin)) {
            packet.Write(SkinId);
        }
    }

    /// <inheritdoc />
    public override void ReadData(IPacket packet) {
        Self = packet.ReadBool();

        if (!Self) {
            Id = packet.ReadUShort();
        }

        UpdateTypes = packet.ReadBitFlag<PlayerSettingUpdateType>();

        if (UpdateTypes.Contains(PlayerSettingUpdateType.Team)) {
            Team = (Team) packet.ReadByte();
        }

        if (UpdateTypes.Contains(PlayerSettingUpdateType.Skin)) {
            SkinId = packet.ReadByte();
        }
    }
}

/// <summary>
/// Packet data for server-bound player setting update.
/// </summary>
internal class ServerPlayerSettingUpdate : IPacketData {
    /// <inheritdoc />
    public bool IsReliable => true;

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists => true;
    
    /// <summary>
    /// Set of the types of updates that this packet contains. For example, only a skin update, or a skin and a team
    /// update.
    /// </summary>
    public ISet<PlayerSettingUpdateType> UpdateTypes { get; set; }

    /// <summary>
    /// The team of the player.
    /// </summary>
    public Team Team { get; set; }

    /// <summary>
    /// The ID of the skin.
    /// </summary>
    public byte SkinId { get; set; }

    public ServerPlayerSettingUpdate() {
        UpdateTypes = new HashSet<PlayerSettingUpdateType>();
    }
    
    /// <inheritdoc />
    public void WriteData(IPacket packet) {
        packet.WriteBitFlag(UpdateTypes);

        if (UpdateTypes.Contains(PlayerSettingUpdateType.Team)) {
            packet.Write((byte) Team);
        }

        if (UpdateTypes.Contains(PlayerSettingUpdateType.Skin)) {
            packet.Write(SkinId);
        }
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        UpdateTypes = packet.ReadBitFlag<PlayerSettingUpdateType>();

        if (UpdateTypes.Contains(PlayerSettingUpdateType.Team)) {
            Team = (Team) packet.ReadByte();
        }

        if (UpdateTypes.Contains(PlayerSettingUpdateType.Skin)) {
            SkinId = packet.ReadByte();
        }
    }
}

/// <summary>
/// Enum for the type of player setting update.
/// </summary>
internal enum PlayerSettingUpdateType {
    Team = 0,
    Skin = 1
}
