namespace Hkmp.Networking;

/// <summary>
/// Enumeration of connection result values.
/// </summary>
internal enum ServerConnectionResult {
    /// <summary>
    /// The client was accepted.
    /// </summary>
    Accepted = 0,
    /// <summary>
    /// The client is using different addons to the server.
    /// </summary>
    InvalidAddons,
    /// <summary>
    /// The client was rejected for other reasons.
    /// </summary>
    RejectedOther
}
