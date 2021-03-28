using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using HKMP.Concurrency;
using HKMP.Game.Client.Entity;
using HKMP.Networking.Packet;
using HKMP.Networking.Packet.Custom.Update;
using UnityEngine;

namespace HKMP.Networking.Client {
    /**
     * Class that manages sending the update packet.
     * Has a simple congestion avoidance system to avoid flooding the channel.
     */
    public class UdpUpdateManager {
        // Number of milliseconds between sending packets if the channel is clear
        private const int HighSendRate = 17;
        // Number of milliseconds between sending packet if the channel is congested
        private const int LowSendRate = 50;
        // The maximum expected round trip time
        private const int MaximumExpectedRtt = 2000;
        // The round trip time threshold after which we switch to the low send rate
        private const int CongestionThreshold = 500;
        
        // The time thresholds (in milliseconds) in which we need to have a good RTT before switching send rates 
        private const int MaximumSwitchThreshold = 60000;
        private const int MinimumSwitchThreshold = 1000;

        // If we switch from High to Low send rates, without even spending
        // this amount of time, we increase the switch threshold
        private const int TimeSpentCongestionThreshold = 10000;
        
        // The UdpNetClient instance to use to send packets
        private readonly UdpNetClient _udpNetClient;
        private bool _canSendPackets;

        private readonly ConcurrentDictionary<ushort, Stopwatch> _sentQueue;
        private ushort _sequenceNumber;

        // The current average round trip time
        private float _averageRtt;
        // Whether the channel is currently congested
        private bool _isChannelCongested;

        private readonly ServerUpdatePacket _currentUpdatePacket;

        private readonly Stopwatch _sendStopwatch;

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

        private Thread _sendThread;

        public UdpUpdateManager(UdpNetClient udpNetClient) {
            _udpNetClient = udpNetClient;
            
            _sentQueue = new ConcurrentDictionary<ushort, Stopwatch>();
            _sequenceNumber = 0;

            // TODO: is this a good initial value?
            _averageRtt = 0f;
            _currentSwitchTimeThreshold = 10000;

            _currentUpdatePacket = new ServerUpdatePacket();

            _sendStopwatch = new Stopwatch();
            _belowThresholdStopwatch = new Stopwatch();
            _currentCongestionStopwatch = new Stopwatch();
        }

        public void StartUdpUpdates() {
            if (_canSendPackets) {
                Logger.Warn(this, "Tried to start new UDP update thread, while another is already running!");
                return;
            }
            
            _canSendPackets = true;
            _sendThread = new Thread(() => {
                while (_canSendPackets) {
                    SendPlayerUpdate();
                }
            });
            _sendThread.Start();
        }

        public void StopUdpUpdates() {
            _canSendPackets = false;
            _sendThread.Abort();
        }

        public void OnReceivePackets(List<Packet.Packet> packets) {
            foreach (var packet in packets) {
                // Read packet ID without advancing read position,
                // so we don't interfere with the packet handling
                var packetId = packet.ReadPacketId(false);
                
                if (packetId.Equals(PacketId.PlayerUpdate)) {
                    // Read the sequence number of the update packet, so we can check
                    // and adjust the average RTT and act on it
                    var sequenceNumber = packet.ReadSequenceNumber();
                    
                    // TODO: we don't do anything when the sequence number is not known to us
                    if (_sentQueue.TryGetValue(sequenceNumber, out var stopwatch)) {
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
                    }
                }
            }
        }
        
        private void SendPlayerUpdate() {
            if (!_canSendPackets) {
                if (_sendStopwatch.IsRunning) {
                    _sendStopwatch.Reset();
                }
                
                return;
            }
            
            if (!_sendStopwatch.IsRunning) {
                _sendStopwatch.Start();
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

                // Reset all one-time use values in the packet, so we can reuse it
                _currentUpdatePacket.ResetValues();
            }
            
            // Before we add another item to our queue, we check whether some
            // already exceed the maximum expected RTT
            foreach (var seqStopwatchPair in _sentQueue.GetCopy()) {
                if (seqStopwatchPair.Value.ElapsedMilliseconds > MaximumExpectedRtt) {
                    _sentQueue.Remove(seqStopwatchPair.Key);
                }
            }
            
            // Now we add our new sequence number into the queue with a running stopwatch
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            _sentQueue[_sequenceNumber] = stopwatch;
            
            // Increase (and potentially loop) the current sequence number
            if (_sequenceNumber == ushort.MaxValue) {
                _sequenceNumber = 0;
            } else {
                _sequenceNumber++;
            }
            
            _udpNetClient.Send(packet);
        }
        
        public void UpdatePlayerPosition(Vector3 position) {
            lock (_currentUpdatePacket) {
                _currentUpdatePacket.UpdateTypes.Add(UpdateType.PlayerUpdate);
                
                _currentUpdatePacket.PlayerUpdate.UpdateTypes.Add(PlayerUpdateType.Position);
                _currentUpdatePacket.PlayerUpdate.Position = position;
            }
        }

