using System;
using Hkmp.Logging;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Chunk;

internal abstract class ChunkReceiver {
    private readonly bool[] _received;
    private readonly byte[] _chunkData;

    private bool _isReceiving;
    private byte _chunkId;
    private int _chunkSize;
    private int _numSlices;
    private int _numReceivedSlices;

    public event Action<Packet.Packet> ChunkReceivedEvent;

    protected ChunkReceiver() {
        _received = new bool[ConnectionManager.MaxSlicesPerChunk];
        _chunkData = new byte[ConnectionManager.MaxChunkSize];
    }

    public void ProcessReceivedData(SliceData sliceData) {
        Logger.Debug($"Received slice packet: {sliceData.ChunkId}, {sliceData.NumSlices}");

        if (_chunkId != sliceData.ChunkId) {
            Logger.Debug("Chunk ID of received slice packet does not corresponding with currently receiving chunk");
            return;
        }

        if (!_isReceiving) {
            _isReceiving = true;
            _numSlices = sliceData.NumSlices;
        } else {
            if (_numSlices != sliceData.NumSlices) {
                Logger.Debug("Number of slices in slice packet does not correspond with local number of slices");
                return;
            }
        }

        if (_received[sliceData.SliceId]) {
            Logger.Debug($"Received duplicate slice: {sliceData.SliceId}, ignoring");
            return;
        }

        _numReceivedSlices += 1;
        _received[sliceData.SliceId] = true;

        Array.Copy(
            sliceData.Data, 
            0, 
            _chunkData, 
            sliceData.SliceId * ConnectionManager.MaxSliceSize, 
            sliceData.Data.Length
        );
        
        SendAckPacket();

        // If this is the last slice in the chunk, we can calculate the chunk size
        if (sliceData.SliceId == _numSlices - 1) {
            _chunkSize = (_numSlices - 1) * ConnectionManager.MaxSliceSize + sliceData.Data.Length;
        }

        if (_numReceivedSlices == _numSlices) {
            var byteArray = new byte[_chunkSize];
            Array.Copy(
                _chunkData,
                0,
                byteArray,
                0,
                _chunkSize
            );
            var packet = new Packet.Packet(byteArray);
            
            ChunkReceivedEvent?.Invoke(packet);
            
            _chunkId += 1;
            _isReceiving = false;
            
            Reset();
        }
    }

    private void SendAckPacket() {
        var acked = new bool[_numSlices];
        Array.Copy(_received, acked, _numSlices);

        SendSliceAckData(_chunkId, (byte) (_numSlices - 1), acked);
    }

    private void Reset() {
        for (var i = 0; i < ConnectionManager.MaxSlicesPerChunk; i++) {
            _received[i] = false;
        }

        _chunkSize = 0;
        _numSlices = 0;
        _numReceivedSlices = 0;
    }

    protected abstract void SendSliceAckData(byte chunkId, byte numSlicesMinusOne, bool[] acked);
}
