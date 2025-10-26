using Hkmp.Networking.Client;

namespace Hkmp.Networking.Chunk;

/// <summary>
/// Specialization class of <see cref="ChunkSender"/> for the client-side chunk receiver.
/// </summary>
internal class ClientChunkSender : ChunkSender {
    /// <summary>
    /// The client update manager instance used for adding slice data to the update packet.
    /// </summary>
    private readonly ClientUpdateManager _updateManager;

    public ClientChunkSender(ClientUpdateManager updateManager) {
        _updateManager = updateManager;
    }

    /// <inheritdoc />
    protected override void SetSliceData(byte chunkId, byte sliceId, byte numSlices, byte[] data) {
        _updateManager.SetSliceData(chunkId, sliceId, numSlices, data);
    }
}
