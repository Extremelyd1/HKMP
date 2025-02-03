namespace Hkmp.Networking.Packet.Connection;

/// <summary>
/// Enumeration of packet IDs for connection packet for server to client communication.
/// </summary>
internal enum ClientConnectionPacketId {
    /// <summary>
    /// Information about the server meant for the client detailing whether the connection was accepted.
    /// </summary>
    ServerInfo,
}
