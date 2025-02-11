using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Hkmp.Logging;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Chunk;

/// <summary>
/// Class that processes and manages chunks by sending slices of those chunks and receiving acknowledgements for those
/// slices.
/// </summary>
internal abstract class ChunkSender {
    /// <summary>
    /// The number of milliseconds to wait between sending slices.
    /// </summary>
    private const int WaitMillisBetweenSlices = 20;
    /// <summary>
    /// The number of milliseconds to wait before re-sending a slice.
    /// </summary>
    private const int WaitMillisResendSlice = 100;
    
    /// <summary>
    /// Blocking collection of packets that need to be sent as chunks.
    /// </summary>
    private readonly BlockingCollection<Packet.Packet> _toSendPackets;

    /// <summary>
    /// Boolean array where each value indicates whether the slice of the same index was acknowledged.
    /// </summary>
    private readonly bool[] _acked;
    /// <summary>
    /// Byte array that contains the chunk data that needs to be sent.
    /// </summary>
    private readonly byte[] _chunkData;

    /// <summary>
    /// Manual reset event that is used for its wait handle to time when to send the next slice.
    /// </summary>
    private readonly ManualResetEventSlim _sliceWaitHandle;

    /// <summary>
    /// Whether we are currently sending a chunk. If we are not sending anything, we ignore incoming chunk
    /// acknowledgements.
    /// </summary>
    private bool _isSending;
    /// <summary>
    /// The ID of the chunk we are currently sending.
    /// </summary>
    private byte _chunkId;
    /// <summary>
    /// The size of the chunk we are currently sending.
    /// </summary>
    private int _chunkSize;
    /// <summary>
    /// The number of slices of the chunk we are currently sending.
    /// </summary>
    private int _numSlices;
    /// <summary>
    /// The number of acknowledged slices in the currently sending chunk.
    /// </summary>
    private int _numAckedSlices;
    /// <summary>
    /// The ID of the slice we are currently sending.
    /// </summary>
    private int _currentSliceId;

    /// <summary>
    /// Array of stopwatches that keep track of the elapsed time since we have last sent the slice with the same ID.
    /// If this time is smaller than a certain threshold, we do not send the slice again yet.
    /// </summary>
    private Stopwatch[] _sliceStopwatches;

    /// <summary>
    /// Cancellation token source for cancelling the send task.
    /// </summary>
    private CancellationTokenSource _sendTaskTokenSource;

    /// <summary>
    /// Event that is called when we finish sending data. This is registered internally when the
    /// <see cref="FinishSendingData"/> method is called and we are waiting for the current chunk to finish sending.
    /// </summary>
    private event Action FinishSendingDataEvent;

    /// <summary>
    /// Construct the chunk sender by initializing the blocking collection and manual reset event, and allocating the
    /// arrays to their maximally used length.
    /// </summary>
    protected ChunkSender() {
        _toSendPackets = new BlockingCollection<Packet.Packet>();
        
        _acked = new bool[ConnectionManager.MaxSlicesPerChunk];
        _chunkData = new byte[ConnectionManager.MaxChunkSize];

        _sliceWaitHandle = new ManualResetEventSlim();
    }

    /// <summary>
    /// Start the chunk sender by starting the thread that manages the chunk sending.
    /// </summary>
    public void Start() {
        _sendTaskTokenSource?.Cancel();
        _sendTaskTokenSource?.Dispose();
        _sendTaskTokenSource = new CancellationTokenSource();
        
        new Thread(() => StartSends(_sendTaskTokenSource.Token)).Start();
    }

    /// <summary>
    /// Stop the chunk sender by cancelling the send task.
    /// </summary>
    public void Stop() {
        _sendTaskTokenSource?.Cancel();
        _sendTaskTokenSource?.Dispose();
        _sendTaskTokenSource = null;
    }

