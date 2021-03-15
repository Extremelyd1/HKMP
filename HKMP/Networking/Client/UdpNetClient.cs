using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using HKMP.Networking.Packet;
using HKMP.Networking.Packet.Custom;
using UnityEngine;

namespace HKMP.Networking.Client {
    
    /**
     * NetClient that uses the UDP protocol
     */
    public class UdpNetClient {
        // Number of milliseconds between sending packets if the channel is clear
        private const int HighSendRate = 17;
        // Number of milliseconds between sending packet if the channel is congested
        private const int LowSendRate = 50;
        // The maximum expected round trip time
        private const int MaximumExpectedRtt = 1000;
        // The round trip time threshold after which we switch to the low send rate
        private const int CongestionThreshold = 250;
        
        // The time thresholds (in milliseconds) in which we need to have a good RTT before switching send rates 
        private const int MaximumSwitchThreshold = 60000;
        private const int MinimumSwitchThreshold = 1000;

        // If we switch from High to Low send rates, without even spending
        // this amount of time, we increase the switch threshold
        private const int TimeSpentCongestionThreshold = 10000;
        
        private readonly object _lock = new object();
        
        private UdpClient _udpClient;
        private IPEndPoint _endPoint;
        
        private OnReceive _onReceive;
        
        private byte[] _leftoverData;

        private readonly Dictionary<ushort, Stopwatch> _sentQueue;
        private ushort _sequenceNumber;

        // The current average round trip time
        private float _averageRtt;
        // Whether the channel is currently congested
        private bool _isChannelCongested;

        private ServerPlayerUpdatePacket _currentUpdatePacket;

        private readonly Stopwatch _sendStopwatch;

        private readonly Thread _sendThread;

        // The current send rate in milliseconds between sending packets
        private int _currentSendRate = HighSendRate;

        // The current time for which we need to have a good RTT before switching send rates
        private int _currentSwitchTimeThreshold;
        // Whether we have spent the threshold in a high send rate.
        // If so, we don't increase the switchTimeThreshold if we switch again
        private bool _spentTimeThreshold;

        // The stopwatch keeping track of time spent below the threshold with the average RTT
        private readonly Stopwatch _belowThresholdStopwatch;
        // The stopwatch keeping track of time spent in either congested or non-congested mode
        private readonly Stopwatch _currentCongestionStopwatch;

        public UdpNetClient() {
            _sentQueue = new Dictionary<ushort, Stopwatch>();
            _sequenceNumber = 0;

            // TODO: is this a good initial value?
            _averageRtt = 0f;
            _currentSwitchTimeThreshold = 10000;

            _currentUpdatePacket = new ServerPlayerUpdatePacket();

            _sendStopwatch = new Stopwatch();
            _belowThresholdStopwatch = new Stopwatch();
            _currentCongestionStopwatch = new Stopwatch();

            _sendThread = new Thread(() => {
                while (true) {
                    Send();
                }
            });
        }
        
        public void RegisterOnReceive(OnReceive onReceive) {
            _onReceive = onReceive;
        }
        
        public void Connect(string host, int port, int localPort) {
            _endPoint = new IPEndPoint(IPAddress.Any, localPort);

            _udpClient = new UdpClient(localPort);
            _udpClient.Connect(host, port);
            _udpClient.BeginReceive(OnReceive, null);

            _sendStopwatch.Reset();
            _sendStopwatch.Start();

            _belowThresholdStopwatch.Reset();
            _currentCongestionStopwatch.Reset();
            _currentCongestionStopwatch.Start();

            _sendThread.Start();

            // Reset some congestion related values
            _averageRtt = 0f;
            _currentSwitchTimeThreshold = 10000;
            _spentTimeThreshold = false;

            Logger.Info(this, $"Starting receiving UDP data on endpoint {_endPoint}");
        }

