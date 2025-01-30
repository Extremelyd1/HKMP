using System;
using System.Collections.Generic;

namespace Hkmp.Networking.Packet.Update;

/// <summary>
/// Abstract base class for the update packet.
/// </summary>
/// <typeparam name="TPacketId"></typeparam>
internal abstract class UpdatePacket<TPacketId> : BasePacket<TPacketId> where TPacketId : Enum {
    /// <summary>
    /// The sequence number of this packet.
    /// </summary>
    public ushort Sequence { get; set; }

    /// <summary>
    /// The acknowledgement number of this packet.
    /// </summary>
    public ushort Ack { get; set; }

    /// <summary>
    /// An array containing booleans that indicate whether sequence number (Ack - x) is also acknowledged
    /// for the x-th value in the array.
    /// </summary>
    public bool[] AckField { get; private set; }
    
    protected UpdatePacket() {
        AckField = new bool[UdpUpdateManager.AckSize];
    }
    
    /// <summary>
    /// Write header info into the given packet (sequence number, acknowledgement number and ack field).
    /// </summary>
    /// <param name="packet">The packet to write the header info into.</param>
    private void WriteHeaders(Packet packet) {
        packet.Write(Sequence);
        packet.Write(Ack);

        ulong ackFieldInt = 0;
        ulong currentFieldValue = 1;
        for (var i = 0; i < UdpUpdateManager.AckSize; i++) {
            if (AckField[i]) {
                ackFieldInt |= currentFieldValue;
            }

            currentFieldValue *= 2;
        }

        packet.Write(ackFieldInt);
    }

    /// <summary>
    /// Read header info from the given packet (sequence number, acknowledgement number and ack field).
    /// </summary>
    /// <param name="packet">The packet to read header info from.</param>
    private void ReadHeaders(Packet packet) {
        Sequence = packet.ReadUShort();
        Ack = packet.ReadUShort();

        // Initialize the AckField array
        AckField = new bool[UdpUpdateManager.AckSize];

        var ackFieldInt = packet.ReadULong();
        ulong currentFieldValue = 1;
        for (var i = 0; i < UdpUpdateManager.AckSize; i++) {
            AckField[i] = (ackFieldInt & currentFieldValue) != 0;

            currentFieldValue *= 2;
        }
    }

    /// <inheritdoc />
    public override void CreatePacket(Packet packet) {
        WriteHeaders(packet);
        
        base.CreatePacket(packet);
    }

    /// <inheritdoc />
    public override bool ReadPacket(Packet packet) {
        // TODO: maybe get rid of exception catching in packet reading and rely on bool return values for all
        // packet reading methods (including Packet class)
        try {
            ReadHeaders(packet);
        } catch {
            return false;
        }

        return base.ReadPacket(packet);
    }
    
    /// <summary>
    /// Set the reliable packet data contained in the lost packet as resend data in this one.
    /// </summary>
    /// <param name="lostPacket">The update packet instance that was lost.</param>
    public void SetLostReliableData(UpdatePacket<TPacketId> lostPacket) {
        // Retrieve the lost packet data
        var lostPacketData = lostPacket.GetPacketData();

        // Finally, put the packet data dictionary in the resend dictionary keyed by its sequence number
        ResendPacketData[lostPacket.Sequence] = CopyReliableDataDict(
            lostPacketData,
            t => NormalPacketData.ContainsKey(t)
        );

        // Retrieve the lost addon data
        var lostAddonData = lostPacket.GetAddonPacketData();
        // Create a new dictionary of addon data in which we store all reliable data from the lost packet
        // for all addons in the dictionary
        var toResendAddonData = new Dictionary<byte, AddonPacketData>();

        foreach (var idLostDataPair in lostAddonData) {
            var addonId = idLostDataPair.Key;
            var addonPacketData = idLostDataPair.Value;

            // Construct a new AddonPacketData instance that holds the reliable data only
            var newAddonPacketData = addonPacketData.GetEmptyCopy();
            newAddonPacketData.PacketData = CopyReliableDataDict(
                addonPacketData.PacketData,
                rawPacketId =>
                    AddonPacketData.TryGetValue(addonId, out var existingAddonData)
                    && existingAddonData.PacketData.ContainsKey(rawPacketId));

            toResendAddonData[addonId] = newAddonPacketData;
        }

        // Put the addon data dictionary in the resend dictionary keyed by its sequence number
        ResendAddonPacketData[lostPacket.Sequence] = toResendAddonData;
    }
    
    /// <summary>
    /// Copy all reliable data in the given dictionary of lost packet data into a new dictionary.
    /// </summary>
    /// <param name="lostPacketData">The dictionary containing all packet data from a lost packet.</param>
    /// <param name="reliabilityCheck">Function that checks whether for a given key there is newer data
    /// available. If it returns true, lost data will be dropped.</param>
    /// <typeparam name="TKey">The key parameter of the dictionaries to copy.</typeparam>
    /// <returns>A new dictionary containing only the reliable data.</returns>
    private Dictionary<TKey, IPacketData> CopyReliableDataDict<TKey>(
        Dictionary<TKey, IPacketData> lostPacketData,
        Func<TKey, bool> reliabilityCheck
    ) {
        // Create a new dictionary of packet data in which we store all reliable data
        var reliablePacketData = new Dictionary<TKey, IPacketData>();

        foreach (var keyDataPair in lostPacketData) {
            var key = keyDataPair.Key;
            var data = keyDataPair.Value;

            // Check if the packet data is supposed to be reliable
            if (!data.IsReliable) {
                continue;
            }

            // Check whether we can drop it since a newer version of that data already exists
            if (data.DropReliableDataIfNewerExists && reliabilityCheck(key)) {
                continue;
            }

            // Logger.Info($"  Resending {data.GetType()} data");
            reliablePacketData[key] = data;
        }

        return reliablePacketData;
    }
}
