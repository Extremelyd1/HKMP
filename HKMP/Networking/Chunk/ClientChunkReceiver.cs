using Hkmp.Networking.Client;

namespace Hkmp.Networking.Chunk;

/// <summary>
/// Specialization class of <see cref="ChunkReceiver"/> for the client-side chunk receiver.
/// </summary>
internal class ClientChunkReceiver : ChunkReceiver {
    /// <summary>
    /// The client update manager instance used for adding slice ack data to the update packet.
    /// </summary>
    private readonly ClientUpdateManager _updateManager;

    public ClientChunkReceiver(ClientUpdateManager updateManager) {
        _updateManager = updateManager;
    }

    /// <inheritdoc />
    protected override void SetSliceAckData(byte chunkId, ushort numSlices, bool[] acked) {
        _updateManager.SetSliceAckData(chunkId, numSlices, acked);
    }
}
