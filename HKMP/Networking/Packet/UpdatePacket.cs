using System;
using System.Collections.Generic;
using Hkmp.Logging;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Packet;

/// <summary>
/// Abstract base class for the update packet.
/// </summary>
/// <typeparam name="T"></typeparam>
internal abstract class UpdatePacket<T> where T : Enum {
    // ReSharper disable once StaticMemberInGenericType
    /// <summary>
    /// A dictionary containing addon packet info per addon ID in order to read and convert raw addon
    /// packet data into IPacketData instances.
    /// </summary>
    public static Dictionary<byte, AddonPacketInfo> AddonPacketInfoDict { get; } =
        new Dictionary<byte, AddonPacketInfo>();

    /// <summary>
    /// The underlying raw packet instance, only used for reading data out of.
    /// </summary>
    private readonly Packet _packet;

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

    // TODO: refactor these dictionaries into a class that contains them for readability
    /// <summary>
    /// Normal non-resend packet data.
    /// </summary>
    private readonly Dictionary<T, IPacketData> _normalPacketData;

    /// <summary>
    /// Resend packet data indexed by sequence number it originates from.
    /// </summary>
    private readonly Dictionary<ushort, Dictionary<T, IPacketData>> _resendPacketData;

    /// <summary>
    /// Packet data from addons indexed by their ID.
    /// </summary>
    private readonly Dictionary<byte, AddonPacketData> _addonPacketData;

    /// <summary>
    /// Resend addon packet data indexed by sequence number it originates from.
    /// </summary>
    private readonly Dictionary<ushort, Dictionary<byte, AddonPacketData>> _resendAddonPacketData;

    /// <summary>
    /// The combination of normal and resent packet data cached in case it needs to be queried multiple times.
    /// </summary>
    private Dictionary<T, IPacketData> _cachedAllPacketData;

    /// <summary>
    /// The combination of addon and resent addon data cached in case it needs to be queried multiple times.
    /// </summary>
    private Dictionary<byte, AddonPacketData> _cachedAllAddonData;

    /// <summary>
    /// Whether the dictionary containing all packet data is cached already or needs to be calculated first.
    /// </summary>
    private bool _isAllPacketDataCached;

    /// <summary>
    /// Whether this packet contains data that needs to be reliable.
    /// </summary>
    private bool _containsReliableData;

