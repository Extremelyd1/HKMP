using System;
using System.Collections.Generic;
using Hkmp.Logging;

namespace Hkmp.Networking.Packet;

/// <summary>
/// Abstract base class for the packets containing structured packet data.
/// </summary>
/// <typeparam name="TPacketId">The enum type for packet IDs in this packet.</typeparam>
internal abstract class BasePacket<TPacketId> where TPacketId : Enum {
    // ReSharper disable once StaticMemberInGenericType
    /// <summary>
    /// A dictionary containing addon packet info per addon ID in order to read and convert raw addon
    /// packet data into IPacketData instances.
    /// </summary>
    public static Dictionary<byte, AddonPacketInfo> AddonPacketInfoDict { get; } = new();

    // TODO: refactor these dictionaries into a class that contains them for readability
    /// <summary>
    /// Normal non-resend packet data.
    /// </summary>
    protected readonly Dictionary<TPacketId, IPacketData> NormalPacketData;

    /// <summary>
    /// Packet data from addons indexed by their ID.
    /// </summary>
    protected readonly Dictionary<byte, AddonPacketData> AddonPacketData;

    /// <summary>
    /// The combination of normal and resent packet data cached in case it needs to be queried multiple times.
    /// </summary>
    protected Dictionary<TPacketId, IPacketData> CachedAllPacketData;

    /// <summary>
    /// The combination of addon and resent addon data cached in case it needs to be queried multiple times.
    /// </summary>
    protected Dictionary<byte, AddonPacketData> CachedAllAddonData;

    /// <summary>
    /// Whether the dictionary containing all packet data is cached already or needs to be calculated first.
    /// </summary>
    protected bool IsAllPacketDataCached;

    /// <summary>
    /// Whether this packet contains data that needs to be reliable.
    /// </summary>
    public bool ContainsReliableData { get; protected set; }

    /// <summary>
    /// Construct the update packet with the given raw packet instance to read from.
    /// </summary>
    protected BasePacket() {
        NormalPacketData = new Dictionary<TPacketId, IPacketData>();
        AddonPacketData = new Dictionary<byte, AddonPacketData>();
    }

    /// <summary>
    /// Write the given dictionary of normal or resent packet data into the given raw packet instance.
    /// </summary>
    /// <param name="packet">The packet to write into.</param>
    /// <param name="packetData">Dictionary of packet data to write.</param>
    /// <returns>true if any of the data written was reliable; otherwise false.</returns>
    protected bool WritePacketData(
        Packet packet,
        Dictionary<TPacketId, IPacketData> packetData
    ) {
        var enumValues = (TPacketId[]) Enum.GetValues(typeof(TPacketId));
        var packetIdSize = (byte) enumValues.Length;

        return WritePacketData(
            packet,
            packetData,
            enumValues,
            packetIdSize
        );
    }

    /// <summary>
    /// Write the data in the given instance of AddonPacketData into the given raw packet instance.
    /// </summary>
    /// <param name="packet">The packet to write into.</param>
    /// <param name="addonPacketData">AddonPacketData instance from which to data should be written.</param>
    /// <returns>true if any of the data written was reliable; otherwise false.</returns>
    private bool WriteAddonPacketData(
        Packet packet,
        AddonPacketData addonPacketData
    ) => WritePacketData(
        packet,
        addonPacketData.PacketData,
        addonPacketData.PacketIdEnumerable,
        addonPacketData.PacketIdSize
    );

