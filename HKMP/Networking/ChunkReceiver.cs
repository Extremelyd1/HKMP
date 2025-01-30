using System;
using Hkmp.Logging;
using Hkmp.Networking.Packet.Connection;
using Org.BouncyCastle.Tls;

namespace Hkmp.Networking;

internal class ChunkReceiver {
    private readonly DtlsTransport _dtlsTransport;
    
    private readonly bool[] _received;
    private readonly byte[] _chunkData;

    private bool _isReceiving;
    private byte _chunkId;
    private int _chunkSize;
    private int _numSlices;
    private int _numReceivedSlices;

    public ChunkReceiver(DtlsTransport dtlsTransport) {
        _dtlsTransport = dtlsTransport;

        _received = new bool[ConnectionManager.MaxSlicesPerChunk];
        _chunkData = new byte[ConnectionManager.MaxChunkSize];
    }

    public void ProcessReceivedPacket(SlicePacket packet) {
        Logger.Debug($"Received slice packet: {packet.ChunkId}, {packet.NumSlices}");

        if (_chunkId != packet.ChunkId) {
            Logger.Debug("Chunk ID of received slice packet does not corresponding with currently receiving chunk");
            return;
        }

        if (!_isReceiving) {
            _isReceiving = true;
            _numSlices = packet.NumSlices;
        } else {
            if (_numSlices != packet.NumSlices) {
                Logger.Debug("Number of slices in slice packet does not correspond with local number of slices");
                return;
            }
        }

        if (_received[packet.SliceId]) {
            Logger.Debug($"Received duplicate slice: {packet.SliceId}, ignoring");
            return;
        }

        _numReceivedSlices += 1;
        _received[packet.SliceId] = true;

        Array.Copy(
            packet.Data, 
            0, 
            _chunkData, 
            packet.SliceId * ConnectionManager.MaxSliceSize, 
            packet.Data.Length
        );

        // If this is the last slice in the chunk, we can calculate the chunk size
        if (packet.SliceId == _numSlices - 1) {
            _chunkSize = (_numSlices - 1) * ConnectionManager.MaxSliceSize + packet.Data.Length;
        }

        if (_numReceivedSlices == _numSlices) {
            _chunkId += 1;
            _isReceiving = false;
            
            Reset();
        }
    }

    private void SendAckPacket() {
        
    }

    private void Reset() {
        for (var i = 0; i < ConnectionManager.MaxSlicesPerChunk; i++) {
            _received[i] = false;
        }

        _chunkSize = 0;
        _numSlices = 0;
        _numReceivedSlices = 0;
    }
}
