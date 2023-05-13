using System;
using System.Net.Sockets;
using Hkmp.Concurrency;
using Hkmp.Logging;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking;

/// <summary>
/// Class that manages sending the update packet. Has a simple congestion avoidance system to
/// avoid flooding the channel.
/// </summary>
internal abstract class UdpUpdateManager {
    /// <summary>
    /// The number of ack numbers from previous packets to store in the packet. 
    /// </summary>
    public const int AckSize = 64;
}

/// <inheritdoc />
internal abstract class UdpUpdateManager<TOutgoing, TPacketId> : UdpUpdateManager
    where TOutgoing : UpdatePacket<TPacketId>, new()
    where TPacketId : Enum {
    /// <summary>
    /// The time in milliseconds to disconnect after not receiving any updates.
    /// </summary>
    private const int ConnectionTimeout = 5000;

    /// <summary>
    /// The number of sequence numbers to store in the received queue to construct ack fields with and
    /// to check against resent data.
    /// </summary>
    private const int ReceiveQueueSize = AckSize;

    /// <summary>
    /// The Socket instance to use to send packets.
    /// </summary>
    protected readonly Socket UdpSocket;

    /// <summary>
    /// The UDP congestion manager instance.
    /// </summary>
    private readonly UdpCongestionManager<TOutgoing, TPacketId> _udpCongestionManager;

    /// <summary>
    /// Boolean indicating whether we are allowed to send packets.
    /// </summary>
    private bool _canSendPackets;

    /// <summary>
    /// The last sent sequence number.
    /// </summary>
    private ushort _localSequence;

    /// <summary>
    /// The last received sequence number.
    /// </summary>
    private ushort _remoteSequence;

    /// <summary>
    /// Fixed-size queue containing sequence numbers that have been received.
    /// </summary>
    private readonly ConcurrentFixedSizeQueue<ushort> _receivedQueue;

    /// <summary>
    /// Object to lock asynchronous accesses.
    /// </summary>
    protected readonly object Lock = new object();

    /// <summary>
    /// The current instance of the update packet.
    /// </summary>
    protected TOutgoing CurrentUpdatePacket;

    /// <summary>
    /// Stopwatch to keep track of when to send a new update.
    /// </summary>
    private readonly ConcurrentStopwatch _sendStopwatch;

    /// <summary>
    /// Stopwatch to keep track of the heart beat to know when the client times out.
    /// </summary>
    private readonly ConcurrentStopwatch _heartBeatStopwatch;

    /// <summary>
    /// The current send rate in milliseconds between sending packets.
    /// </summary>
    public int CurrentSendRate { get; set; } = UdpCongestionManager<TOutgoing, TPacketId>.HighSendRate;

    /// <summary>
    /// Moving average of round trip time (RTT) between sending and receiving a packet.
    /// </summary>
    public int AverageRtt => (int) System.Math.Round(_udpCongestionManager.AverageRtt);

    /// <summary>
    /// Event that is called when the client times out.
    /// </summary>
    public event Action OnTimeout;

    /// <summary>
    /// Construct the update manager with a UDP socket.
    /// </summary>
    /// <param name="udpSocket">The UDP socket instance.</param>
    protected UdpUpdateManager(Socket udpSocket) {
        UdpSocket = udpSocket;

        _udpCongestionManager = new UdpCongestionManager<TOutgoing, TPacketId>(this);

        _receivedQueue = new ConcurrentFixedSizeQueue<ushort>(ReceiveQueueSize);

        CurrentUpdatePacket = new TOutgoing();

        _sendStopwatch = new ConcurrentStopwatch();
        _heartBeatStopwatch = new ConcurrentStopwatch();
    }

    /// <summary>
    /// Start the update manager and allow sending updates.
    /// </summary>
    public void StartUpdates() {
        _canSendPackets = true;

        _sendStopwatch.Restart();
        _heartBeatStopwatch.Restart();
    }

    /// <summary>
    /// Process an update for this update manager.
    /// </summary>
    public void ProcessUpdate() {
        if (!_canSendPackets) {
            return;
        }

        // Check if we can send another update
        if (_sendStopwatch.ElapsedMilliseconds > CurrentSendRate) {
            CreateAndSendUpdatePacket();

            _sendStopwatch.Restart();
        }

        // Check heartbeat to make sure the connection is still alive
        if (_heartBeatStopwatch.ElapsedMilliseconds > ConnectionTimeout) {
            // The stopwatch has surpassed the connection timeout value, so we call the timeout event
            OnTimeout?.Invoke();

            // Stop the stopwatch for now to prevent the callback being executed multiple times
            _heartBeatStopwatch.Reset();
        }
    }

    /// <summary>
    /// Stop sending the periodic UDP update packets after sending the current one.
    /// </summary>
    public void StopUpdates() {
        Logger.Debug("Stopping UDP updates, sending last packet");

        // Send the last packet
        CreateAndSendUpdatePacket();

        _sendStopwatch.Reset();
        _heartBeatStopwatch.Reset();

        _canSendPackets = false;
    }

    /// <summary>
    /// Callback method for when a packet is received.
    /// </summary>
    /// <param name="packet"></param>
    /// <typeparam name="TIncoming"></typeparam>
    /// <typeparam name="TOtherPacketId"></typeparam>
    public void OnReceivePacket<TIncoming, TOtherPacketId>(TIncoming packet)
        where TIncoming : UpdatePacket<TOtherPacketId>
        where TOtherPacketId : Enum {
        _udpCongestionManager.OnReceivePackets<TIncoming, TOtherPacketId>(packet);

        // Get the sequence number from the packet and add it to the receive queue
        var sequence = packet.Sequence;
        _receivedQueue.Enqueue(sequence);

        // Instruct the packet to drop all resent data that was received already
        packet.DropDuplicateResendData(_receivedQueue.GetCopy());

        // Update the latest remote sequence number if applicable
        if (IsSequenceGreaterThan(sequence, _remoteSequence)) {
            _remoteSequence = sequence;
        }

        _heartBeatStopwatch.Restart();
    }

    /// <summary>
    /// Create and send the current update packet.
    /// </summary>
    private void CreateAndSendUpdatePacket() {
        if (UdpSocket == null) {
            return;
        }

        Packet.Packet packet;
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
        _localSequence++;

        SendPacket(packet);
    }

    /// <summary>
    /// Check whether the first given sequence number is greater than the second given sequence number.
    /// Accounts for sequence number wrap-around, by inverse comparison if differences are larger than half
    /// of the sequence number space.
    /// </summary>
    /// <param name="sequence1">The first sequence number to compare.</param>
    /// <param name="sequence2">The second sequence number to compare.</param>
    /// <returns>True if the first sequence number is greater than the second sequence number.</returns>
    private bool IsSequenceGreaterThan(ushort sequence1, ushort sequence2) {
        return sequence1 > sequence2 && sequence1 - sequence2 <= 32768
               || sequence1 < sequence2 && sequence2 - sequence1 > 32768;
    }

    /// <summary>
    /// Resend the given packet that was (supposedly) lost by adding data that needs to be reliable to the
    /// current update packet.
    /// </summary>
    /// <param name="lostPacket">The packet instance that was lost.</param>
    public abstract void ResendReliableData(TOutgoing lostPacket);

    /// <summary>
    /// Send the given packet over the corresponding medium.
    /// </summary>
    /// <param name="packet">The raw packet instance.</param>
    protected abstract void SendPacket(Packet.Packet packet);

    /// <summary>
    /// Either get or create an AddonPacketData instance for the given addon.
    /// </summary>
    /// <param name="addonId">The ID of the addon.</param>
    /// <param name="packetIdSize">The size of the packet ID size.</param>
    /// <returns>The instance of AddonPacketData already in the packet or a new one if no such instance
    /// exists</returns>
    private AddonPacketData GetOrCreateAddonPacketData(byte addonId, byte packetIdSize) {
        lock (Lock) {
            if (!CurrentUpdatePacket.TryGetSendingAddonPacketData(
                    addonId,
                    out var addonPacketData
                )) {
                addonPacketData = new AddonPacketData(packetIdSize);
                CurrentUpdatePacket.SetSendingAddonPacketData(addonId, addonPacketData);
            }

            return addonPacketData;
        }
    }

    /// <summary>
    /// Set (non-collection) addon data to be networked for the addon with the given ID.
    /// </summary>
    /// <param name="addonId">The ID of the addon.</param>
    /// <param name="packetId">The ID of the packet data.</param>
    /// <param name="packetIdSize">The size of the packet ID space.</param>
    /// <param name="packetData">The packet data to send.</param>
    public void SetAddonData(
        byte addonId,
        byte packetId,
        byte packetIdSize,
        IPacketData packetData
    ) {
        lock (Lock) {
            var addonPacketData = GetOrCreateAddonPacketData(addonId, packetIdSize);

            addonPacketData.PacketData[packetId] = packetData;
        }
    }

    /// <summary>
    /// Set addon data as a collection to be networked for the addon with the given ID.
    /// </summary>
    /// <param name="addonId">The ID of the addon.</param>
    /// <param name="packetId">The ID of the packet data.</param>
    /// <param name="packetIdSize">The size of the packet ID space.</param>
    /// <param name="packetData">The packet data to send.</param>
    /// <typeparam name="TPacketData">The type of the packet data in the collection.</typeparam>
    /// <exception cref="InvalidOperationException">Thrown if the packet data could not be added.</exception>
    public void SetAddonDataAsCollection<TPacketData>(
        byte addonId,
        byte packetId,
        byte packetIdSize,
        TPacketData packetData
    ) where TPacketData : IPacketData, new() {
        lock (Lock) {
            // Obtain the AddonPacketData object from the packet
            var addonPacketData = GetOrCreateAddonPacketData(addonId, packetIdSize);

            // Check whether there is already data associated with the given packet ID
            // If not, we create a new instance of PacketDataCollection and add it for that ID
            if (!addonPacketData.PacketData.TryGetValue(packetId, out var existingPacketData)) {
                existingPacketData = new PacketDataCollection<TPacketData>();
                addonPacketData.PacketData[packetId] = existingPacketData;
            }

            // Make sure that the existing packet data is a data collection and throw an exception if not
            if (!(existingPacketData is RawPacketDataCollection existingDataCollection)) {
                throw new InvalidOperationException("Could not add addon data with existing non-collection data");
            }

            // Based on whether the given packet data is a collection or not, we correctly add it to the
            // new or existing collection
            if (packetData is RawPacketDataCollection packetDataAsCollection) {
                existingDataCollection.DataInstances.AddRange(packetDataAsCollection.DataInstances);
            } else {
                existingDataCollection.DataInstances.Add(packetData);
            }
        }
    }
}
