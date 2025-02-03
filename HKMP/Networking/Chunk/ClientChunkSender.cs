using Hkmp.Networking.Client;

namespace Hkmp.Networking.Chunk;

internal class ClientChunkSender : ChunkSender {
    private readonly ClientUpdateManager _updateManager;

    public ClientChunkSender(ClientUpdateManager updateManager) {
        _updateManager = updateManager;
    }

    protected override void SendSliceData(byte chunkId, byte sliceId, byte numSlices, byte[] data) {
        _updateManager.SetSliceData(chunkId, sliceId, numSlices, data);
    }
}
