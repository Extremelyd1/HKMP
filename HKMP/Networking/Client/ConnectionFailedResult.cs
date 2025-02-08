using System.Collections.Generic;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Client;

/// <summary>
/// Class that encapsulates the result of a failed connection.
/// </summary>
internal class ConnectionFailedResult {
    public ConnectionFailedReason Reason { get; set; }
}

/// <summary>
/// Specialization class of <seealso cref="ConnectionFailedResult"/> for invalid addons.
/// </summary>
internal class ConnectionInvalidAddonsResult : ConnectionFailedResult {
    public List<AddonData> AddonData { get; set; }
}

/// <summary>
/// Specialization class of <seealso cref="ConnectionFailedResult"/> for a generic failed connection with message.
/// </summary>
internal class ConnectionFailedMessageResult : ConnectionFailedResult {
    public string Message { get; set; }
}

/// <summary>
/// Enumeration of reasons why the connection failed.
/// </summary>
internal enum ConnectionFailedReason {
    InvalidAddons,
    Other,
    TimedOut,
    SocketException,
    IOException
}