    /// <summary>
    /// Write the given dictionary of packet data into the given raw packet instance.
    /// </summary>
    /// <param name="packet">The packet to write into.</param>
    /// <param name="packetData">The dictionary containing packet data to write in the packet.</param>
    /// <param name="keyEnumerable">An enumerator that enumerates over all possible keys in the dictionary.</param>
    /// <param name="keySpaceSize">The exact size of the key space.</param>
    /// <typeparam name="TKey">Dictionary key parameter and enumerator parameter.</typeparam>
    /// <returns>true if any of the data written was reliable; otherwise false.</returns>
    private bool WritePacketData<TKey>(
        Packet packet,
        Dictionary<TKey, IPacketData> packetData,
        IEnumerable<TKey> keyEnumerable,
        byte keySpaceSize
    ) {
        // Keep track of the bit flag in an unsigned long, which is the largest integer implicit type allowed
        ulong idFlag = 0;
        // Also keep track of the value of the current bit in an unsigned long
        ulong currentTypeValue = 1;

        var keyEnumerator = keyEnumerable.GetEnumerator();
        while (keyEnumerator.MoveNext()) {
            var key = keyEnumerator.Current;

            // Update the bit in the flag if the current value is included in the dictionary
            if (key != null && packetData.ContainsKey(key)) {
                idFlag |= currentTypeValue;
            }

            // Always increase the current bit
            currentTypeValue *= 2;
        }

        // Based on the size of the values space, we cast to the smallest primitive that can hold the flag
        // and write it to the packet
        if (keySpaceSize <= 8) {
            packet.Write((byte) idFlag);
        } else if (keySpaceSize <= 16) {
            packet.Write((ushort) idFlag);
        } else if (keySpaceSize <= 32) {
            packet.Write((uint) idFlag);
        } else if (keySpaceSize <= 64) {
            packet.Write(idFlag);
        }

        // Let each individual piece of packet data write themselves into the packet
        // and keep track of whether any of them need to be reliable
        var containsReliableData = false;
        // We loop over the possible IDs in the order from the given array to make it
        // consistent between server and client
        keyEnumerator.Reset();
        while (keyEnumerator.MoveNext()) {
            var key = keyEnumerator.Current;

            if (key != null && packetData.TryGetValue(key, out var iPacketData)) {
                iPacketData.WriteData(packet);

                if (iPacketData.IsReliable) {
                    containsReliableData = true;
                }
            }
        }
        
        keyEnumerator.Dispose();

        return containsReliableData;
    }

    /// <summary>
    /// Write the given dictionary containing addon data for all addons in the given packet.
    /// </summary>
    /// <param name="packet">The raw packet instance to write into.</param>
    /// <param name="addonDataDict">The dictionary containing all addon data to write.</param>
    /// <returns>true if any of the data written was reliable; otherwise false.</returns>
    protected bool WriteAddonDataDict(
        Packet packet,
        Dictionary<byte, AddonPacketData> addonDataDict
    ) {
        // Normally, we put the length of the addon packet data as a byte in the packet.
        // There should only be a maximum of 255 addons, so the length should fit in a byte.
        // But we don't know which addon data is going to get written correctly and which throws
        // an exception, so for now we hold off on writing anything yet, but keep track of how
        // many instances we are writing
        var addonPacketDataCount = (byte) addonDataDict.Count;

        // We also construct a temporary packet that we use to write the progress of all
        // addon packet data into. This temp packet we can then later write into the original
        // packet as soon as we know the number of successful addon packet data instances we
        // have written.
        var addonPacketDataPacket = new Packet();

        // Also keep track of whether we have written reliable data
        var containsReliable = false;

        // Add the packet data per addon ID
        foreach (var addonPacketDataPair in addonDataDict) {
            var addonId = addonPacketDataPair.Key;
            var addonPacketData = addonPacketDataPair.Value;

            // Create a new packet to try and write addon packet data into
            var addonPacket = new Packet();
            bool addonContainsReliable;
            try {
                addonContainsReliable = WriteAddonPacketData(
                    addonPacket,
                    addonPacketData
                );
            } catch (Exception e) {
                // If the addon data writing throws an exception, we skip it entirely and since we
                // wrote it in a separate packet, it has no impact on the regular packet
                Logger.Debug($"Addon with ID {addonId} has thrown an exception while writing addon packet data:\n{e}");
                // We decrease the count of addon packet data's we write, so we know how many are actually in
                // final packet
                addonPacketDataCount--;
                continue;
            }

            // Prepend the length of the addon packet data to the addon packet
            addonPacket.WriteLength();

            // Now we add the addon ID to the addon packet data packet and then the contents of the addon packet
            addonPacketDataPacket.Write(addonId);
            addonPacketDataPacket.Write(addonPacket.ToArray());

            // Potentially update whether this packet contains reliable data now
            containsReliable |= addonContainsReliable;
        }

        // Finally write the resulting size and the addon packet data itself in the regular packet
        packet.Write(addonPacketDataCount);
        packet.Write(addonPacketDataPacket.ToArray());

        return containsReliable;
    }

