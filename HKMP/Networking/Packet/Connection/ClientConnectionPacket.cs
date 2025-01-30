using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Packet.Connection;

internal class ClientConnectionPacket : BasePacket<ClientConnectionPacketId> {
    /// <inheritdoc />
    protected override IPacketData InstantiatePacketDataFromId(ClientConnectionPacketId packetId) {
        switch (packetId) {
            default:
                return new EmptyData();
        }
    }
}
