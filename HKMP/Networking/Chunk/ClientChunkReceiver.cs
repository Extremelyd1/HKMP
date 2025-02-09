using Hkmp.Networking.Client;

namespace Hkmp.Networking.Chunk;

internal class ClientChunkReceiver : ChunkReceiver {
    private readonly ClientUpdateManager _updateManager;

    public ClientChunkReceiver(ClientUpdateManager updateManager) {
        _updateManager = updateManager;
    }

    protected override void SendSliceAckData(byte chunkId, ushort numSlices, bool[] acked) {
        _updateManager.SetSliceAckData(chunkId, numSlices, acked);
    }
}