    /// <summary>
    /// Read raw data from the given packet into the given packet data dictionary.
    /// This method is only for normal and resent packet data, not for addon packet data.
    /// </summary>
    /// <param name="packet">The raw packet instance to read from.</param>
    /// <param name="packetData">The dictionary of packet data to write the read data into.</param>
    protected void ReadPacketData(
        Packet packet,
        Dictionary<TPacketId, IPacketData> packetData
    ) {
        // Figure out the size of the packet ID enum
        var enumValues = (TPacketId[]) Enum.GetValues(typeof(TPacketId));
        var packetIdSize = (byte) enumValues.Length;

        // Read the byte flag representing which packets are included in this update
        // The number of bytes we read is dependent on the size of the enum
        ulong dataPacketIdFlag = 0;
        if (packetIdSize <= 8) {
            dataPacketIdFlag = packet.ReadByte();
        } else if (packetIdSize <= 16) {
            dataPacketIdFlag = packet.ReadUShort();
        } else if (packetIdSize <= 32) {
            dataPacketIdFlag = packet.ReadUInt();
        } else if (packetIdSize <= 64) {
            dataPacketIdFlag = packet.ReadULong();
        }

        // Keep track of value of current bit
        ulong currentTypeValue = 1;

        var packetIdValues = Enum.GetValues(typeof(TPacketId));
        foreach (TPacketId packetId in packetIdValues) {
            // If this bit was set in our flag, we add the type to the list
            if ((dataPacketIdFlag & currentTypeValue) != 0) {
                var iPacketData = InstantiatePacketDataFromId(packetId);
                iPacketData?.ReadData(packet);

                packetData[packetId] = iPacketData;
            }

            // Increase the value of current bit
            currentTypeValue *= 2;
        }
    }

    /// <summary>
    /// Read raw addon data from the given packet into the given addon data dictionary.
    /// </summary>
    /// <param name="packet">The raw packet instance to read from.</param>
    /// <param name="packetIdSize">The size of the packet ID space.</param>
    /// <param name="packetDataInstantiator">A function that instantiate IPacketData implementations given a
    /// packet ID in byte form.</param>
    /// <param name="packetData">The dictionary of addon data to write the read data into.</param>
    /// <exception cref="Exception">Thrown if the given instantiation function returns null.</exception>
    private void ReadAddonPacketData(
        Packet packet,
        byte packetIdSize,
        Func<byte, IPacketData> packetDataInstantiator,
        Dictionary<byte, IPacketData> packetData
    ) {
        // Read the byte flag representing which packets are included in this update
        // This flag may come in different primitives based on the size of the packet
        // ID space
        ulong dataPacketIdFlag;

        if (packetIdSize <= 8) {
            dataPacketIdFlag = packet.ReadByte();
        } else if (packetIdSize <= 16) {
            dataPacketIdFlag = packet.ReadUShort();
        } else if (packetIdSize <= 32) {
            dataPacketIdFlag = packet.ReadUInt();
        } else if (packetIdSize <= 64) {
            dataPacketIdFlag = packet.ReadULong();
        } else {
            // This should never happen, but in case it does, we throw an exception
            throw new Exception("Addon packet ID space size is larger than expected");
        }

        // Keep track of value of current bit in the largest integer primitive
        ulong currentTypeValue = 1;

        for (byte packetId = 0; packetId < packetIdSize; packetId++) {
            // If this bit was set in our flag, we add the type to the list
            if ((dataPacketIdFlag & currentTypeValue) != 0) {
                IPacketData iPacketData;

                // Wrap this in try catch so we can add information on what happened when the addon submitted
                // packet data instantiator throws
                try {
                    iPacketData = packetDataInstantiator.Invoke(packetId);
                } catch (Exception e) {
                    throw new Exception(
                        $"Packet data instantiator for addon data threw an exception:\n{e}");
                }

                if (iPacketData == null) {
                    throw new Exception("Addon packet data instantiating method returned null");
                }

                iPacketData.ReadData(packet);

                packetData[packetId] = iPacketData;
            }

            // Increase the value of current bit
            currentTypeValue *= 2;
        }
    }

