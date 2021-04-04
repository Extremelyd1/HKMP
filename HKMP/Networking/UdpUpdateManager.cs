using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using HKMP.Concurrency;
using HKMP.Networking.Packet;

namespace HKMP.Networking {
    /**
     * Class that manages sending the update packet.
     * Has a simple congestion avoidance system to avoid flooding the channel.
     */
    public abstract class UdpUpdateManager {
        // This class exists solely to host a non-generic version of the const
        public const int AckSize = 32;
    }
    
    public abstract class UdpUpdateManager<TOutgoing> : UdpUpdateManager where TOutgoing : UpdatePacket, new() {
        // The UdpNetClient instance to use to send packets
        protected readonly UdpClient UdpClient;
        private readonly UdpCongestionManager<TOutgoing> _udpCongestionManager;
        
        private bool _canSendPackets;

        private ushort _localSequence;
        private ushort _remoteSequence;

        private readonly ConcurrentFixedSizeQueue<ushort> _receivedQueue;

        protected TOutgoing CurrentUpdatePacket;

        private readonly Stopwatch _sendStopwatch;

        // The current send rate in milliseconds between sending packets
        public int CurrentSendRate { get; set; } = UdpCongestionManager<TOutgoing>.HighSendRate;

        private Thread _sendThread;

        protected UdpUpdateManager(UdpClient udpClient) {
            UdpClient = udpClient;
            _udpCongestionManager = new UdpCongestionManager<TOutgoing>(this);
            
            _localSequence = 0;

            _receivedQueue = new ConcurrentFixedSizeQueue<ushort>(AckSize);

            CurrentUpdatePacket = new TOutgoing();

            _sendStopwatch = new Stopwatch();
        }

        /**
         * Start sending periodic UDP update packets based on the send rate
         */
        public void StartUdpUpdates() {
            if (_canSendPackets) {
                Logger.Warn(this, "Tried to start new UDP update thread, while another is already running!");
                return;
            }
            
            _canSendPackets = true;
            _sendThread = new Thread(() => {
                while (_canSendPackets) {
                    CheckPlayerUpdate();
                }
            });
            _sendThread.Start();
        }

        /**
         * Stop sending the periodic UDP update packets after sending
         * the current one
         */
        public void StopUdpUpdates() {
            // Send the last packet
            CreateAndSendUpdatePacket();
            
            _canSendPackets = false;
            _sendThread.Abort();
        }

        public void OnReceivePacket<TIncoming>(TIncoming packet) where TIncoming : UpdatePacket {
            _udpCongestionManager.OnReceivePackets(packet);
            
            // Get the sequence number from the packet and add it to the receive queue
            var sequence = packet.Sequence;
            _receivedQueue.Enqueue(sequence);

            // Update the latest remote sequence number if applicable
            if (sequence > _remoteSequence) {
                _remoteSequence = sequence;
            }
        }

        /**
         * Check whether we should already send a new player update
         * and if so, send it
         */
        private void CheckPlayerUpdate() {
            if (!_canSendPackets) {
                if (_sendStopwatch.IsRunning) {
                    _sendStopwatch.Reset();
                }
                
                return;
            }
            
            if (!_sendStopwatch.IsRunning) {
                _sendStopwatch.Start();
            }

            if (_sendStopwatch.ElapsedMilliseconds < CurrentSendRate) {
                // TODO: maybe let the thread sleep here?
                return;
            }

            _sendStopwatch.Reset();
            _sendStopwatch.Start();
            
            if (UdpClient == null) {
                return;
            }

            CreateAndSendUpdatePacket();
        }

        /**
         * Create and send the current update packet
         */
        private void CreateAndSendUpdatePacket() {
            Packet.Packet packet;
            lock (CurrentUpdatePacket) {
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
                
                // Reset the packet by creating a new instance
                CurrentUpdatePacket = new TOutgoing();
            }

            _udpCongestionManager.OnSendPacket(_localSequence, CurrentUpdatePacket);
            
            // Increase (and potentially wrap) the current local sequence number
            if (_localSequence == ushort.MaxValue) {
                _localSequence = 0;
            } else {
                _localSequence++;
            }

            SendPacket(packet);
        }

        /**
         * Resend the given packet that was (supposedly) lost by adding data that needs
         * to be reliable to the current update packet
         */
        public abstract void ResendReliableData(TOutgoing lostPacket);

        /**
         * Send the given packet over the corresponding medium
         */
        protected abstract void SendPacket(Packet.Packet packet);
    }
}