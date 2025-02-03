using Hkmp.Networking.Server;

namespace Hkmp.Networking.Chunk;

internal class ServerChunkSender : ChunkSender {
    private readonly ServerUpdateManager _updateManager;

    public ServerChunkSender(ServerUpdateManager updateManager) {
        _updateManager = updateManager;
    }

    protected override void SendSliceData(byte chunkId, byte sliceId, byte numSlices, byte[] data) {
        _updateManager.SetSliceData(chunkId, sliceId, numSlices, data);
    }
}
