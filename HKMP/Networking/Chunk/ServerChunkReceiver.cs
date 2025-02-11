using Hkmp.Networking.Server;

namespace Hkmp.Networking.Chunk;

/// <summary>
/// Specialization class of <see cref="ChunkReceiver"/> for the server-side chunk receiver.
/// </summary>
internal class ServerChunkReceiver : ChunkReceiver {
    /// <summary>
    /// The server update manager instance used for adding slice ack data to the update packet.
    /// </summary>
    private readonly ServerUpdateManager _updateManager;

    public ServerChunkReceiver(ServerUpdateManager updateManager) {
        _updateManager = updateManager;
    }

    /// <inheritdoc />
    protected override void SetSliceAckData(byte chunkId, ushort numSlices, bool[] acked) {
        _updateManager.SetSliceAckData(chunkId, numSlices, acked);
    }
}