        private void OnReceive(IAsyncResult result) {
            byte[] receivedData = {};
            
            try {
                receivedData = _udpClient.EndReceive(result, ref _endPoint);
            } catch (Exception e) {
                Logger.Warn(this, $"UDP Receive exception: {e.Message}");
            }

            // Immediately start listening for new data
            // Only do this when the client exists, we might have closed the client
            _udpClient?.BeginReceive(OnReceive, null);
            
            // If we did not receive at least an int of bytes, something went wrong
            if (receivedData.Length < 4) {
                Logger.Error(this, $"Received incorrect data length: {receivedData.Length}");
                
                return;
            }
            
            List<Packet.Packet> packets;

            // Lock the leftover data array for synchronous data handling
            // This makes sure that from another asynchronous receive callback we don't
            // read/write to it in different places
            lock (_lock) {
                packets = PacketManager.HandleReceivedData(receivedData, ref _leftoverData);
            }

            foreach (var packet in packets) {
                // Logger.Info(this, $"Received packet from server");
            
                if (packet.IsAckPacket()) {
                    var sequenceNumber = packet.ReadSequenceNumber();
                    // Logger.Info(this, $"Packet is ack, seq: {sequenceNumber}");
                    
                    if (_sentQueue.ContainsKey(sequenceNumber)) {
                        var stopwatch = _sentQueue[sequenceNumber];

                        var rtt = stopwatch.ElapsedMilliseconds;
                        var difference = rtt - _averageRtt;

                        // Adjust average with 1/10th of difference
                        _averageRtt += difference * 0.1f;

                        _sentQueue.Remove(sequenceNumber);
                        
                        if (_isChannelCongested) {
                            // If the stopwatch for checking packets below the threshold was already running
                            if (_belowThresholdStopwatch.IsRunning) {
                                // If our average is above the threshold again, we reset the stopwatch
                                if (_averageRtt > CongestionThreshold) {
                                    _belowThresholdStopwatch.Reset();
                                }                
                            } else {
                                // If the stopwatch wasn't running, and we are below the threshold
                                // we can start the stopwatch again
                                if (_averageRtt < CongestionThreshold) {
                                    _belowThresholdStopwatch.Start();
                                }
                            }

                            // If the average RTT was below the threshold for a certain amount of time,
                            // we can go back to high send rates
                            if (_belowThresholdStopwatch.IsRunning 
                                && _belowThresholdStopwatch.ElapsedMilliseconds > _currentSwitchTimeThreshold) {
                                Logger.Info(this, "Switched to non-congested send rates");
                                
                                _isChannelCongested = false;
                                _currentSendRate = HighSendRate;
                                
                                // Reset whether we have spent the threshold in non-congested mode
                                _spentTimeThreshold = false;

                                // Since we switched send rates, we restart the stopwatch again
                                _currentCongestionStopwatch.Reset();
                                _currentCongestionStopwatch.Start();
                            }
                        } else {
                            // If the channel is not congested, we check whether we have spent a certain
                            // amount of time in this mode, and decrease the switch threshold
                            if (_currentCongestionStopwatch.ElapsedMilliseconds > TimeSpentCongestionThreshold) {
                                // We spent at least the threshold in non-congestion mode
                                _spentTimeThreshold = true;
                            
                                _currentCongestionStopwatch.Reset();
                                _currentCongestionStopwatch.Start();
                                
                                // Also cap it at a minimum
                                _currentSwitchTimeThreshold = Math.Max(_currentSwitchTimeThreshold / 2, MinimumSwitchThreshold);
                                
                                Logger.Info(this, $"Proper time spent in non-congested mode, halved switch threshold to: {_currentSwitchTimeThreshold}");

                                // After we reach the minimum threshold, there's no reason to keep the stopwatch going
                                if (_currentSwitchTimeThreshold == MinimumSwitchThreshold) {
                                    _currentCongestionStopwatch.Reset();
                                }
                            }
                        
                            // If the channel was not previously congested, but our average round trip time
                            // exceeds the threshold, we switch to congestion values
                            if (_averageRtt > CongestionThreshold) {
                                Logger.Info(this, "Switched to congested send rates");
                                
                                _isChannelCongested = true;
                                _currentSendRate = LowSendRate;

                                // If we were a few seconds in the High send rates before switching again, we
                                // double the threshold for switching
                                if (!_spentTimeThreshold) {
                                    // Also cap it at a maximum
                                    _currentSwitchTimeThreshold = Math.Min(_currentSwitchTimeThreshold * 2, MaximumSwitchThreshold);
                                    
                                    Logger.Info(this, $"Too little time spent in non-congested mode, doubled switch threshold to: {_currentSwitchTimeThreshold}");
                                }
                                
                                // Since we switched send rates, we restart the stopwatch again
                                _currentCongestionStopwatch.Reset();
                                _currentCongestionStopwatch.Start();
                            }   
                        }

                        // Logger.Info(this, $"New RTT: {rtt}");
                        // Logger.Info(this, $"Average RTT: {_averageRtt}");
                    }
                } else {
                    _onReceive?.Invoke(packets);
                }
            }
        }

