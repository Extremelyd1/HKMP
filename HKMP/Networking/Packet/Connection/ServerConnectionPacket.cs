using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Packet.Connection;

internal class ServerConnectionPacket : BasePacket<ServerConnectionPacketId> {
    /// <inheritdoc />
    protected override IPacketData InstantiatePacketDataFromId(ServerConnectionPacketId packetId) {
        switch (packetId) {
            default:
                return new EmptyData();
        }
    }
}
