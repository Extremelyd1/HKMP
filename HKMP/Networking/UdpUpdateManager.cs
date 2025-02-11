using System;
using System.Timers;
using Hkmp.Concurrency;
using Hkmp.Logging;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using Hkmp.Networking.Packet.Update;
using Org.BouncyCastle.Tls;
using Timer = System.Timers.Timer;

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
    /// The MTU (maximum transfer unit) to use to send packets with. If the length of a packet exceeds this, we break
    /// it up into smaller packets before sending. This ensures that we control the breaking of packets in most
    /// cases and do not rely on smaller network devices for the breaking up as this could impact performance.
    /// This size is lower than the limit for DTLS packets, since there is a slight DTLS overhead for packets.
    /// </summary>
    private const int PacketMtu = 1200;

    /// <summary>
    /// The number of sequence numbers to store in the received queue to construct ack fields with and
    /// to check against resent data.
    /// </summary>
    private const int ReceiveQueueSize = AckSize;

    /// <summary>
    /// The UDP congestion manager instance.
    /// </summary>
    private readonly UdpCongestionManager<TOutgoing, TPacketId> _udpCongestionManager;

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
    /// Timer for keeping track of when to send an update packet.
    /// </summary>
    private readonly Timer _sendTimer;

    /// <summary>
    /// Timer for keeping track of the connection timing out.
    /// </summary>
    private readonly Timer _heartBeatTimer;

    /// <summary>
    /// The last used send rate for the send timer. Used to check whether the interval of the timers needs to be
    /// updated.
    /// </summary>
    private int _lastSendRate;
    
    /// <summary>
    /// The Socket instance to use to send packets.
    /// </summary>
    public DtlsTransport DtlsTransport { get; set; }

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
    public event Action TimeoutEvent;

    /// <summary>
    /// Construct the update manager with a UDP socket.
    /// </summary>
    protected UdpUpdateManager() {
        _udpCongestionManager = new UdpCongestionManager<TOutgoing, TPacketId>(this);

        _receivedQueue = new ConcurrentFixedSizeQueue<ushort>(ReceiveQueueSize);

        CurrentUpdatePacket = new TOutgoing();

        // Construct the timers with correct intervals and register the Elapsed events
        _sendTimer = new Timer {
            AutoReset = true,
            Interval = CurrentSendRate
        };
        _sendTimer.Elapsed += OnSendTimerElapsed;

        _heartBeatTimer = new Timer {
            AutoReset = false,
            Interval = ConnectionTimeout
        };
        _heartBeatTimer.Elapsed += OnHeartBeatTimerElapsed;
    }

    /// <summary>
    /// Start the update manager and allow sending updates.
    /// </summary>
    public void StartUpdates() {
        _lastSendRate = CurrentSendRate;
        _sendTimer.Start();
        _heartBeatTimer.Start();
    }

    /// <summary>
    /// Stop sending the periodic UDP update packets after sending the current one.
    /// </summary>
    public void StopUpdates() {
        Logger.Debug("Stopping UDP updates, sending last packet");
        
        // Send the last packet
        CreateAndSendUpdatePacket();

        _sendTimer.Stop();
        _heartBeatTimer.Stop();
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

        // Reset the heart beat timer, as we have received a packet and the connection is alive
        _heartBeatTimer.Stop();
        _heartBeatTimer.Start();
    }

    /// <summary>
    /// Create and send the current update packet.
    /// </summary>
    private void CreateAndSendUpdatePacket() {
        if (DtlsTransport == null) {
            return;
        }

        var packet = new Packet.Packet();
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

            try {
                CurrentUpdatePacket.CreatePacket(packet);
            } catch (Exception e) {
                Logger.Error($"An error occurred while trying to create packet:\n{e}");
                return;
            }

            // Reset the packet by creating a new instance,
            // but keep the original instance for reliability data re-sending
            updatePacket = CurrentUpdatePacket;
            CurrentUpdatePacket = new TOutgoing();
        }

        _udpCongestionManager.OnSendPacket(_localSequence, updatePacket);

        // Increase (and potentially wrap) the current local sequence number
        _localSequence++;

        // Check if the packet exceeds (usual) MTU and break it up if so
        if (packet.Length > PacketMtu) {
            // Get the original packet's bytes as an array
            var byteArray= packet.ToArray();
            
            // Keep track of the index in the original array for copying
            var index = 0;
            // While we have not reached the end of the original array yet with the index
            while (index < byteArray.Length) {
                // Take the minimum of what's left to copy in the original array and the max MTU
                var length = System.Math.Min(byteArray.Length - index, PacketMtu);
                // Create a new array that is this calculated length
                var newBytes = new byte[length];
                // Copy over the length of bytes starting from index into the new array
                Array.Copy(byteArray, index, newBytes, 0, length);

                SendPacket(new Packet.Packet(newBytes));

                index += length;
            }

            return;
        }
        
        SendPacket(packet);
    }

    /// <summary>
    /// Callback method for when the send timer elapses. Will create and send a new update packet and update the
    /// timer interval in case the send rate changes.
    /// </summary>
    private void OnSendTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs) {
        CreateAndSendUpdatePacket();

        if (_lastSendRate != CurrentSendRate) {
            _sendTimer.Interval = CurrentSendRate;
            _lastSendRate = CurrentSendRate;
        }
    }

    /// <summary>
    /// Callback method for when the heart beat timer elapses. Will invoke the timeout event.
    /// </summary>
    private void OnHeartBeatTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs) {
        // The timer has surpassed the connection timeout value, so we call the timeout event
        TimeoutEvent?.Invoke();
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
    private void SendPacket(Packet.Packet packet) {
        var buffer = packet.ToArray();
        
        DtlsTransport?.Send(buffer, 0, buffer.Length);
    }

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
