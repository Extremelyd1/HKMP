using System;
using System.Collections.Generic;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Packet {
    public abstract class UpdatePacket<T> where T : Enum {
        // A dictionary containing addon packet info per addon ID in order to read and convert raw
        // addon packet data into IPacketData instances
        // ReSharper disable once StaticMemberInGenericType
        public static Dictionary<byte, AddonPacketInfo> AddonPacketInfoDict { get; } =
            new Dictionary<byte, AddonPacketInfo>();

        // The underlying raw packet instance, only used for reading data out of
        private readonly Packet _packet;

        // The sequence number of this packet
        public ushort Sequence { get; set; }

        // The acknowledgement number of this packet
        public ushort Ack { get; set; }

        // An array containing booleans that indicate whether sequence number (Ack - x) is also
        // acknowledged for the x-th value in the array
        public bool[] AckField { get; private set; }

        // Normal non-resend packet data
        private readonly Dictionary<T, IPacketData> _normalPacketData;

        // Resend packet data indexed by sequence number it originates from
        private readonly Dictionary<ushort, Dictionary<T, IPacketData>> _resendPacketData;

        // Packet data from addons indexed by their ID
        private readonly Dictionary<byte, AddonPacketData> _addonPacketData;
        // TODO: include resend data for addons, perhaps another dictionary, or something more elegant?

        // The combination of normal and resent packet data cached in case it needs to be queried multiple times
        private Dictionary<T, IPacketData> _cachedAllPacketData;

        // Whether the dictionary containing all packet data is cached already or needs to be calculated first
        private bool _isAllPacketDataCached;

        // Whether this packet contains data that needs to be reliable
        private bool _containsReliableData;

        protected UpdatePacket(Packet packet) {
            _packet = packet;

            AckField = new bool[UdpUpdateManager.AckSize];

            _normalPacketData = new Dictionary<T, IPacketData>();
            _resendPacketData = new Dictionary<ushort, Dictionary<T, IPacketData>>();
            _addonPacketData = new Dictionary<byte, AddonPacketData>();
        }

        /**
         * Write header info into the given packet (sequence number, acknowledgement number and ack field).
         */
        private void WriteHeaders(Packet packet) {
            packet.Write(Sequence);
            packet.Write(Ack);

            uint ackFieldInt = 0;
            uint currentFieldValue = 1;
            for (var i = 0; i < UdpUpdateManager.AckSize; i++) {
                if (AckField[i]) {
                    ackFieldInt |= currentFieldValue;
                }

                currentFieldValue *= 2;
            }

            packet.Write(ackFieldInt);
        }

        /**
         * Read header info from the given packet (sequence number, acknowledgement number and ack field).
         */
        private void ReadHeaders(Packet packet) {
            Sequence = packet.ReadUShort();
            Ack = packet.ReadUShort();

            // Initialize the AckField array
            AckField = new bool[UdpUpdateManager.AckSize];

            var ackFieldInt = packet.ReadUInt();
            uint currentFieldValue = 1;
            for (var i = 0; i < UdpUpdateManager.AckSize; i++) {
                AckField[i] = (ackFieldInt & currentFieldValue) != 0;

                currentFieldValue *= 2;
            }
        }

        /**
         * Write the given dictionary of normal or resent packet data into the given raw packet instance.
         */
        private bool WritePacketData(
            Packet packet,
            Dictionary<T, IPacketData> packetData
        ) {
            var enumValues = Enum.GetValues(typeof(T));

            return WritePacketData(
                packet,
                packetData,
                (IEnumerator<T>)enumValues.GetEnumerator(),
                (byte)enumValues.Length
            );
        }

        private bool WriteAddonPacketData(
            Packet packet,
            AddonPacketData addonPacketData
        ) => WritePacketData(
            packet,
            addonPacketData.PacketData,
            addonPacketData.PacketIdEnumerator,
            addonPacketData.PacketIdSize
        );

        /**
         * Write the given dictionary of packet data into the given raw packet instance.
         * The given enumerator should enumerate over all possible keys in the dictionary,
         * and the keySpaceSize parameter should indicate the exact size of the key space.
         */
        private bool WritePacketData<TKey>(
            Packet packet,
            Dictionary<TKey, IPacketData> packetData,
            IEnumerator<TKey> keyEnumerator,
            byte keySpaceSize
        ) {
            // Keep track of the bit flag in an unsigned long, which is the largest integer implicit type allowed
            ulong idFlag = 0;
            // Also keep track of the value of the current bit in an unsigned long
            ulong currentTypeValue = 0;

            while (keyEnumerator.MoveNext()) {
                var key = keyEnumerator.Current;

                // Update the bit in the flag if the current value is included in the dictionary
                if (packetData.ContainsKey(key)) {
                    idFlag |= currentTypeValue;
                }

                // Always increase the current bit
                currentTypeValue *= 2;
            }

            // Based on the size of the values space, we cast to the smallest primitive that can hold the flag
            // and write it to the packet
            if (keySpaceSize <= 8) {
                packet.Write((byte)idFlag);
            } else if (keySpaceSize <= 16) {
                packet.Write((ushort)idFlag);
            } else if (keySpaceSize <= 32) {
                packet.Write((uint)idFlag);
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

                if (packetData.TryGetValue(key, out var iPacketData)) {
                    iPacketData.WriteData(packet);

                    if (iPacketData.IsReliable) {
                        containsReliableData = true;
                    }
                }
            }

            return containsReliableData;
        }

        /**
         * Read the given dictionary of packet data into the given raw packet instance.
         * This method is only for normal and resent packet data, not for addon packet data.
         */
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
                    var iPacketData = packetDataInstantiator.Invoke(packetId);
                    if (iPacketData == null) {
                        throw new Exception("Addon packet data instantiating method returned null");
                    }

                    iPacketData.ReadData(_packet);

                    packetData[packetId] = iPacketData;
                }

                // Increase the value of current bit
                currentTypeValue *= 2;
            }
        }

        /**
         * Create a raw packet out of the data contained in this class.
         */
        public Packet CreatePacket() {
            var packet = new Packet();

            WriteHeaders(packet);

            // Write the normal packet data into the packet and keep track of whether this packet
            // contains reliable data now
            _containsReliableData = WritePacketData(packet, _normalPacketData);

            // Put the length of the resend data as a ushort in the packet
            var resendLength = (ushort)_resendPacketData.Count;
            if (_resendPacketData.Count > ushort.MaxValue) {
                resendLength = ushort.MaxValue;

                Logger.Get().Warn(this, "Length of resend packet data dictionary does not fit in ushort");
            }

            packet.Write(resendLength);

            // Add each entry of lost data to resend to the packet
            foreach (var seqPacketDataPair in _resendPacketData) {
                var seq = seqPacketDataPair.Key;
                var packetData = seqPacketDataPair.Value;

                // Make sure to not put more resend data in the packet than we specify
                if (resendLength-- == 0) {
                    break;
                }

                // First write the sequence number it belongs to
                packet.Write(seq);

                // Then write the reliable packet data and note that this packet now contains reliable data
                WritePacketData(packet, packetData);
                _containsReliableData = true;
            }

            // Put the length of the addon packet data as a byte in the packet
            // There should only be a maximum of 255 addons, so the length should fit in a byte
            packet.Write((byte)_addonPacketData.Count);

            // Add the packet data per addon ID
            foreach (var addonPacketDataPair in _addonPacketData) {
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
                    Logger.Get().Debug(this,
                        $"Addon with ID {addonId} has thrown an exception while writing addon packet data, type: {e.GetType()}, message: {e.Message}");
                    continue;
                }

                // Prepend the length of the addon packet data to the addon packet
                addonPacket.WriteLength();

                // Now we add the addon ID to the regular packet and then the contents of the addon packet
                packet.Write(addonId);
                packet.Write(addonPacket.ToArray());

                // Finally potentially update whether this packet contains reliable data now
                _containsReliableData |= addonContainsReliable;
            }

            packet.WriteLength();

            return packet;
        }

        /**
         * Read the raw packets contents into easy to access dictionaries. Returns false if the packet
         * cannot be successfully read due to malformed data, true otherwise.
         */
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

                // Read the number of the addon packet data instances from the packet
                var numAddonData = _packet.ReadByte();

                while (numAddonData-- > 0) {
                    var addonId = _packet.ReadByte();
                    
                    if (!AddonPacketInfoDict.TryGetValue(addonId, out var addonPacketInfo)) {
                        // If the addon packet info for this addon could not be found, we need to throw an error
                        throw new Exception($"Addon with ID {addonId} has no defined addon packet info");
                    }

                    // Read the length of the addon packet data for this addon
                    var addonDataLength = _packet.ReadUShort();

                    // Read exactly as many bytes as was indicated by the previously read value
                    var addonDataBytes = _packet.ReadBytes(addonDataLength);

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
                        Logger.Get().Debug(this,
                            $"Addon with ID {addonId} has thrown an exception while reading addon packet data, type: {e.GetType()}, message: {e.Message}");
                        continue;
                    }

                    _addonPacketData[addonId] = addonPacketData;
                }
            } catch {
                return false;
            }

            return true;
        }

        /**
         * Whether this packet contains data that needs to be reliable.
         */
        public bool ContainsReliableData() {
            return _containsReliableData;
        }

        /**
         * Set the reliable packet data contained in the lost packet as resend data in this one.
         */
        public void SetLostReliableData(UpdatePacket<T> lostPacket) {
            // Retrieve the lost packet data
            var lostPacketData = lostPacket.GetPacketData();
            // Create a new dictionary of packet data in which we store all reliable data from the lost packet
            var toResendPacketData = new Dictionary<T, IPacketData>();

            foreach (var idLostDataPair in lostPacketData) {
                var packetId = idLostDataPair.Key;
                var packetData = idLostDataPair.Value;

                // Check if the packet data is supposed to be reliable
                if (!packetData.IsReliable) {
                    continue;
                }

                // Check whether we can drop it since a newer version of that data already exists
                if (packetData.DropReliableDataIfNewerExists && _normalPacketData.ContainsKey(packetId)) {
                    continue;
                }

                Logger.Get().Info(this, $"  Resending {packetData.GetType()} data");
                toResendPacketData[packetId] = packetData;
            }

            // Finally, put the packet data dictionary in the resent dictionary keyed by its sequence number
            _resendPacketData[lostPacket.Sequence] = toResendPacketData;
        }

        /**
         * Tries to get packet data that is going to be sent with the given packet ID.
         * Returns true if the packet data exists and will be stored in the packetData variable, false otherwise.
         */
        public bool TryGetSendingPacketData(T packetId, out IPacketData packetData) {
            return _normalPacketData.TryGetValue(packetId, out packetData);
        }

        /**
         * Tries to get addon packet data for the addon with the given ID.
         * Returns true if the addon packet data exists and will be stored in the addonPacketData variable, false
         * otherwise.
         */
        public bool TryGetSendingAddonPacketData(byte addonId, out AddonPacketData addonPacketData) {
            return _addonPacketData.TryGetValue(addonId, out addonPacketData);
        }

        /**
         * Sets the given packetData with the given packet ID for sending.
         */
        public void SetSendingPacketData(T packetId, IPacketData packetData) {
            _normalPacketData[packetId] = packetData;
        }

        /**
         * Sets the given addonPacketData with the given addon ID for sending.
         */
        public void SetSendingAddonPacketData(byte addonId, AddonPacketData packetData) {
            _addonPacketData[addonId] = packetData;
        }

        /**
         * Get all the packet data contained in this packet, normal and resent data (but not addon data).
         */
        public Dictionary<T, IPacketData> GetPacketData() {
            if (!_isAllPacketDataCached) {
                CacheAllPacketData();
            }

            return _cachedAllPacketData;
        }

        /**
         * Get the addon packet data in this packet.
         */
        public Dictionary<byte, AddonPacketData> GetAddonPacketData() {
            return _addonPacketData;
        }

        /**
         * Computes all packet data (normal and resent data), caches it and sets a boolean indicating
         * that this cache is now available.
         */
        private void CacheAllPacketData() {
            // Construct a new dictionary for all the data
            _cachedAllPacketData = new Dictionary<T, IPacketData>();

            // Iteratively add the normal packet data
            foreach (var packetIdDataPair in _normalPacketData) {
                _cachedAllPacketData.Add(packetIdDataPair.Key, packetIdDataPair.Value);
            }

            // Iteratively add the resent packet data, but make sure to merge it with existing data
            foreach (var resentPacketData in _resendPacketData.Values) {
                foreach (var packetIdDataPair in resentPacketData) {
                    // Get the ID and the data itself
                    var packetId = packetIdDataPair.Key;
                    var packetData = packetIdDataPair.Value;

                    // Check whether for this ID there already exists data
                    if (_cachedAllPacketData.TryGetValue(packetId, out var existingPacketData)) {
                        // If the existing data is a PacketDataCollection, we can simply add all the data instance to it
                        // If not, we simply discard the resent data, since it is older
                        if (existingPacketData is RawPacketDataCollection existingPacketDataCollection
                            && packetData is RawPacketDataCollection packetDataCollection) {
                            existingPacketDataCollection.DataInstances.AddRange(packetDataCollection.DataInstances);
                        }
                    } else {
                        // If no data exists for this ID, we can simply set the resent data for that key
                        _cachedAllPacketData[packetId] = packetData;
                    }
                }
            }

            _isAllPacketDataCached = true;
        }

        /**
         * Drops resend data that is duplicate, i.e. that we already received in an earlier packet.
         */
        public void DropDuplicateResendData(Queue<ushort> receivedSequenceNumbers) {
            // For each key in the resend dictionary, we check whether it is contained in the
            // queue of sequence numbers that we already received. If so, we remove it from the dictionary
            // because it is duplicate data that we already handled
            foreach (var resendSequence in new List<ushort>(_resendPacketData.Keys)) {
                if (receivedSequenceNumbers.Contains(resendSequence)) {
                    // TODO: remove this output
                    Logger.Get().Info(this, "Dropping resent data due to duplication");
                    _resendPacketData.Remove(resendSequence);
                }
            }
        }

        /**
         * Get an instantiation of IPacketData for the given packet ID.
         */
        protected abstract IPacketData InstantiatePacketDataFromId(T packetId);
    }

    public class ServerUpdatePacket : UpdatePacket<ServerPacketId> {
        // This constructor is not unused, as it is a constraint for a generic parameter in the UdpUpdateManager.
        // ReSharper disable once UnusedMember.Global
        public ServerUpdatePacket() : this(null) {
        }

        public ServerUpdatePacket(Packet packet) : base(packet) {
        }

        protected override IPacketData InstantiatePacketDataFromId(ServerPacketId packetId) {
            switch (packetId) {
                case ServerPacketId.LoginRequest:
                    return new LoginRequest();
                case ServerPacketId.HelloServer:
                    return new HelloServer();
                case ServerPacketId.PlayerUpdate:
                    return new PlayerUpdate();
                case ServerPacketId.EntityUpdate:
                    return new PacketDataCollection<EntityUpdate>();
                case ServerPacketId.PlayerEnterScene:
                    return new ServerPlayerEnterScene();
                case ServerPacketId.PlayerTeamUpdate:
                    return new ServerPlayerTeamUpdate();
                case ServerPacketId.PlayerSkinUpdate:
                    return new ServerPlayerSkinUpdate();
                default:
                    return new EmptyData();
            }
        }
    }

    public class ClientUpdatePacket : UpdatePacket<ClientPacketId> {
        public ClientUpdatePacket() : this(null) {
        }

        public ClientUpdatePacket(Packet packet) : base(packet) {
        }

        protected override IPacketData InstantiatePacketDataFromId(ClientPacketId packetId) {
            switch (packetId) {
                case ClientPacketId.LoginResponse:
                    return new LoginResponse();
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
                case ClientPacketId.EntityUpdate:
                    return new PacketDataCollection<EntityUpdate>();
                case ClientPacketId.PlayerDeath:
                    return new PacketDataCollection<GenericClientData>();
                case ClientPacketId.PlayerTeamUpdate:
                    return new PacketDataCollection<ClientPlayerTeamUpdate>();
                case ClientPacketId.PlayerSkinUpdate:
                    return new PacketDataCollection<ClientPlayerSkinUpdate>();
                case ClientPacketId.GameSettingsUpdated:
                    return new GameSettingsUpdate();
                default:
                    return new EmptyData();
            }
        }
    }
}