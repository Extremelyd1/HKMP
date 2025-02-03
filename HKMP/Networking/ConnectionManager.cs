using Hkmp.Networking.Packet;

namespace Hkmp.Networking;

/// <summary>
/// Class that manages sending packets while establishing connection to a server.
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
    /// The packet manager instance to register handlers for slice and slice ack data.
    /// </summary>
    protected readonly PacketManager PacketManager;

    protected ConnectionManager(PacketManager packetManager) {
        PacketManager = packetManager;
    }
}
