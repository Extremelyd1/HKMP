using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Packet.Connection;

/// <summary>
/// Packet that contains connection information for server to client communication.
/// </summary>
internal class ClientConnectionPacket : BasePacket<ClientConnectionPacketId> {
    /// <inheritdoc />
    protected override IPacketData InstantiatePacketDataFromId(ClientConnectionPacketId packetId) {
        switch (packetId) {
            default:
                return new EmptyData();
        }
    }
}
