using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Packet.Connection;

/// <summary>
/// Packet that contains connection information for client to server communication.
/// </summary>
internal class ServerConnectionPacket : BasePacket<ServerConnectionPacketId> {
    /// <inheritdoc />
    protected override IPacketData InstantiatePacketDataFromId(ServerConnectionPacketId packetId) {
        switch (packetId) {
            default:
                return new EmptyData();
        }
    }
}