    /// <summary>
    /// Read all raw addon data from the given packet into the given dictionary containing entries for all addons.
    /// </summary>
    /// <param name="packet">The raw packet instance to read from.</param>
    /// <param name="addonDataDict">The dictionary for all addon data to write the read data into.</param>
    /// <exception cref="Exception">Thrown if any part of reading the data throws.</exception>
    protected void ReadAddonDataDict(
        Packet packet,
        Dictionary<byte, AddonPacketData> addonDataDict
    ) {
        // Read the number of the addon packet data instances from the packet
        var numAddonData = packet.ReadByte();

        while (numAddonData-- > 0) {
            var addonId = packet.ReadByte();

            if (!AddonPacketInfoDict.TryGetValue(addonId, out var addonPacketInfo)) {
                // If the addon packet info for this addon could not be found, we need to throw an exception
                throw new Exception($"Addon with ID {addonId} has no defined addon packet info");
            }

            // Read the length of the addon packet data for this addon
            var addonDataLength = packet.ReadUShort();

            // Read exactly as many bytes as was indicated by the previously read value
            var addonDataBytes = packet.ReadBytes(addonDataLength);

            // Create a new packet object with the given bytes so we can sandbox the reading
            var addonPacket = new Packet(addonDataBytes);

            // Create a new instance of AddonPacketData to read packet data into and eventually
            // add to this packet instance's dictionary
            var addonPacketData = new AddonPacketData(addonPacketInfo.PacketIdSize);

            try {
                ReadAddonPacketData(
                    addonPacket,
                    addonPacketInfo.PacketIdSize,
                    addonPacketInfo.PacketDataInstantiator,
                    addonPacketData.PacketData
                );
            } catch (Exception e) {
                // If the addon data reading throws an exception, we skip it entirely and since
                // we read it into a separate packet, it has no impact on the regular packet
                Logger.Debug($"Addon with ID {addonId} has thrown an exception while reading addon packet data:\n{e}");
                continue;
            }

            addonDataDict[addonId] = addonPacketData;
        }
    }

    /// <summary>
    /// Create a raw packet out of the data contained in this class by writing to the given packet.
    /// </summary>
    /// <param name="packet">The packet instance to write the data to.</param>
    public virtual void CreatePacket(Packet packet) {
        // Write the normal packet data into the packet and keep track of whether this packet contains reliable data
        // now
        ContainsReliableData = WritePacketData(packet, NormalPacketData);

        // Write the addon packet data into the packet and add to the fact that this packet contains reliable data now
        ContainsReliableData |= WriteAddonDataDict(packet, AddonPacketData);
    }

