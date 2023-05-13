using System.Collections.Generic;

namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data for the hello client data.
/// </summary>
internal class HelloClient : IPacketData {
    /// <inheritdoc />
    public bool IsReliable => true;

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists => true;

    /// <summary>
    /// List of ID, username pairs for each connected client.
    /// </summary>
    public List<(ushort, string)> ClientInfo { get; set; }

    /// <summary>
    /// Construct the hello client data.
    /// </summary>
    public HelloClient() {
        ClientInfo = new List<(ushort, string)>();
    }

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
        packet.Write((ushort) ClientInfo.Count);

        foreach (var (id, username) in ClientInfo) {
            packet.Write(id);
            packet.Write(username);
        }
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        var length = packet.ReadUShort();

        for (var i = 0; i < length; i++) {
            ClientInfo.Add((
                packet.ReadUShort(),
                packet.ReadString()
            ));
        }
    }
}
