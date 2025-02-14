using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Packet.Update;

/// <summary>
/// Specialization of the update packet for client to server communication.
/// </summary>
internal class ServerUpdatePacket : UpdatePacket<ServerUpdatePacketId> {
    /// <inheritdoc />
    protected override IPacketData InstantiatePacketDataFromId(ServerUpdatePacketId packetId) {
        switch (packetId) {
            case ServerUpdatePacketId.Slice:
                return new SliceData();
            case ServerUpdatePacketId.SliceAck:
                return new SliceAckData();
            case ServerUpdatePacketId.PlayerUpdate:
                return new PlayerUpdate();
            case ServerUpdatePacketId.PlayerMapUpdate:
                return new PlayerMapUpdate();
            case ServerUpdatePacketId.EntitySpawn:
                return new PacketDataCollection<EntitySpawn>();
            case ServerUpdatePacketId.EntityUpdate:
                return new PacketDataCollection<EntityUpdate>();
            case ServerUpdatePacketId.ReliableEntityUpdate:
                return new PacketDataCollection<ReliableEntityUpdate>();
            case ServerUpdatePacketId.PlayerEnterScene:
                return new ServerPlayerEnterScene();
            case ServerUpdatePacketId.ChatMessage:
                return new ChatMessage();
            case ServerUpdatePacketId.SaveUpdate:
                return new PacketDataCollection<SaveUpdate>();
            default:
                return new EmptyData();
        }
    }
}