    /// <summary>
    /// Construct the update packet with the given raw packet instance to read from.
    /// </summary>
    /// <param name="packet">The raw packet instance.</param>
    protected UpdatePacket(Packet packet) {
        _packet = packet;

        AckField = new bool[UdpUpdateManager.AckSize];

        _normalPacketData = new Dictionary<T, IPacketData>();
        _resendPacketData = new Dictionary<ushort, Dictionary<T, IPacketData>>();
        _addonPacketData = new Dictionary<byte, AddonPacketData>();
        _resendAddonPacketData = new Dictionary<ushort, Dictionary<byte, AddonPacketData>>();
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

    /// <summary>
    /// Write the given dictionary of normal or resent packet data into the given raw packet instance.
    /// </summary>
    /// <param name="packet">The packet to write into.</param>
    /// <param name="packetData">Dictionary of packet data to write.</param>
    /// <returns>true if any of the data written was reliable; otherwise false.</returns>
    private bool WritePacketData(
        Packet packet,
        Dictionary<T, IPacketData> packetData
    ) {
        var enumValues = (T[]) Enum.GetValues(typeof(T));
        var enumerator = ((IEnumerable<T>) enumValues).GetEnumerator();
        var packetIdSize = (byte) enumValues.Length;

        return WritePacketData(
            packet,
            packetData,
            enumerator,
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
        addonPacketData.PacketIdEnumerator,
        addonPacketData.PacketIdSize
    );

    /// <summary>
    /// Write the given dictionary of packet data into the given raw packet instance.
    /// </summary>
    /// <param name="packet">The packet to write into.</param>
    /// <param name="packetData">The dictionary containing packet data to write in the packet.</param>
    /// <param name="keyEnumerator">An enumerator that enumerates over all possible keys in the dictionary.</param>
    /// <param name="keySpaceSize">The exact size of the key space.</param>
    /// <typeparam name="TKey">Dictionary key parameter and enumerator parameter.</typeparam>
    /// <returns>true if any of the data written was reliable; otherwise false.</returns>
    private bool WritePacketData<TKey>(
        Packet packet,
        Dictionary<TKey, IPacketData> packetData,
        IEnumerator<TKey> keyEnumerator,
        byte keySpaceSize
    ) {
        // Keep track of the bit flag in an unsigned long, which is the largest integer implicit type allowed
        ulong idFlag = 0;
        // Also keep track of the value of the current bit in an unsigned long
        ulong currentTypeValue = 1;

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

        return containsReliableData;
    }

    /// <summary>
    /// Write the given dictionary containing addon data for all addons in the given packet.
    /// </summary>
    /// <param name="packet">The raw packet instance to write into.</param>
    /// <param name="addonDataDict">The dictionary containing all addon data to write.</param>
    /// <returns>true if any of the data written was reliable; otherwise false.</returns>
    private bool WriteAddonDataDict(
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
                // We decrease the count of addon packet datas we write, so we know how many are actually in
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
    private void ReadPacketData(
        Packet packet,
        Dictionary<T, IPacketData> packetData
    ) {
        // Read the byte flag representing which packets
        // are included in this update
        var dataPacketIdFlag = packet.ReadUShort();
        // Keep track of value of current bit
        var currentTypeValue = 1;

        var packetIdValues = Enum.GetValues(typeof(T));
        foreach (T packetId in packetIdValues) {
            // If this bit was set in our flag, we add the type to the list
            if ((dataPacketIdFlag & currentTypeValue) != 0) {
                var iPacketData = InstantiatePacketDataFromId(packetId);
                iPacketData?.ReadData(_packet);

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
    /// <exception cref="Exception">Thrown if the any part of reading the data throws.</exception>
    private void ReadAddonDataDict(
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
    /// Create a raw packet out of the data contained in this class.
    /// </summary>
    /// <returns>A new packet instance containing all data.</returns>
    public Packet CreatePacket() {
        var packet = new Packet();

        WriteHeaders(packet);

        // Write the normal packet data into the packet and keep track of whether this packet
        // contains reliable data now
        _containsReliableData = WritePacketData(packet, _normalPacketData);

        // Put the length of the resend data as a ushort in the packet
        var resendLength = (ushort) _resendPacketData.Count;
        if (_resendPacketData.Count > ushort.MaxValue) {
            resendLength = ushort.MaxValue;

            Logger.Error("Length of resend packet data dictionary does not fit in ushort");
        }

        packet.Write(resendLength);

        // Add each entry of lost data to resend to the packet
        foreach (var seqPacketDataPair in _resendPacketData) {
            var seq = seqPacketDataPair.Key;
            var packetData = seqPacketDataPair.Value;

            // Make sure to not put more resend data in the packet than we specified
            if (resendLength-- == 0) {
                break;
            }

            // First write the sequence number it belongs to
            packet.Write(seq);

            // Then write the reliable packet data and note that this packet now contains reliable data
            WritePacketData(packet, packetData);
            _containsReliableData = true;
        }

        _containsReliableData |= WriteAddonDataDict(packet, _addonPacketData);

        // Put the length of the addon resend data as a ushort in the packet
        resendLength = (ushort) _resendAddonPacketData.Count;
        if (_resendAddonPacketData.Count > ushort.MaxValue) {
            resendLength = ushort.MaxValue;

            Logger.Error("Length of addon resend packet data dictionary does not fit in ushort");
        }

        packet.Write(resendLength);

        // Add each entry of lost addon data to resend to the packet
        foreach (var seqAddonDictPair in _resendAddonPacketData) {
            var seq = seqAddonDictPair.Key;
            var addonDataDict = seqAddonDictPair.Value;

            // Make sure to not put more resend data in the packet than we specified
            if (resendLength-- == 0) {
                break;
            }

            // First write the sequence number it belongs to
            packet.Write(seq);

            // Then write the reliable addon data for all addons and note that this packet
            // now contains reliable data
            WriteAddonDataDict(packet, addonDataDict);
            _containsReliableData = true;
        }

        packet.WriteLength();

        return packet;
    }

    /// <summary>
    /// Read the raw packet contents into easy to access dictionaries.
    /// </summary>
    /// <returns>false if the packet cannot be successfully read due to malformed data; otherwise true.</returns>
    public bool ReadPacket() {
        try {
            ReadHeaders(_packet);

            // Read the normal packet data from the packet
            ReadPacketData(_packet, _normalPacketData);

            // Read the length of the resend data
            var resendLength = _packet.ReadUShort();

            while (resendLength-- > 0) {
                // Read the sequence number of the packet it was lost from
                var seq = _packet.ReadUShort();

                // Create a new dictionary for the packet data and read the data from the packet into it
                var packetData = new Dictionary<T, IPacketData>();
                ReadPacketData(_packet, packetData);

                // Input the data into the resend dictionary keyed by its sequence number
                _resendPacketData[seq] = packetData;
            }

            // Read the addon packet data (non-resend)
            ReadAddonDataDict(_packet, _addonPacketData);

            // Read the length of the addon resend data
            resendLength = _packet.ReadUShort();

            while (resendLength-- > 0) {
                // Read the sequence number of the packet it was lost from
                var seq = _packet.ReadUShort();

                // Create a new dictionary for the addon data and read the data from the packet into it
                var addonDataDict = new Dictionary<byte, AddonPacketData>();
                ReadAddonDataDict(_packet, addonDataDict);

                // Input the dictionary into the resend dictionary keyed by its sequence number
                _resendAddonPacketData[seq] = addonDataDict;
            }
        } catch {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Whether this packet contains data that needs to be reliable.
    /// </summary>
    /// <returns>true if the packet contains reliable data; otherwise false.</returns>
    public bool ContainsReliableData() {
        return _containsReliableData;
    }

    /// <summary>
    /// Set the reliable packet data contained in the lost packet as resend data in this one.
    /// </summary>
    /// <param name="lostPacket">The update packet instance that was lost.</param>
    public void SetLostReliableData(UpdatePacket<T> lostPacket) {
        // Retrieve the lost packet data
        var lostPacketData = lostPacket.GetPacketData();

        // Finally, put the packet data dictionary in the resend dictionary keyed by its sequence number
        _resendPacketData[lostPacket.Sequence] = CopyReliableDataDict(
            lostPacketData,
            t => _normalPacketData.ContainsKey(t)
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
                    _addonPacketData.TryGetValue(addonId, out var existingAddonData)
                    && existingAddonData.PacketData.ContainsKey(rawPacketId));

            toResendAddonData[addonId] = newAddonPacketData;
        }

        // Put the addon data dictionary in the resend dictionary keyed by its sequence number
        _resendAddonPacketData[lostPacket.Sequence] = toResendAddonData;
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

    /// <summary>
    /// Tries to get packet data that is going to be sent with the given packet ID.
    /// </summary>
    /// <param name="packetId">The packet ID to try and get.</param>
    /// <param name="packetData">Variable to store the retrieved data in. Null if this method returns false.</param>
    /// <returns>true if the packet data exists and will be stored in the packetData variable; otherwise
    /// false.</returns>
    public bool TryGetSendingPacketData(T packetId, out IPacketData packetData) {
        return _normalPacketData.TryGetValue(packetId, out packetData);
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
        return _addonPacketData.TryGetValue(addonId, out addonPacketData);
    }

    /// <summary>
    /// Sets the given packetData with the given packet ID for sending.
    /// </summary>
    /// <param name="packetId">The packet ID to set data for.</param>
    /// <param name="packetData">The packet data to set.</param>
    public void SetSendingPacketData(T packetId, IPacketData packetData) {
        _normalPacketData[packetId] = packetData;
    }

    /// <summary>
    /// Sets the given addonPacketData with the given addon ID for sending.
    /// </summary>
    /// <param name="addonId">The addon ID to set data for.</param>
    /// <param name="packetData">Instance of AddonPacketData to set.</param>
    public void SetSendingAddonPacketData(byte addonId, AddonPacketData packetData) {
        _addonPacketData[addonId] = packetData;
    }

    /// <summary>
    /// Get all the packet data contained in this packet, normal and resent data (but not addon data).
    /// </summary>
    /// <returns>A dictionary containing packet IDs mapped to packet data.</returns>
    public Dictionary<T, IPacketData> GetPacketData() {
        if (!_isAllPacketDataCached) {
            CacheAllPacketData();
        }

        return _cachedAllPacketData;
    }

    /// <summary>
    /// Get the addon packet data in this packet, normal addon and resent data.
    /// </summary>
    /// <returns>A dictionary containing addon IDs mapped to addon packet data.</returns>
    public Dictionary<byte, AddonPacketData> GetAddonPacketData() {
        if (!_isAllPacketDataCached) {
            CacheAllPacketData();
        }

        return _cachedAllAddonData;
    }

    /// <summary>
    /// Computes all packet data (normal, resent, addon and addon resent data), caches it and sets a boolean
    /// indicating that this cache is now available.
    /// </summary>
    private void CacheAllPacketData() {
        // Construct a new dictionary for all the data
        _cachedAllPacketData = new Dictionary<T, IPacketData>();

        // Iteratively add the normal packet data
        foreach (var packetIdDataPair in _normalPacketData) {
            _cachedAllPacketData.Add(packetIdDataPair.Key, packetIdDataPair.Value);
        }

        void AddResendData<TKey>(
            Dictionary<TKey, IPacketData> dataDict,
            Dictionary<TKey, IPacketData> cachedData
        ) {
            foreach (var packetIdDataPair in dataDict) {
                // Get the ID and the data itself
                var packetId = packetIdDataPair.Key;
                var packetData = packetIdDataPair.Value;

                // Check whether for this ID there already exists data
                if (cachedData.TryGetValue(packetId, out var existingPacketData)) {
                    // If the existing data is a PacketDataCollection, we can simply add all the data instance to it
                    // If not, we simply discard the resent data, since it is older
                    if (existingPacketData is RawPacketDataCollection existingPacketDataCollection
                        && packetData is RawPacketDataCollection packetDataCollection) {
                        existingPacketDataCollection.DataInstances.AddRange(packetDataCollection.DataInstances);
                    }
                } else {
                    // If no data exists for this ID, we can simply set the resent data for that key
                    cachedData[packetId] = packetData;
                }
            }
        }

        // Iteratively add the resent packet data, but make sure to merge it with existing data
        foreach (var resentPacketData in _resendPacketData.Values) {
            AddResendData(resentPacketData, _cachedAllPacketData);
        }

        // Do the same as above but for addon data
        _cachedAllAddonData = new Dictionary<byte, AddonPacketData>();

        // Iteratively add the addon data
        foreach (var addonIdDataPair in _addonPacketData) {
            _cachedAllAddonData.Add(addonIdDataPair.Key, addonIdDataPair.Value);
        }

        // Iteratively add the resent addon data, but make sure to merge it with existing data
        foreach (var resentAddonData in _resendAddonPacketData.Values) {
            foreach (var addonIdDataPair in resentAddonData) {
                var addonId = addonIdDataPair.Key;
                var addonPacketData = addonIdDataPair.Value;

                if (_cachedAllAddonData.TryGetValue(addonId, out var existingAddonPacketData)) {
                    AddResendData(addonPacketData.PacketData, existingAddonPacketData.PacketData);
                } else {
                    _cachedAllAddonData[addonId] = addonPacketData;
                }
            }
        }

        _isAllPacketDataCached = true;
    }

    /// <summary>
    /// Drops resend data that is duplicate, i.e. that we already received in an earlier packet.
    /// </summary>
    /// <param name="receivedSequenceNumbers">A queue containing sequence numbers that were already
    /// received.</param>
    public void DropDuplicateResendData(Queue<ushort> receivedSequenceNumbers) {
        // For each key in the resend dictionary, we check whether it is contained in the
        // queue of sequence numbers that we already received. If so, we remove it from the dictionary
        // because it is duplicate data that we already handled
        foreach (var resendSequence in new List<ushort>(_resendPacketData.Keys)) {
            if (receivedSequenceNumbers.Contains(resendSequence)) {
                // Logger.Info("Dropping resent data due to duplication");
                _resendPacketData.Remove(resendSequence);
            }
        }

        // Do the same for addon data
        foreach (var resendSequence in new List<ushort>(_resendAddonPacketData.Keys)) {
            if (receivedSequenceNumbers.Contains(resendSequence)) {
                // Logger.Info("Dropping resent data due to duplication");
                _resendAddonPacketData.Remove(resendSequence);
            }
        }
    }

    /// <summary>
    /// Get an instantiation of IPacketData for the given packet ID.
    /// </summary>
    /// <param name="packetId">The packet ID to get an instance for.</param>
    /// <returns>A new instance of IPacketData.</returns>
    protected abstract IPacketData InstantiatePacketDataFromId(T packetId);
}

/// <summary>
/// Specialization of the update packet for client to server communication.
/// </summary>
internal class ServerUpdatePacket : UpdatePacket<ServerPacketId> {
    // This constructor is not unused, as it is a constraint for a generic parameter in the UdpUpdateManager.
    // ReSharper disable once UnusedMember.Global
    public ServerUpdatePacket() : this(null) {
    }

    public ServerUpdatePacket(Packet packet) : base(packet) {
    }

    /// <inheritdoc />
    protected override IPacketData InstantiatePacketDataFromId(ServerPacketId packetId) {
        switch (packetId) {
            case ServerPacketId.LoginRequest:
                return new LoginRequest();
            case ServerPacketId.HelloServer:
                return new HelloServer();
            case ServerPacketId.PlayerUpdate:
                return new PlayerUpdate();
            case ServerPacketId.PlayerMapUpdate:
                return new PlayerMapUpdate();
            case ServerPacketId.EntityUpdate:
                return new PacketDataCollection<EntityUpdate>();
            case ServerPacketId.PlayerEnterScene:
                return new ServerPlayerEnterScene();
            case ServerPacketId.PlayerTeamUpdate:
                return new ServerPlayerTeamUpdate();
            case ServerPacketId.PlayerSkinUpdate:
                return new ServerPlayerSkinUpdate();
            case ServerPacketId.ChatMessage:
                return new ChatMessage();
            default:
                return new EmptyData();
        }
    }
}

/// <summary>
/// Specialization of the update packet for server to client communication.
/// </summary>
internal class ClientUpdatePacket : UpdatePacket<ClientPacketId> {
    public ClientUpdatePacket() : this(null) {
    }

    public ClientUpdatePacket(Packet packet) : base(packet) {
    }

    /// <inheritdoc />
    protected override IPacketData InstantiatePacketDataFromId(ClientPacketId packetId) {
        switch (packetId) {
            case ClientPacketId.LoginResponse:
                return new LoginResponse();
            case ClientPacketId.HelloClient:
                return new HelloClient();
            case ClientPacketId.ServerClientDisconnect:
                return new ServerClientDisconnect();
            case ClientPacketId.PlayerConnect:
                return new PacketDataCollection<PlayerConnect>();
            case ClientPacketId.PlayerDisconnect:
                return new PacketDataCollection<ClientPlayerDisconnect>();
            case ClientPacketId.PlayerEnterScene:
                return new PacketDataCollection<ClientPlayerEnterScene>();
            case ClientPacketId.PlayerAlreadyInScene:
                return new ClientPlayerAlreadyInScene();
            case ClientPacketId.PlayerLeaveScene:
                return new PacketDataCollection<GenericClientData>();
            case ClientPacketId.PlayerUpdate:
                return new PacketDataCollection<PlayerUpdate>();
            case ClientPacketId.PlayerMapUpdate:
                return new PacketDataCollection<PlayerMapUpdate>();
            case ClientPacketId.EntityUpdate:
                return new PacketDataCollection<EntityUpdate>();
            case ClientPacketId.PlayerDeath:
                return new PacketDataCollection<GenericClientData>();
            case ClientPacketId.PlayerTeamUpdate:
                return new PacketDataCollection<ClientPlayerTeamUpdate>();
            case ClientPacketId.PlayerSkinUpdate:
                return new PacketDataCollection<ClientPlayerSkinUpdate>();
            case ClientPacketId.ServerSettingsUpdated:
                return new ServerSettingsUpdate();
            case ClientPacketId.ChatMessage:
                return new PacketDataCollection<ChatMessage>();
            default:
                return new EmptyData();
        }
    }
}
