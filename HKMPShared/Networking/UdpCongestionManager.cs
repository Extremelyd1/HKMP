using System.Diagnostics;
using Hkmp.Concurrency;
using Hkmp.Networking.Packet;

namespace Hkmp {
    public class UdpCongestionManager<TOutgoing> where TOutgoing : UpdatePacket, new() {
        // Number of milliseconds between sending packets if the channel is clear
        public const int HighSendRate = 17;

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

        // The corresponding update manager from which we receive the packets that
        // we calculate the RTT from
        private readonly UdpUpdateManager<TOutgoing> _udpUpdateManager;

        // Dictionary containing for each sequence number the corresponding packet and stopwatch
        // We use this to check the RTT of sent packets and to resend packets that contain reliable data
        // if they time out
        private readonly ConcurrentDictionary<ushort, SentPacket<TOutgoing>> _sentQueue;

        // The current average round trip time
        public float AverageRtt { get; private set; }

        // Whether the channel is currently congested
        private bool _isChannelCongested;

        // The current time for which we need to have a good RTT before switching send rates
        private int _currentSwitchTimeThreshold;

        // Whether we have spent the threshold in a high send rate.
        // If so, we don't increase the switchTimeThreshold if we switch again
        private bool _spentTimeThreshold;

        // The stopwatch keeping track of time spent below the threshold with the average RTT
        private readonly Stopwatch _belowThresholdStopwatch;

        // The stopwatch keeping track of time spent in either congested or non-congested mode
        private readonly Stopwatch _currentCongestionStopwatch;

        public UdpCongestionManager(UdpUpdateManager<TOutgoing> udpUpdateManager) {
            _udpUpdateManager = udpUpdateManager;

            _sentQueue = new ConcurrentDictionary<ushort, SentPacket<TOutgoing>>();

            AverageRtt = 0f;
            _currentSwitchTimeThreshold = 10000;

            _belowThresholdStopwatch = new Stopwatch();
            _currentCongestionStopwatch = new Stopwatch();
        }

        public void OnReceivePackets<TIncoming>(TIncoming packet) where TIncoming : UpdatePacket {
            // Check the congestion of the latest ack
            CheckCongestion(packet.Ack);

            // Check the congestion of all acknowledged packet in the ack field
            for (ushort i = 0; i < UdpUpdateManager.AckSize; i++) {
                if (packet.AckField[i]) {
                    var sequenceToCheck = (ushort) (packet.Ack - i - 1);

                    CheckCongestion(sequenceToCheck);
                }
            }
        }

        /**
         * Check the congestion after receiving the given sequence number that was acknowledged
         * We also switch send rates in this method if the average RTT is consistently high/low
         */
        private void CheckCongestion(ushort sequence) {
            if (!_sentQueue.TryGetValue(sequence, out var sentPacket)) {
                return;
            }

            var stopwatch = sentPacket.Stopwatch;

            var rtt = stopwatch.ElapsedMilliseconds;
            var difference = rtt - AverageRtt;

            // Adjust average with 1/10th of difference
            AverageRtt += difference * 0.1f;

            _sentQueue.Remove(sequence);

            if (_isChannelCongested) {
                // If the stopwatch for checking packets below the threshold was already running
                if (_belowThresholdStopwatch.IsRunning) {
                    // If our average is above the threshold again, we reset the stopwatch
                    if (AverageRtt > CongestionThreshold) {
                        _belowThresholdStopwatch.Reset();
                    }
                } else {
                    // If the stopwatch wasn't running, and we are below the threshold
                    // we can start the stopwatch again
                    if (AverageRtt < CongestionThreshold) {
                        _belowThresholdStopwatch.Start();
                    }
                }

                // If the average RTT was below the threshold for a certain amount of time,
                // we can go back to high send rates
                if (_belowThresholdStopwatch.IsRunning
                    && _belowThresholdStopwatch.ElapsedMilliseconds > _currentSwitchTimeThreshold) {
                    Logger.Get().Info(this, "Switched to non-congested send rates");

                    _isChannelCongested = false;

                    _udpUpdateManager.CurrentSendRate = HighSendRate;

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
                    _currentSwitchTimeThreshold =
                        System.Math.Max(_currentSwitchTimeThreshold / 2, MinimumSwitchThreshold);

                    Logger.Get().Info(this,
                        $"Proper time spent in non-congested mode, halved switch threshold to: {_currentSwitchTimeThreshold}");

                    // After we reach the minimum threshold, there's no reason to keep the stopwatch going
                    if (_currentSwitchTimeThreshold == MinimumSwitchThreshold) {
                        _currentCongestionStopwatch.Reset();
                    }
                }

                // If the channel was not previously congested, but our average round trip time
                // exceeds the threshold, we switch to congestion values
                if (AverageRtt > CongestionThreshold) {
                    Logger.Get().Info(this, "Switched to congested send rates");

                    _isChannelCongested = true;

                    _udpUpdateManager.CurrentSendRate = LowSendRate;

                    // If we were a few seconds in the High send rates before switching again, we
                    // double the threshold for switching
                    if (!_spentTimeThreshold) {
                        // Also cap it at a maximum
                        _currentSwitchTimeThreshold =
                            System.Math.Min(_currentSwitchTimeThreshold * 2, MaximumSwitchThreshold);

                        Logger.Get().Info(this,
                            $"Too little time spent in non-congested mode, doubled switch threshold to: {_currentSwitchTimeThreshold}");
                    }

                    // Since we switched send rates, we restart the stopwatch again
                    _currentCongestionStopwatch.Reset();
                    _currentCongestionStopwatch.Start();
                }
            }
        }

        public void OnSendPacket(ushort sequence, TOutgoing updatePacket) {
            // Before we add another item to our queue, we check whether some
            // already exceed the maximum expected RTT
            foreach (var seqSentPacketPair in _sentQueue.GetCopy()) {
                var sentPacket = seqSentPacketPair.Value;

                if (sentPacket.Stopwatch.ElapsedMilliseconds > MaximumExpectedRtt) {
                    _sentQueue.Remove(seqSentPacketPair.Key);

                    Logger.Get().Info(this,
                        $"Packet ack of seq: {seqSentPacketPair.Key} exceeded maximum RTT, assuming lost");

                    // Check if this packet contained information that needed to be reliable
                    // and if so, resend the data by adding it to the current packet
                    if (sentPacket.Packet.ContainsReliableData()) {
                        Logger.Get().Info(this, "  Packet contained reliable data, resending data");

                        _udpUpdateManager.ResendReliableData(sentPacket.Packet);
                    }
                }
            }

            // Now we add our new sequence number into the queue with a running stopwatch
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            _sentQueue[sequence] = new SentPacket<TOutgoing> {
                Packet = updatePacket,
                Stopwatch = stopwatch
            };
        }
    }

    public class SentPacket<T> where T : UpdatePacket {
        public T Packet { get; set; }
        public Stopwatch Stopwatch { get; set; }
    }
}