    /// <summary>
    /// Finish sending data and call the given callback whenever the data is finished sending.
    /// </summary>
    /// <param name="callback">The callback to invoke.</param>
    public void FinishSendingData(Action callback) {
        // If we aren't currently sending and the queue does not contain any packets to send, we immediately invoke
        // the callback and return
        if (!_isSending && _toSendPackets.Count == 0) {
            callback?.Invoke();
            return;
        }

        // Otherwise, we register the event
        // We do it like this so we can deregister the event immediately after it is called, so it doesn't trigger
        // more than once
        Action lambda = null;
        lambda = () => {
            callback?.Invoke();
            FinishSendingDataEvent -= lambda;
        };
        FinishSendingDataEvent += lambda;
    }

    /// <summary>
    /// Enqueue a packet to be sent as a chunk.
    /// </summary>
    /// <param name="packet">The packet to send.</param>
    public void EnqueuePacket(Packet.Packet packet) {
        _toSendPackets.Add(packet);
    }

    /// <summary>
    /// Process received slice acknowledgement data. First does sanity checks to see if we are actually sending a
    /// chunk, whether the received chunk ID matches the currently sending chunk ID, and whether the number of slices
    /// matches. Then for each of the slice indices in the acknowledgement array, it checks whether this is a newly
    /// acknowledged slice and locally marks it as acknowledged.
    /// </summary>
    /// <param name="sliceAckData">The received slice acknowledgement data.</param>
    public void ProcessReceivedData(SliceAckData sliceAckData) {
        Logger.Debug($"Received slice ack packet: {sliceAckData.ChunkId}, {sliceAckData.NumSlices}");

        if (!_isSending) {
            Logger.Debug("Not sending a chunk, ignoring ack packet");
            return;
        }

        if (_chunkId != sliceAckData.ChunkId) {
            Logger.Debug("Chunk ID of received ack packet does not correspond with currently sending chunk");
            return;
        }

        if (_numSlices != sliceAckData.NumSlices) {
            Logger.Debug("Number of slices in ack packet does not correspond with local number of slices");
            return;
        }

        for (var i = 0; i < _numSlices; i++) {
            if (sliceAckData.Acked[i] && !_acked[i]) {
                _acked[i] = true;
                _numAckedSlices += 1;
                
                Logger.Debug($"Received acknowledgement for slice {i}, total acked: {_numAckedSlices}");
            }
        }
    }

