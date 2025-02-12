using Hkmp.Networking.Packet;

namespace Hkmp.Networking;

/// <summary>
/// Abstract base class that manages handling the initial connection to a server.
/// </summary>
internal abstract class ConnectionManager {
    /// <summary>
    /// The maximum size that a slice can be in bytes.
    /// </summary>
    public const int MaxSliceSize = 1024;

    /// <summary>
    /// The maximum number of slices in a chunk.
    /// </summary>
    public const int MaxSlicesPerChunk = 256;

    /// <summary>
    /// The maximum size of a chunk in bytes.
    /// </summary>
    public const int MaxChunkSize = MaxSliceSize * MaxSlicesPerChunk;

    /// <summary>
    /// The number of milliseconds a connection attempt can maximally take before being timed out.
    /// </summary>
    public const int TimeoutMillis = 60000;
    
    /// <summary>
    /// The packet manager instance to register handlers for slice and slice ack data.
    /// </summary>
    protected readonly PacketManager PacketManager;

    protected ConnectionManager(PacketManager packetManager) {
        PacketManager = packetManager;
    }

    /// <summary>
    /// Check whether the first ID is smaller than the second ID. Accounts for ID wrap-around, by inverse comparison
    /// if differences are larger than half of the ID number space.
    /// </summary>
    /// <param name="id1">The first ID as a byte.</param>
    /// <param name="id2">The second ID as a byte.</param>
    /// <returns>True if the first ID is smaller than the second ID, false otherwise.</returns>
    public static bool IsWrappingIdSmaller(byte id1, byte id2) {
        return id1 < id2 && id2 - id1 <= 128 || id1 > id2 && id1 - id2 > 128;
    }
}
