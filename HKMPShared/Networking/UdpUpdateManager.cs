using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using Hkmp.Concurrency;
using Hkmp.Networking.Packet;

namespace Hkmp {
    /**
     * Class that manages sending the update packet.
     * Has a simple congestion avoidance system to avoid flooding the channel.
     */
    public abstract class UdpUpdateManager {
        // This class exists solely to host a non-generic version of the const
        public const int AckSize = 32;
    }

    public abstract class UdpUpdateManager<TOutgoing> : UdpUpdateManager where TOutgoing : UpdatePacket, new() {
        // The time in milliseconds to disconnect after not receiving any updates
        private const int ConnectionTimeout = 5000;
        
        // The UdpNetClient instance to use to send packets
        protected readonly UdpClient UdpClient;

        private readonly UdpCongestionManager<TOutgoing> _udpCongestionManager;

        private bool _canSendPackets;

        private ushort _localSequence;
        private ushort _remoteSequence;

        private readonly ConcurrentFixedSizeQueue<ushort> _receivedQueue;

        protected readonly object Lock = new object();
        protected TOutgoing CurrentUpdatePacket;

        private Thread _sendThread;

        private Stopwatch _heartBeatStopwatch;

        // The current send rate in milliseconds between sending packets
        public int CurrentSendRate { get; set; } = UdpCongestionManager<TOutgoing>.HighSendRate;

        public int AverageRtt => (int) System.Math.Round(_udpCongestionManager.AverageRtt);

        public event Action OnTimeout;

        protected UdpUpdateManager(UdpClient udpClient) {
            UdpClient = udpClient;

            _udpCongestionManager = new UdpCongestionManager<TOutgoing>(this);

            _localSequence = 0;

            _receivedQueue = new ConcurrentFixedSizeQueue<ushort>(AckSize);

            CurrentUpdatePacket = new TOutgoing();

            _heartBeatStopwatch = new Stopwatch();
        }

        /**
         * Start sending periodic UDP update packets based on the send rate
         */
        public void StartUdpUpdates() {
            if (_canSendPackets) {
                Logger.Get().Warn(this, "Tried to start new UDP update thread, while another is already running!");
                return;
            }

            _canSendPackets = true;
            _sendThread = new Thread(() => {
                while (_canSendPackets) {
                    CreateAndSendUpdatePacket();
                    
                    if (_heartBeatStopwatch.ElapsedMilliseconds > ConnectionTimeout) {
                        // The stopwatch has surpassed the connection timeout value, so we call the timeout event
                        OnTimeout?.Invoke();
                        
                        // Stop the stopwatch for now to prevent the callback being execute multiple times
                        _heartBeatStopwatch.Reset();
                    }

                    Thread.Sleep(CurrentSendRate);
                }
            });
            _sendThread.Start();
            
            _heartBeatStopwatch.Reset();
            _heartBeatStopwatch.Start();
        }

        /**
         * Stop sending the periodic UDP update packets after sending
         * the current one
         */
        public void StopUdpUpdates() {
            Logger.Get().Info(this, "Stopping UDP updates, sending last packet");

            // Send the last packet
            CreateAndSendUpdatePacket();

            _heartBeatStopwatch.Reset();
            
            _canSendPackets = false;
            _sendThread.Abort();
        }

        public void OnReceivePacket<TIncoming>(TIncoming packet) where TIncoming : UpdatePacket {
            _udpCongestionManager.OnReceivePackets(packet);

            // Get the sequence number from the packet and add it to the receive queue
            var sequence = packet.Sequence;
            _receivedQueue.Enqueue(sequence);

            // Update the latest remote sequence number if applicable
            if (IsSequenceGreaterThan(sequence, _remoteSequence)) {
                _remoteSequence = sequence;
            }
            
            _heartBeatStopwatch.Reset();
            _heartBeatStopwatch.Start();
        }

        /**
         * Create and send the current update packet
         */
        private void CreateAndSendUpdatePacket() {
            if (UdpClient == null) {
                return;
            }

            Packet packet;
            TOutgoing updatePacket;

            lock (Lock) {
                CurrentUpdatePacket.Sequence = _localSequence;
                CurrentUpdatePacket.Ack = _remoteSequence;

                // Fill the ack field according to which packets have been acknowledged
                var receivedQueue = _receivedQueue.GetCopy();

                for (ushort i = 0; i < AckSize; i++) {
                    var pastSequence = (ushort) (_remoteSequence - i - 1);

                    // Set the value in the array to whether we have this sequence number in our receive queue
                    CurrentUpdatePacket.AckField[i] = receivedQueue.Contains(pastSequence);
                }

                packet = CurrentUpdatePacket.CreatePacket();

                // Reset the packet by creating a new instance,
                // but keep the original instance for reliability data re-sending
                updatePacket = CurrentUpdatePacket;
                CurrentUpdatePacket = new TOutgoing();
            }

            _udpCongestionManager.OnSendPacket(_localSequence, updatePacket);

            // Increase (and potentially wrap) the current local sequence number
            if (_localSequence == ushort.MaxValue) {
                _localSequence = 0;
            } else {
                _localSequence++;
            }

            SendPacket(packet);
        }

        /**
         * Check whether the first given sequence number is greater than the second given sequence number.
         * Accounts for sequence number wrap-around, by inverse comparison if differences are larger than half
         * of the sequence number space.
         */
        private bool IsSequenceGreaterThan(ushort sequence1, ushort sequence2) {
            return sequence1 > sequence2 && sequence1 - sequence2 <= 32768
                   || sequence1 < sequence2 && sequence2 - sequence1 > 32768;
        }

        /**
         * Resend the given packet that was (supposedly) lost by adding data that needs
         * to be reliable to the current update packet
         */
        public abstract void ResendReliableData(TOutgoing lostPacket);

        /**
         * Send the given packet over the corresponding medium
         */
        protected abstract void SendPacket(Packet packet);
    }
}