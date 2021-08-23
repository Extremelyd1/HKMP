using System;
using System.Collections.Generic;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Packet {
    public abstract class UpdatePacket<T> where T : Enum {
        // The underlying raw packet instance, only used for reading data out of.
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
         * Write the given dictionary of packet data into the given raw packet instance.
         */
        private bool WritePacketData(Packet packet, Dictionary<T, IPacketData> packetData) {
            // Construct the bit flag representing which packets are included
            // in this update
            ushort dataPacketIdFlag = 0;
            // Keep track of value of current bit
            ushort currentTypeValue = 1;

            var packetIdValues = Enum.GetValues(typeof(T));
            // Loop over all packet IDs and check if it is contained in the update type list,
            // if so, we add the current bit to the flag
            foreach (T packetId in packetIdValues) {
                if (packetData.ContainsKey(packetId)) {
                    dataPacketIdFlag |= currentTypeValue;
                }

                currentTypeValue *= 2;
            }

            // Now we write the bit flag
            packet.Write(dataPacketIdFlag);
            
            // Let each individual piece of packet data write themselves into the packet
            // and keep track of whether any of them need to be reliable
            var containsReliableData = false;
            foreach (T packetId in packetIdValues) {
                if (packetData.TryGetValue(packetId, out var iPacketData)) {
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
         */
        private void ReadPacketData(Packet packet, Dictionary<T, IPacketData> packetData) {
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
        
        /**
         * Create a raw packet out of the data contained in this class.
         */
        public Packet CreatePacket() {
            var packet = new Packet();

            WriteHeaders(packet);

            // Write the normal packet data into the packet and keep track of whether this packet
            // contains reliable data now
            _containsReliableData = WritePacketData(packet, _normalPacketData);

            // Add each entry of lost data to resend to the packet
            foreach (var seqPacketDataPair in _resendPacketData) {
                var seq = seqPacketDataPair.Key;
                var packetData = seqPacketDataPair.Value;

                // First write the sequence number it belongs to
                packet.Write(seq);

                // Then write the reliable packet data and note that this packet now contains reliable data
                WritePacketData(packet, packetData);
                _containsReliableData = true;
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

                // Check whether there is more data to be read
                // If so, this is a resend of lost data
                while (_packet.HasDataLeft()) {
                    // Read the sequence number of the packet it was lost from
                    var seq = _packet.ReadUShort();

                    // Create a new dictionary for the packet data and read the data from the packet into it
                    var packetData = new Dictionary<T, IPacketData>();
                    ReadPacketData(_packet, packetData);

                    // Input the data into the resend dictionary keyed by its sequence number
                    _resendPacketData[seq] = packetData;
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
         * Sets the given packetData with the given packet ID for sending.
         */
        public void SetSendingPacketData(T packetId, IPacketData packetData) {
            _normalPacketData[packetId] = packetData;
        }

        /**
         * Get all the packet data contained in this packet, normal and resent data.
         */
        public Dictionary<T, IPacketData> GetPacketData() {
            if (!_isAllPacketDataCached) {
                CacheAllPacketData();
            }

            return _cachedAllPacketData;
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
                case ClientPacketId.AlreadyInScene:
                    return new ClientAlreadyInScene();
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