    /// <summary>
    /// Start the sending process with the given cancellation token.
    /// We block on the collection to take a new packet to start sending. Once a packet is taken from the collection,
    /// we calculate the chunk size and number of slices that we need to send. Then, wee go over the slices in
    /// ascending order and send one with a given delay between each slice. Each slice that is acknowledged already
    /// is skipped in the sending order. If we have already sent a given slice less than a certain threshold ago, we
    /// also skip sending it. Once all slices have been acknowledged, we go back to blocking on the collection to wait
    /// for a new chunk to send.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for cancelling the sending process.</param>
    private void StartSends(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            if (_toSendPackets.Count == 0) {
                FinishSendingDataEvent?.Invoke();
            }
            
            Packet.Packet packet;
            try {
                packet = _toSendPackets.Take(cancellationToken);
            } catch (OperationCanceledException) {
                return;
            }
            
            _isSending = true;
            
            Logger.Debug("Successfully taken new packet from blocking collection, starting networking chunk");

            _sliceStopwatches = new Stopwatch[ConnectionManager.MaxSlicesPerChunk];
            _numAckedSlices = 0;

            var packetBytes = packet.ToArray();

            _chunkSize = packetBytes.Length;
            _numSlices = _chunkSize / ConnectionManager.MaxSliceSize;
            if (_chunkSize % ConnectionManager.MaxSliceSize != 0) {
                _numSlices += 1;
            }
            
            Logger.Debug($"ChunkSize: {_chunkSize}, NumSlices: {_numSlices}");

            // Skip over chunks that exceed the maximum size that our system can handle
            if (_chunkSize > ConnectionManager.MaxChunkSize) {
                Logger.Error($"Could not send packet that exceeds max chunk size: {_chunkSize}");
                continue;
            }

            // Copy the raw bytes from the packet into the chunk data array
            Array.Copy(packetBytes, _chunkData, _chunkSize);

            do {
                Logger.Debug($"Sending next slice: {_currentSliceId}");
                SendNextSlice();

                // Obtain (or create) the stopwatch for the slice and start it
                var sliceStopwatch = _sliceStopwatches[_currentSliceId];
                if (sliceStopwatch == null) {
                    sliceStopwatch = new Stopwatch();
                    _sliceStopwatches[_currentSliceId] = sliceStopwatch;
                }
                sliceStopwatch.Restart();

                if (!TryGetNextSliceToSend()) {
                    Logger.Debug($"All slices have been acked ({_numAckedSlices}), stopping sending slices");
                    break;
                }

                long waitMillisNextSlice;

                // Get the stopwatch for this slice, and check whether we have already sent this slice not too long ago
                // If so, we wait longer before resending the slice. Otherwise, we default to the normal send rate.
                sliceStopwatch = _sliceStopwatches[_currentSliceId];
                if (sliceStopwatch == null) {
                    waitMillisNextSlice = WaitMillisBetweenSlices;
                } else {
                    waitMillisNextSlice = WaitMillisResendSlice - sliceStopwatch.ElapsedMilliseconds;
                    if (waitMillisNextSlice < 0) {
                        waitMillisNextSlice = WaitMillisBetweenSlices;
                    }
                }
                
                Logger.Debug($"Waiting on handle for next slice: {waitMillisNextSlice}");
                try {
                    _sliceWaitHandle.Wait((int) waitMillisNextSlice, cancellationToken);
                } catch (OperationCanceledException) {
                    Logger.Debug("Wait operation was cancelled, breaking");
                    break;
                }
            } while (!cancellationToken.IsCancellationRequested);
            
            Logger.Debug($"Incrementing chunk ID to: {_chunkId + 1}");
            _chunkId += 1;
            _isSending = false;
        }
    }

    /// <summary>
    /// Send the next slice, whose ID is <see cref="_currentSliceId"/>. This will figure out the start index of the
    /// data in the array and copy the data into a new array for adding to the update packet.
    /// </summary>
    private void SendNextSlice() {
        var startIndex = _currentSliceId * ConnectionManager.MaxSliceSize;
        
        byte[] sliceBytes;
        // Figure out if the start index for the next slice would exceed the chunk size. If so, the length of the slice
        // is less than the maximum slice size, which we need to calculate
        if ((_currentSliceId + 1) * ConnectionManager.MaxSliceSize > _chunkSize) {
            var length = _chunkSize - startIndex;
            sliceBytes = new byte[length];
            
            Array.Copy(_chunkData, startIndex, sliceBytes, 0, length);
        } else {
            sliceBytes = new byte[ConnectionManager.MaxSliceSize];
            
            Array.Copy(_chunkData, startIndex, sliceBytes, 0, sliceBytes.Length);
        }

        SetSliceData(_chunkId, (byte) _currentSliceId, (byte) _numSlices, sliceBytes);
    }

    /// <summary>
    /// Try to get the next slice ID that we need to send. We simply iterate in ascending order over slice IDs until
    /// we find one that is not yet acknowledged. Each iteration we check whether the number of acknowledged slices
    /// equals the number of slices in the chunk, so we don't end up in an infinite loop.
    /// </summary>
    /// <returns>True if a next slice could be found, false if all slices are acknowledged.</returns>
    private bool TryGetNextSliceToSend() {
        do {
            // We do the check inside the loop to prevent multi-thread issues where another ack is received and
            // a non-acked slice cannot be found anywhere, resulting in an infinite while loop
            if (_numAckedSlices == _numSlices) {
                return false;
            }

            _currentSliceId += 1;
            if (_currentSliceId >= _numSlices) {
                _currentSliceId = 0;
            }
        } while (_acked[_currentSliceId]);

        return true;
    }

    /// <summary>
    /// Set the slice data in the corresponding update manager for sending.
    /// </summary>
    /// <param name="chunkId">The ID of the chunk for this slice.</param>
    /// <param name="sliceId">The ID of the slice.</param>
    /// <param name="numSlices">The number of slices in this chunk.</param>
    /// <param name="data">The byte array containing the data of the slice.</param>
    protected abstract void SetSliceData(byte chunkId, byte sliceId, byte numSlices, byte[] data);
}