        /**
         * Disconnect the UDP client and clean it up
         */
        public void Disconnect() {
            if (!_udpClient.Client.Connected) {
                Logger.Warn(this, "UDP client was not connected, cannot disconnect");
                return;
            }
            
            _udpClient.Close();
            _udpClient = null;
            
            _sendThread.Abort();

            _sendStopwatch.Reset();

            _belowThresholdStopwatch.Reset();
        }

        public void SendPositionUpdate(Vector3 position) {
            lock (_currentUpdatePacket) {
                _currentUpdatePacket.Position = position;
            }
        }

        public void SendScaleUpdate(Vector3 scale) {
            lock (_currentUpdatePacket) {
                _currentUpdatePacket.Scale = scale;
            }
        }

        public void SendMapUpdate(Vector3 mapPosition) {
            lock (_currentUpdatePacket) {
                _currentUpdatePacket.MapPosition = mapPosition;
            }
        }

        public void Send(Packet.Packet packet) {
            if (!_udpClient.Client.Connected) {
                Logger.Error(this, "Tried sending packet, but UDP was not connected");
                return;
            }
            
            // Send the packet
            _udpClient.BeginSend(packet.ToArray(), packet.Length(), null, null);
        }

        private void Send() {
            if (!_udpClient.Client.Connected) {
                Logger.Error(this, "Tried sending packet, but UDP was not connected");
                Thread.Sleep(100);
                return;
            }
            
            if (_sendStopwatch.ElapsedMilliseconds < _currentSendRate) {
                // TODO: maybe let the thread sleep here?
                return;
            }

            _sendStopwatch.Reset();
            _sendStopwatch.Start();

            Packet.Packet packet;
            lock (_currentUpdatePacket) {
                _currentUpdatePacket.SequenceNumber = _sequenceNumber;
                
                packet = _currentUpdatePacket.CreatePacket();

                // Logger.Info(this, $"Animation: {_currentUpdatePacket.AnimationClipName}, {_currentUpdatePacket.AnimationEffectName}");
            }

            // Logger.Info(this, $"Sending new update packet, seq: {_sequenceNumber}");

            // Before we add another item to our queue, we check whether some
            // already exceed the maximum expected RTT
            foreach (var seqStopwatchPair in new Dictionary<ushort, Stopwatch>(_sentQueue)) {
                if (seqStopwatchPair.Value.ElapsedMilliseconds > MaximumExpectedRtt) {
                    _sentQueue.Remove(seqStopwatchPair.Key);
                }
            }
            
            // Now we add our new sequence number into the queue with a running stopwatch
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            _sentQueue.Add(_sequenceNumber, stopwatch);
            
            // Increase (and potentially loop) the current sequence number
            if (_sequenceNumber == ushort.MaxValue) {
                _sequenceNumber = 0;
            } else {
                _sequenceNumber++;
            }

            // Send the packet
            _udpClient.BeginSend(packet.ToArray(), packet.Length(), null, null);
        }
    }
}