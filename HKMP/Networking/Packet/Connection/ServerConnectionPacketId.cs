namespace Hkmp.Networking.Packet.Connection;

/// <summary>
/// Enumeration of packet IDs for the connection packet for client to server communication.
/// </summary>
internal enum ServerConnectionPacketId {
    /// <summary>
    /// Information about the client that the server can use to determine whether to accept the connection.
    /// </summary>
    ClientInfo = 0,
}