        public void UpdatePlayerScale(Vector3 scale) {
            lock (_currentUpdatePacket) {
                _currentUpdatePacket.UpdateTypes.Add(UpdateType.PlayerUpdate);

                _currentUpdatePacket.PlayerUpdate.UpdateTypes.Add(PlayerUpdateType.Scale);
                _currentUpdatePacket.PlayerUpdate.Scale = scale;
            }
        }

        public void UpdatePlayerMapPosition(Vector3 mapPosition) {
            lock (_currentUpdatePacket) {
                _currentUpdatePacket.UpdateTypes.Add(UpdateType.PlayerUpdate);

                _currentUpdatePacket.PlayerUpdate.UpdateTypes.Add(PlayerUpdateType.MapPosition);
                _currentUpdatePacket.PlayerUpdate.MapPosition = mapPosition;
            }
        }

        public void UpdatePlayerAnimation(ushort clipId, byte frame, bool[] effectInfo) {
            lock (_currentUpdatePacket) {
                _currentUpdatePacket.UpdateTypes.Add(UpdateType.PlayerUpdate);

                _currentUpdatePacket.PlayerUpdate.UpdateTypes.Add(PlayerUpdateType.Animation);
            
                // Create a new animation info instance
                var animationInfo = new AnimationInfo {
                    ClipId = clipId,
                    Frame = frame,
                    EffectInfo = effectInfo
                };

                // And add it to the list of animation info instances
                _currentUpdatePacket.PlayerUpdate.AnimationInfos.Add(animationInfo);
            }
        }

        public void UpdateEntityPosition(EntityType entityType, byte entityId, Vector3 position) {
            lock (_currentUpdatePacket) {
                _currentUpdatePacket.UpdateTypes.Add(UpdateType.EntityUpdate);
                
                // Try to find an already existing instance with the same type and id
                EntityUpdate entityUpdate = null;
                foreach (var existingEntityUpdate in _currentUpdatePacket.EntityUpdates) {
                    if (existingEntityUpdate.EntityType.Equals(entityType) && existingEntityUpdate.Id == entityId) {
                        entityUpdate = existingEntityUpdate;
                        break;
                    }
                }

                // If no existing instance was found, create one and add it to the list
                if (entityUpdate == null) {
                    entityUpdate = new EntityUpdate {
                        EntityType = entityType,
                        Id = entityId
                    };
                        
                    _currentUpdatePacket.EntityUpdates.Add(entityUpdate);
                }
                
                entityUpdate.UpdateTypes.Add(EntityUpdateType.Position);
                entityUpdate.Position = position;
            }
        }

        public void UpdateEntityState(EntityType entityType, byte entityId, byte stateIndex) {
            lock (_currentUpdatePacket) {
                _currentUpdatePacket.UpdateTypes.Add(UpdateType.EntityUpdate);
                
                // Try to find an already existing instance with the same type and id
                EntityUpdate entityUpdate = null;
                foreach (var existingEntityUpdate in _currentUpdatePacket.EntityUpdates) {
                    if (existingEntityUpdate.EntityType.Equals(entityType) && existingEntityUpdate.Id == entityId) {
                        entityUpdate = existingEntityUpdate;
                        break;
                    }
                }

                // If no existing instance was found, create one and add it to the list
                if (entityUpdate == null) {
                    entityUpdate = new EntityUpdate {
                        EntityType = entityType,
                        Id = entityId
                    };
                        
                    _currentUpdatePacket.EntityUpdates.Add(entityUpdate);
                }
                
                entityUpdate.UpdateTypes.Add(EntityUpdateType.State);
                entityUpdate.StateIndex = stateIndex;
            }
        }
        
        public void UpdateEntityVariables(EntityType entityType, byte entityId, List<byte> fsmVariables) {
            lock (_currentUpdatePacket) {
                _currentUpdatePacket.UpdateTypes.Add(UpdateType.EntityUpdate);

                // Try to find an already existing instance with the same type and id
                EntityUpdate entityUpdate = null;
                foreach (var existingEntityUpdate in _currentUpdatePacket.EntityUpdates) {
                    if (existingEntityUpdate.EntityType.Equals(entityType) && existingEntityUpdate.Id == entityId) {
                        entityUpdate = existingEntityUpdate;
                        break;
                    }
                }

                // If no existing instance was found, create one and add it to the list
                if (entityUpdate == null) {
                    entityUpdate = new EntityUpdate {
                        EntityType = entityType,
                        Id = entityId
                    };
                    
                    _currentUpdatePacket.EntityUpdates.Add(entityUpdate);
                }

                entityUpdate.UpdateTypes.Add(EntityUpdateType.Variables);
                entityUpdate.FsmVariables.AddRange(fsmVariables);
            }
        }
    }
}