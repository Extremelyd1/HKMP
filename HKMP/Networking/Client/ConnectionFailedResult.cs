using System.Collections.Generic;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Client;

/// <summary>
/// Class that encapsulates the result of a failed connection.
/// </summary>
internal class ConnectionFailedResult {
    /// <summary>
    /// The reason that the connection failed.
    /// </summary>
    public ConnectionFailedReason Reason { get; init; }
}

/// <summary>
/// Specialization class of <seealso cref="ConnectionFailedResult"/> for invalid addons.
/// </summary>
internal class ConnectionInvalidAddonsResult : ConnectionFailedResult {
    /// <summary>
    /// The list of addon data that the server uses to compare against for clients.
    /// </summary>
    public List<AddonData> AddonData { get; init; }
}

/// <summary>
/// Specialization class of <seealso cref="ConnectionFailedResult"/> for a generic failed connection with message.
/// </summary>
internal class ConnectionFailedMessageResult : ConnectionFailedResult {
    /// <summary>
    /// The string message that describes the reason the connection failed.
    /// </summary>
    public string Message { get; init; }
}

/// <summary>
/// Enumeration of reasons why the connection failed.
/// </summary>
internal enum ConnectionFailedReason {
    /// <summary>
    /// The client and server addon do not match.
    /// </summary>
    InvalidAddons,
    /// <summary>
    /// The connection timed out (took too long to establish).
    /// </summary>
    TimedOut,
    /// <summary>
    /// A socket exception occurred while trying to establish the connection.
    /// </summary>
    SocketException,
    /// <summary>
    /// An IO exception occurred while trying to establish the connection.
    /// </summary>
    IOException,
    /// <summary>
    /// The reason is miscellaneous.
    /// </summary>
    Other,
}
