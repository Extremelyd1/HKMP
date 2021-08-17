using System;
using System.Collections.Generic;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Packet {
    public abstract class UpdatePacket<T> where T : Enum {
        private readonly Packet _packet;

        public ushort Sequence { get; set; }

        public ushort Ack { get; set; }

        public bool[] AckField { get; private set; }
        
        public Dictionary<T, IPacketData> PacketData { get; }

        private bool _containsReliableData;

        protected UpdatePacket(Packet packet) {
            _packet = packet;

            AckField = new bool[UdpUpdateManager.AckSize];

            PacketData = new Dictionary<T, IPacketData>();
        }

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
        
        public Packet CreatePacket() {
            var packet = new Packet();

            WriteHeaders(packet);

            // Construct the byte flag representing which packets are included
            // in this update
            ushort dataPacketIdFlag = 0;
            // Keep track of value of current bit
            ushort currentTypeValue = 1;

            var packetIdValues = Enum.GetValues(typeof(T));
            // Loop over all packet IDs and check if it is contained in the update type list,
            // if so, we add the current bit to the flag
            foreach (T packetId in packetIdValues) {
                if (PacketData.ContainsKey(packetId)) {
                    dataPacketIdFlag |= currentTypeValue;
                }

                currentTypeValue *= 2;
            }

            packet.Write(dataPacketIdFlag);

            _containsReliableData = false;

            foreach (T packetId in packetIdValues) {
                if (PacketData.TryGetValue(packetId, out var packetData)) {
                    packetData.WriteData(packet);

                    if (packetData.IsReliable) {
                        _containsReliableData = true;
                    }
                }
            }

            packet.WriteLength();

            return packet;
        }
        
        public void ReadPacket() {
            ReadHeaders(_packet);

            // Read the byte flag representing which packets
            // are included in this update
            var dataPacketIdFlag = _packet.ReadUShort();
            // Keep track of value of current bit
            var currentTypeValue = 1;

            var packetIdValues = Enum.GetValues(typeof(T));
            foreach (T packetId in packetIdValues) {
                // If this bit was set in our flag, we add the type to the list
                if ((dataPacketIdFlag & currentTypeValue) != 0) {
                    var packetData = InstantiatePacketDataFromId(packetId);
                    packetData?.ReadData(_packet);

                    PacketData[packetId] = packetData;
                }

                // Increase the value of current bit
                currentTypeValue *= 2;
            }
        }

        public bool ContainsReliableData() {
            return _containsReliableData;
        }
        
        public void SetLostReliableData(UpdatePacket<T> lostPacket) {
            foreach (var idLostDataPair in lostPacket.PacketData) {
                var packetId = idLostDataPair.Key;
                var packetData = idLostDataPair.Value;

                // Check whether the lost data is a data collection
                // We need to check an intermediate class here that has no parameterization, otherwise
                // this wouldn't be possible
                if (packetData is RawPacketDataCollection packetDataCollection) {
                    Logger.Get().Info(this, $"  Resending {packetData.GetType()} data");

                    // If the new packet already contains a data collection
                    if (PacketData.ContainsKey(packetId)) {
                        var existingPacketData = (RawPacketDataCollection) PacketData[packetId];

                        // With the intermediate raw packet data collection class, we can access the data instances
                        // and add them to the existing ones
                        existingPacketData.DataInstances.AddRange(packetDataCollection.DataInstances);
                    } else {
                        // Otherwise, we simply set the packet data at the given key
                        PacketData[packetId] = packetData;
                    }

                    // We continue here since the rest of the flow is only for non-packetDataCollection instances
                    continue;
                }

                if (PacketData.ContainsKey(packetId) && packetData.DropReliableDataIfNewerExists) {
                    continue;
                }
                
                Logger.Get().Info(this, $"  Resending {packetData.GetType()} data");

                PacketData[packetId] = packetData;
            }
        }

        protected abstract IPacketData InstantiatePacketDataFromId(T packetId);
    }

    public class ServerUpdatePacket : UpdatePacket<ServerPacketId> {
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