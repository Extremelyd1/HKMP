using System;
using Hkmp.Logging;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Chunk;

/// <summary>
/// Class that processes and manages chunks by receiving slices of those chunks and sending acknowledgements for those
/// slices.
/// </summary>
internal abstract class ChunkReceiver {
    /// <summary>
    /// Boolean array where each value indicates whether the slice of the same index was received.
    /// </summary>
    private readonly bool[] _received;
    /// <summary>
    /// Byte array that contains (parts of) the chunk data that is received.
    /// </summary>
    private readonly byte[] _chunkData;

    /// <summary>
    /// Whether we are currently receiving a chunk. If not, receiving a slice containing a chunk ID that is one higher
    /// that the last received chunk will start the reception process again.
    /// </summary>
    private bool _isReceiving;
    /// <summary>
    /// The currently (if receiving) or last received (when not receiving) chunk ID.
    /// </summary>
    private byte _chunkId = 255;
    /// <summary>
    /// The size of the chunk that we are currently receiving. Only calculated when the last slice is received, since
    /// that is the only slice with a different slice size.
    /// </summary>
    private int _chunkSize;
    /// <summary>
    /// The number of slices that the chunk we are currently receiving contains. Set whenever we receive the first
    /// slice in a chunk.
    /// </summary>
    private int _numSlices;
    /// <summary>
    /// The number of slices we have received so far. Used to keep track when all slices are received.
    /// </summary>
    private int _numReceivedSlices;

    /// <summary>
    /// Event that is called when the entirety of a chunk is received.
    /// </summary>
    public event Action<Packet.Packet> ChunkReceivedEvent;

    /// <summary>
    /// Construct the chunk receiver by allocating the readonly arrays with their maximally used lengths.
    /// </summary>
    protected ChunkReceiver() {
        _received = new bool[ConnectionManager.MaxSlicesPerChunk];
        _chunkData = new byte[ConnectionManager.MaxChunkSize];
    }

    /// <summary>
    /// Process received slice data by checking whether we have not yet received this slice and adding it to the data
    /// array and marking it received. If this is the first slice received in this chunk we note that we are
    /// receiving, set the number of slices we expect to receive and increment the currently receiving chunk ID.
    /// If this is the last slice in the chunk we invoke the event that an entire chunk is received.
    /// </summary>
    /// <param name="sliceData">The received slice data.</param>
    public void ProcessReceivedData(SliceData sliceData) {
        Logger.Debug($"Received slice packet: {sliceData.ChunkId}, {sliceData.SliceId}, {sliceData.NumSlices}");

        // We check if the received chunk ID is smaller than the current chunk ID accounting for wrapping IDs
        if (ConnectionManager.IsWrappingIdSmaller(sliceData.ChunkId, _chunkId)) {
            Logger.Debug("Chunk ID of received slice packet is smaller than currently receiving chunk");
            return;
        }

        if (!_isReceiving) {
            if (sliceData.ChunkId == (byte) (_chunkId + 1)) {
                Logger.Debug($"Received new chunk with ID: {sliceData.ChunkId}");
                SoftReset();
                
                _chunkId += 1;
                _isReceiving = true;
                _numSlices = sliceData.NumSlices;
            } else if (sliceData.ChunkId == _chunkId) {
                Logger.Debug("Already received all slices, resending ack packet");
                SendAckData();
                return;
            } else {
                Logger.Debug($"Received old chunk: {_chunkId}, ignoring");
                return;
            }
        } else {
            // If the received number of slices does not match the number slices we are keeping track of, we discard
            // the slice altogether as it is likely not correct
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

        // Copy over the data from the received slice into the chunk data array at the correct position
        Array.Copy(
            sliceData.Data, 
            0, 
            _chunkData, 
            sliceData.SliceId * ConnectionManager.MaxSliceSize, 
            sliceData.Data.Length
        );
        
        SendAckData();

        // If this is the last slice in the chunk, we can calculate the chunk size
        if (sliceData.SliceId == _numSlices - 1) {
            _chunkSize = (_numSlices - 1) * ConnectionManager.MaxSliceSize + sliceData.Data.Length;
            Logger.Debug($"Received last slice in chunk, chunk size: {_chunkSize}");
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

            _isReceiving = false;
        }
    }

    /// <summary>
    /// Reset the chunk receiver so it can be used for a new connection. This will reset most variables to their
    /// default values.
    /// </summary>
    public void Reset() {
        SoftReset();
        
        _isReceiving = false;
        _chunkId = 255;
    }

    /// <summary>
    /// Send acknowledgement data containing the boolean array of all slices that have been acknowledged thus far.
    /// </summary>
    private void SendAckData() {
        var acked = new bool[_numSlices];
        Array.Copy(_received, acked, _numSlices);

        SetSliceAckData(_chunkId, (ushort) _numSlices, acked);
    }

    /// <summary>
    /// Soft reset the chunk receiver by clearing the array of received slices and setting chunk size, number of
    /// slices, and number of received slices to 0.
    /// </summary>
    private void SoftReset() {
        Array.Clear(_received, 0, _received.Length);

        _chunkSize = 0;
        _numSlices = 0;
        _numReceivedSlices = 0;
    }

    /// <summary>
    /// Set the slice ack data in the corresponding update manager for sending.
    /// </summary>
    /// <param name="chunkId">The ID of the chunk for this acknowledgement.</param>
    /// <param name="numSlices">The number of slices in this chunk.</param>
    /// <param name="acked">The boolean array containing acknowledgements of all slices.</param>
    protected abstract void SetSliceAckData(byte chunkId, ushort numSlices, bool[] acked);
}
