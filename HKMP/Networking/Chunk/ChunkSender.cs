using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Hkmp.Logging;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Chunk;

internal abstract class ChunkSender {
    private readonly BlockingCollection<Packet.Packet> _toSendPackets;
    
    private readonly bool[] _acked;
    private readonly byte[] _chunkData;

    /// <summary>
    /// Wait handle for inter-thread signalling when a new slice is ready to be sent.
    /// </summary>
    private readonly ManualResetEventSlim _sliceWaitHandle;
    
    private bool _isSending;
    private byte _chunkId;
    private int _chunkSize;
    private int _numSlices;
    private int _numAckedSlices;
    private int _currentSliceId;
    
    private Stopwatch[] _sliceStopwatches;

    private CancellationTokenSource _sendTaskTokenSource;

    private event Action FinishSendingDataEvent;

    protected ChunkSender() {
        _toSendPackets = new BlockingCollection<Packet.Packet>();
        
        _acked = new bool[ConnectionManager.MaxSlicesPerChunk];
        _chunkData = new byte[ConnectionManager.MaxChunkSize];

        _sliceWaitHandle = new ManualResetEventSlim();
    }

    public void Start() {
        _sendTaskTokenSource?.Cancel();
        _sendTaskTokenSource?.Dispose();
        _sendTaskTokenSource = new CancellationTokenSource();
        
        new Thread(() => StartSends(_sendTaskTokenSource.Token)).Start();
    }

    public void Stop() {
        _sendTaskTokenSource?.Cancel();
        _sendTaskTokenSource?.Dispose();
        _sendTaskTokenSource = null;
    }

    public void FinishSendingData(Action callback) {
        if (!_isSending && _toSendPackets.Count == 0) {
            Stop();
            callback?.Invoke();
            return;
        }

        FinishSendingDataEvent += () => {
            Stop();
            callback?.Invoke();
        };
    }

    public void EnqueuePacket(Packet.Packet packet) {
        _toSendPackets.Add(packet);
    }

    public void ProcessReceivedData(SliceAckData sliceAckData) {
        Logger.Debug($"Received slice ack packet: {sliceAckData.ChunkId}, {sliceAckData.NumSlicesMinusOne}");

        if (!_isSending) {
            Logger.Debug("Not sending a chunk, ignoring ack packet");
            return;
        }

        if (_chunkId != sliceAckData.ChunkId) {
            Logger.Debug("Chunk ID of received ack packet does not correspond with currently sending chunk");
            return;
        }

        if (_numSlices != sliceAckData.NumSlicesMinusOne + 1) {
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

            if (_chunkSize > ConnectionManager.MaxChunkSize) {
                Logger.Error($"Could not send packet that exceeds max chunk size: {_chunkSize}");
                continue;
            }
            
            Array.Copy(packetBytes, _chunkData, _chunkSize);

            do {
                Logger.Debug($"Sending next slice: {_currentSliceId}");
                SendNextSlice();

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

                sliceStopwatch = _sliceStopwatches[_currentSliceId];
                if (sliceStopwatch == null) {
                    waitMillisNextSlice = 50;
                } else {
                    waitMillisNextSlice = 100 - sliceStopwatch.ElapsedMilliseconds;
                    if (waitMillisNextSlice < 0) {
                        waitMillisNextSlice = 50;
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

    private void SendNextSlice() {
        var startIndex = _currentSliceId * ConnectionManager.MaxSliceSize;
        
        byte[] sliceBytes;
        if ((_currentSliceId + 1) * ConnectionManager.MaxSliceSize > _chunkSize) {
            var length = _chunkSize - startIndex;
            sliceBytes = new byte[length];
            
            Array.Copy(_chunkData, startIndex, sliceBytes, 0, length);
        } else {
            sliceBytes = new byte[ConnectionManager.MaxSliceSize];
            
            Array.Copy(_chunkData, startIndex, sliceBytes, 0, sliceBytes.Length);
        }

        SendSliceData(_chunkId, (byte) _currentSliceId, (byte) _numSlices, sliceBytes);
    }

    private bool TryGetNextSliceToSend() {
        do {
            // We do the check inside the loop to prevent multi-thread issues where another ack is received and
            // a non-acked slice cannot be found anywhere, resulting in an infinite while loop
            if (_numAckedSlices == _numSlices) {
                return false;
            }

            _currentSliceId += 1;
        } while (_acked[_currentSliceId]);

        return true;
    }

    protected abstract void SendSliceData(byte chunkId, byte sliceId, byte numSlices, byte[] data);
}
