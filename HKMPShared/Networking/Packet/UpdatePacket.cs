using System;
using System.Collections.Generic;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Packet {
    public abstract class UpdatePacket {
        protected readonly Packet Packet;

        public ushort Sequence { get; set; }

        public ushort Ack { get; set; }

        public bool[] AckField { get; private set; }

        protected UpdatePacket(Packet packet) {
            Packet = packet;

            AckField = new bool[UdpUpdateManager.AckSize];
        }

        protected void WriteHeaders(Packet packet) {
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

        protected void ReadHeaders(Packet packet) {
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

        public abstract Packet CreatePacket();

        public abstract void ReadPacket();

        public abstract bool ContainsReliableData();
    }

    public class ServerUpdatePacket : UpdatePacket {
        private bool _containsReliableData;

        public Dictionary<ServerPacketId, IPacketData> PacketData { get; }

        public ServerUpdatePacket() : this(null) {
        }

        public ServerUpdatePacket(Packet packet) : base(packet) {
            PacketData = new Dictionary<ServerPacketId, IPacketData>();
        }

        public override Packet CreatePacket() {
            var packet = new Packet();

            WriteHeaders(packet);

            // Construct the byte flag representing which packets are included
            // in this update
            ushort dataPacketIdFlag = 0;
            // Keep track of value of current bit
            ushort currentTypeValue = 1;

            for (var i = 0; i < Enum.GetNames(typeof(ServerPacketId)).Length; i++) {
                // Cast the current index of the loop to a ServerPacketId and check if it is
                // contained in the update type list, if so, we add the current bit to the flag
                if (PacketData.ContainsKey((ServerPacketId) i)) {
                    dataPacketIdFlag |= currentTypeValue;
                }

                currentTypeValue *= 2;
            }

            packet.Write(dataPacketIdFlag);

            _containsReliableData = false;

            for (var i = 0; i < Enum.GetNames(typeof(ServerPacketId)).Length; i++) {
                if (PacketData.TryGetValue((ServerPacketId) i, out var packetData)) {
                    packetData.WriteData(packet);

                    if (packetData.IsReliable) {
                        _containsReliableData = true;
                    }
                }
            }

            packet.WriteLength();

            return packet;
        }

        public override void ReadPacket() {
            ReadHeaders(Packet);

            // Read the byte flag representing which packets
            // are included in this update
            var dataPacketIdFlag = Packet.ReadUShort();
            // Keep track of value of current bit
            var currentTypeValue = 1;

            for (var i = 0; i < Enum.GetNames(typeof(ServerPacketId)).Length; i++) {
                // If this bit was set in our flag, we add the type to the list
                if ((dataPacketIdFlag & currentTypeValue) != 0) {
                    var serverPacketId = (ServerPacketId) i;
                    var packetData = InstantiatePacketDataFromId(serverPacketId);
                    packetData?.ReadData(Packet);

                    PacketData[serverPacketId] = packetData;
                }

                // Increase the value of current bit
                currentTypeValue *= 2;
            }
        }

        public override bool ContainsReliableData() {
            return _containsReliableData;
        }

        public void SetLostReliableData(ServerUpdatePacket lostPacket) {
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
                }

                if (PacketData.ContainsKey(packetId) && packetData.DropReliableDataIfNewerExists) {
                    continue;
                }
                
                Logger.Get().Info(this, $"  Resending {packetData.GetType()} data");

                PacketData[packetId] = packetData;
            }
        }

        private IPacketData InstantiatePacketDataFromId(ServerPacketId packetId) {
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

    public class ClientUpdatePacket : UpdatePacket {
        private bool _containsReliableData;

        public Dictionary<ClientPacketId, IPacketData> PacketData { get; }

        public ClientUpdatePacket() : this(null) {
        }

        public ClientUpdatePacket(Packet packet) : base(packet) {
            PacketData = new Dictionary<ClientPacketId, IPacketData>();
        }

        public override Packet CreatePacket() {
            var packet = new Packet();

            WriteHeaders(packet);

            // Construct the ushort flag representing which packets are included
            // in this update, we need a ushort since we have more than 8 possible packet IDs
            ushort dataPacketIdFlag = 0;
            // Keep track of value of current bit
            ushort currentTypeValue = 1;

            for (var i = 0; i < Enum.GetNames(typeof(ClientPacketId)).Length; i++) {
                // Cast the current index of the loop to a ClientPacketId and check if it is
                // contained in the update type list, if so, we add the current bit to the flag
                if (PacketData.ContainsKey((ClientPacketId) i)) {
                    dataPacketIdFlag |= currentTypeValue;
                }

                currentTypeValue *= 2;
            }

            packet.Write(dataPacketIdFlag);

            _containsReliableData = false;

            for (var i = 0; i < Enum.GetNames(typeof(ClientPacketId)).Length; i++) {
                if (PacketData.TryGetValue((ClientPacketId) i, out var packetData)) {
                    packetData.WriteData(packet);

                    if (packetData.IsReliable) {
                        _containsReliableData = true;
                    }
                }
            }
            
            packet.WriteLength();

            return packet;
        }

        public override void ReadPacket() {
            ReadHeaders(Packet);

            // Read the byte flag representing which packets
            // are included in this update
            var dataPacketIdFlag = Packet.ReadUShort();
            // Keep track of value of current bit
            var currentTypeValue = 1;

            for (var i = 0; i < Enum.GetNames(typeof(ClientPacketId)).Length; i++) {
                // If this bit was set in our flag, we add the type to the list
                if ((dataPacketIdFlag & currentTypeValue) != 0) {
                    var clientPacketId = (ClientPacketId) i;
                    var packetData = InstantiatePacketDataFromId(clientPacketId);
                    packetData?.ReadData(Packet);

                    PacketData[clientPacketId] = packetData;
                }

                // Increase the value of current bit
                currentTypeValue *= 2;
            }
        }

        public override bool ContainsReliableData() {
            return _containsReliableData;
        }

        public void SetLostReliableData(ClientUpdatePacket lostPacket) {
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
                }

                if (PacketData.ContainsKey(packetId) && packetData.DropReliableDataIfNewerExists) {
                    continue;
                }
                
                Logger.Get().Info(this, $"  Resending {packetData.GetType()} data");

                PacketData[packetId] = packetData;
            }
        }
        
        private IPacketData InstantiatePacketDataFromId(ClientPacketId packetId) {
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