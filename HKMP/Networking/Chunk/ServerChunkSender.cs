using Hkmp.Networking.Server;

namespace Hkmp.Networking.Chunk;

/// <summary>
/// Specialization class of <see cref="ChunkSender"/> for the server-side chunk receiver.
/// </summary>
internal class ServerChunkSender : ChunkSender {
    /// <summary>
    /// The server update manager instance used for adding slice data to the update packet.
    /// </summary>
    private readonly ServerUpdateManager _updateManager;

    public ServerChunkSender(ServerUpdateManager updateManager) {
        _updateManager = updateManager;
    }

    /// <inheritdoc />
    protected override void SetSliceData(byte chunkId, byte sliceId, byte numSlices, byte[] data) {
        _updateManager.SetSliceData(chunkId, sliceId, numSlices, data);
    }
}
