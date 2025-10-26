namespace Hkmp.Networking.Client;

/// <summary>
/// Enumeration of connection statuses for the client.
/// </summary>
internal enum ClientConnectionStatus {
    /// <summary>
    /// Not connected to any server.
    /// </summary>
    NotConnected,
    /// <summary>
    /// Trying to establish a connection to a server.
    /// </summary>
    Connecting,
    /// <summary>
    /// Connected to a server.
    /// </summary>
    Connected
}
