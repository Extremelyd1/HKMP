using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Packet.Update;

/// <summary>
/// Specialization of the update packet for server to client communication.
/// </summary>
internal class ClientUpdatePacket : UpdatePacket<ClientUpdatePacketId> {
    /// <inheritdoc />
    protected override IPacketData InstantiatePacketDataFromId(ClientUpdatePacketId packetId) {
        switch (packetId) {
            case ClientUpdatePacketId.Slice:
                return new SliceData();
            case ClientUpdatePacketId.SliceAck:
                return new SliceAckData();
            case ClientUpdatePacketId.ServerClientDisconnect:
                return new ServerClientDisconnect();
            case ClientUpdatePacketId.PlayerConnect:
                return new PacketDataCollection<PlayerConnect>();
            case ClientUpdatePacketId.PlayerDisconnect:
                return new PacketDataCollection<ClientPlayerDisconnect>();
            case ClientUpdatePacketId.PlayerEnterScene:
                return new PacketDataCollection<ClientPlayerEnterScene>();
            case ClientUpdatePacketId.PlayerAlreadyInScene:
                return new ClientPlayerAlreadyInScene();
            case ClientUpdatePacketId.PlayerLeaveScene:
                return new PacketDataCollection<ClientPlayerLeaveScene>();
            case ClientUpdatePacketId.PlayerUpdate:
                return new PacketDataCollection<PlayerUpdate>();
            case ClientUpdatePacketId.PlayerMapUpdate:
                return new PacketDataCollection<PlayerMapUpdate>();
            case ClientUpdatePacketId.EntitySpawn:
                return new PacketDataCollection<EntitySpawn>();
            case ClientUpdatePacketId.EntityUpdate:
                return new PacketDataCollection<EntityUpdate>();
            case ClientUpdatePacketId.ReliableEntityUpdate:
                return new PacketDataCollection<ReliableEntityUpdate>();
            case ClientUpdatePacketId.SceneHostTransfer:
                return new HostTransfer();
            case ClientUpdatePacketId.PlayerDeath:
                return new PacketDataCollection<GenericClientData>();
            case ClientUpdatePacketId.PlayerSetting:
                return new PacketDataCollection<ClientPlayerSettingUpdate>();
            case ClientUpdatePacketId.ServerSettingsUpdated:
                return new ServerSettingsUpdate();
            case ClientUpdatePacketId.ChatMessage:
                return new PacketDataCollection<ChatMessage>();
            case ClientUpdatePacketId.SaveUpdate:
                return new PacketDataCollection<SaveUpdate>();
            default:
                return new EmptyData();
        }
    }
}