    /// <summary>
    /// Read the raw packet contents into easy to access dictionaries.
    /// </summary>
    /// <param name="packet">The packet instance to read the data from.</param>
    /// <returns>False if the packet cannot be successfully read due to malformed data; otherwise true.</returns>
    public virtual bool ReadPacket(Packet packet) {
        try {
            // Read the normal packet data from the packet
            ReadPacketData(packet, NormalPacketData);

            // Read the addon packet data
            ReadAddonDataDict(packet, AddonPacketData);
        } catch (Exception e) {
            Logger.Debug($"Exception while reading base packet:\n{e}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Tries to get packet data that is going to be sent with the given packet ID.
    /// </summary>
    /// <param name="packetId">The packet ID to try and get.</param>
    /// <param name="packetData">Variable to store the retrieved data in. Null if this method returns false.</param>
    /// <returns>true if the packet data exists and will be stored in the packetData variable; otherwise
    /// false.</returns>
    public bool TryGetSendingPacketData(TPacketId packetId, out IPacketData packetData) {
        return NormalPacketData.TryGetValue(packetId, out packetData);
    }

    /// <summary>
    /// Tries to get addon packet data for the addon with the given ID.
    /// </summary>
    /// <param name="addonId">The ID of the addon to get the data for.</param>
    /// <param name="addonPacketData">An instance of AddonPacketData corresponding to the given ID.
    /// Null if this method returns false.</param>
    /// <returns>true if the addon packet data exists and will be stored in the addonPacketData variable;
    /// otherwise false.</returns>
    public bool TryGetSendingAddonPacketData(byte addonId, out AddonPacketData addonPacketData) {
        return AddonPacketData.TryGetValue(addonId, out addonPacketData);
    }

    /// <summary>
    /// Sets the given packetData with the given packet ID for sending.
    /// </summary>
    /// <param name="packetId">The packet ID to set data for.</param>
    /// <param name="packetData">The packet data to set.</param>
    public void SetSendingPacketData(TPacketId packetId, IPacketData packetData) {
        NormalPacketData[packetId] = packetData;
    }

    /// <summary>
    /// Sets the given addonPacketData with the given addon ID for sending.
    /// </summary>
    /// <param name="addonId">The addon ID to set data for.</param>
    /// <param name="packetData">Instance of AddonPacketData to set.</param>
    public void SetSendingAddonPacketData(byte addonId, AddonPacketData packetData) {
        AddonPacketData[addonId] = packetData;
    }

    /// <summary>
    /// Get all the packet data contained in this packet, normal and resent data (but not addon data).
    /// </summary>
    /// <returns>A dictionary containing packet IDs mapped to packet data.</returns>
    public Dictionary<TPacketId, IPacketData> GetPacketData() {
        if (!IsAllPacketDataCached) {
            CacheAllPacketData();
        }

        return CachedAllPacketData;
    }

    /// <summary>
    /// Get the addon packet data in this packet, normal addon and resent data.
    /// </summary>
    /// <returns>A dictionary containing addon IDs mapped to addon packet data.</returns>
    public Dictionary<byte, AddonPacketData> GetAddonPacketData() {
        if (!IsAllPacketDataCached) {
            CacheAllPacketData();
        }

        return CachedAllAddonData;
    }

    /// <summary>
    /// Computes all packet data (normal, resent, addon and addon resent data), caches it and sets a boolean
    /// indicating that this cache is now available.
    /// </summary>
    protected virtual void CacheAllPacketData() {
        // Construct a new dictionary for all the data
        CachedAllPacketData = new Dictionary<TPacketId, IPacketData>();

        // Iteratively add the normal packet data
        foreach (var packetIdDataPair in NormalPacketData) {
            CachedAllPacketData.Add(packetIdDataPair.Key, packetIdDataPair.Value);
        }

        // Do the same as above but for addon data
        CachedAllAddonData = new Dictionary<byte, AddonPacketData>();

        // Iteratively add the addon data
        foreach (var addonIdDataPair in AddonPacketData) {
            CachedAllAddonData.Add(addonIdDataPair.Key, addonIdDataPair.Value);
        }
    }

    /// <summary>
    /// Get an instantiation of IPacketData for the given packet ID.
    /// </summary>
    /// <param name="packetId">The packet ID to get an instance for.</param>
    /// <returns>A new instance of IPacketData.</returns>
    protected abstract IPacketData InstantiatePacketDataFromId(TPacketId packetId);
}
