using System;
using System.Collections.Generic;

namespace Hkmp.Networking.Packet;

/// <summary>
/// Class that holds all packet data for a specific addon.
/// </summary>
internal class AddonPacketData {
    /// <summary>
    /// Dictionary mapping packet IDs to packet data instances.
    /// </summary>
    public Dictionary<byte, IPacketData> PacketData { get; set; }

    /// <summary>
    /// The size of the packet ID space.
    /// </summary>
    public byte PacketIdSize { get; }

    /// <summary>
    /// Enumerator to go over each packet ID in the packet ID space of this addon. 
    /// </summary>
    public IEnumerator<byte> PacketIdEnumerator {
        get {
            if (_packetIdArray == null) {
                // Create an array containing all possible IDs for this addon
                _packetIdArray = new byte[PacketIdSize];
                for (byte i = 0; i < PacketIdSize; i++) {
                    _packetIdArray[i] = i;
                }
            }

            // Return a fresh enumerator for the ID space
            return ((IEnumerable<byte>) _packetIdArray).GetEnumerator();
        }
    }

    /// <summary>
    /// Byte array containing all raw packet ID bytes.
    /// </summary>
    private byte[] _packetIdArray;

    /// <summary>
    /// Construct this addon packet data class with the packet ID size.
    /// </summary>
    /// <param name="packetIdSize">The size of the packet ID space.</param>
    public AddonPacketData(byte packetIdSize) {
        PacketData = new Dictionary<byte, IPacketData>();

        PacketIdSize = packetIdSize;
    }

    /// <summary>
    /// Return an empty copy of this class with the same packet ID size.
    /// </summary>
    /// <returns></returns>
    public AddonPacketData GetEmptyCopy() {
        return new AddonPacketData(PacketIdSize);
    }
}

/// <summary>
/// Class that stores information about addons that is needed to read addon data from raw packet instances.
/// </summary>
internal class AddonPacketInfo {
    /// <summary>
    /// The function that instantiates IPacketData instances from a packet ID as byte.
    /// </summary>
    public Func<byte, IPacketData> PacketDataInstantiator { get; }

    /// <summary>
    /// The size of the packet ID space.
    /// </summary>
    public byte PacketIdSize { get; }

    /// <summary>
    /// Construct the addon info with the given instantiator and packet ID size.
    /// </summary>
    /// <param name="packetDataInstantiator">The instantiation function.</param>
    /// <param name="packetIdSize">The size of the packet ID space.</param>
    public AddonPacketInfo(Func<byte, IPacketData> packetDataInstantiator, byte packetIdSize) {
        PacketDataInstantiator = packetDataInstantiator;
        PacketIdSize = packetIdSize;
    }
}
