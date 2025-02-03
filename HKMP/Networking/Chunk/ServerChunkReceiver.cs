using Hkmp.Networking.Server;

namespace Hkmp.Networking.Chunk;

internal class ServerChunkReceiver : ChunkReceiver {
    private readonly ServerUpdateManager _updateManager;

    public ServerChunkReceiver(ServerUpdateManager updateManager) {
        _updateManager = updateManager;
    }

    protected override void SendSliceAckData(byte chunkId, byte numSlicesMinusOne, bool[] acked) {
        _updateManager.SetSliceAckData(chunkId, numSlicesMinusOne, acked);
    }
}
