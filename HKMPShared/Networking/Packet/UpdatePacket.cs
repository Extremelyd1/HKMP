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

        public HashSet<ServerPacketId> DataPacketIds { get; }

        public HelloServer HelloServer { get; private set; }

        public PlayerUpdate PlayerUpdate { get; }

        public PacketDataCollection<EntityUpdate> EntityUpdates { get; }

        public ServerPlayerEnterScene PlayerEnterScene { get; private set; }

        public ServerPlayerTeamUpdate PlayerTeamUpdate { get; private set; }

        public ServerPlayerSkinUpdate PlayerSkinUpdate { get; private set; }

        public ServerPlayerEmoteUpdate PlayerEmoteUpdate { get; private set; }

        public ServerUpdatePacket() : this(null) {
        }

        public ServerUpdatePacket(Packet packet) : base(packet) {
            DataPacketIds = new HashSet<ServerPacketId>();

            HelloServer = new HelloServer();

            PlayerUpdate = new PlayerUpdate();
            EntityUpdates = new PacketDataCollection<EntityUpdate>();

            PlayerEnterScene = new ServerPlayerEnterScene();
            PlayerTeamUpdate = new ServerPlayerTeamUpdate();
            PlayerSkinUpdate = new ServerPlayerSkinUpdate();
            PlayerEmoteUpdate = new ServerPlayerEmoteUpdate();
        }

        public override Packet CreatePacket() {
            var packet = new Packet();

            WriteHeaders(packet);

            // Construct the byte flag representing which packets are included
            // in this update
            ushort dataPacketIdFlag = 0;
            // Keep track of value of current bit
            ushort currentTypeValue = 1;

            /*
            foreach (var item in DataPacketIds)
            {
                Logger.Info(this,$"creating packet with {Enum.GetName(typeof(ServerPacketId), item)}");
            }
            */

            for (var i = 0; i < Enum.GetNames(typeof(ServerPacketId)).Length; i++) {
                // Cast the current index of the loop to a ServerPacketId and check if it is
                // contained in the update type list, if so, we add the current bit to the flag
                if (DataPacketIds.Contains((ServerPacketId) i)) {
                    dataPacketIdFlag |= currentTypeValue;
                }

                currentTypeValue *= 2;
            }

            packet.Write(dataPacketIdFlag);

            // TODO: this is a mess, we have an interface that exposes a write and read method
            // to packets, but we don't really use the abstraction since we still need to
            // write and read in a specific order to ensure consistency
            // The same holds then for determining whether this packet contains reliable data
            // and finding a way to elegantly copy reliable data to a new packet

            if (DataPacketIds.Contains(ServerPacketId.HelloServer)) {
                HelloServer.WriteData(packet);
            }

            if (DataPacketIds.Contains(ServerPacketId.PlayerUpdate)) {
                PlayerUpdate.WriteData(packet);
            }

            if (DataPacketIds.Contains(ServerPacketId.EntityUpdate)) {
                EntityUpdates.WriteData(packet);
            }

            if (DataPacketIds.Contains(ServerPacketId.PlayerEnterScene)) {
                PlayerEnterScene.WriteData(packet);
            }

            if (DataPacketIds.Contains(ServerPacketId.PlayerTeamUpdate)) {
                PlayerTeamUpdate.WriteData(packet);
            }

            if (DataPacketIds.Contains(ServerPacketId.PlayerSkinUpdate)) {
                PlayerSkinUpdate.WriteData(packet);
            }

            if (DataPacketIds.Contains(ServerPacketId.PlayerEmoteUpdate)) {
                PlayerEmoteUpdate.WriteData(packet);
            }

            // Check whether there is reliable data written in this packet
            // and set the boolean value accordingly
            _containsReliableData = DataPacketIds.Contains(ServerPacketId.HelloServer)
                                    || DataPacketIds.Contains(ServerPacketId.PlayerEnterScene)
                                    || DataPacketIds.Contains(ServerPacketId.PlayerLeaveScene)
                                    || DataPacketIds.Contains(ServerPacketId.PlayerDeath)
                                    || DataPacketIds.Contains(ServerPacketId.PlayerTeamUpdate)
                                    || DataPacketIds.Contains(ServerPacketId.PlayerSkinUpdate);

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
                    DataPacketIds.Add((ServerPacketId) i);
                }

                // Increase the value of current bit
                currentTypeValue *= 2;
            }

            /*
            foreach (var item in DataPacketIds)
            {
                Logger.Info(this,$"reading packet with {Enum.GetName(typeof(ServerPacketId), item)}");
            }
            */


            if (DataPacketIds.Contains(ServerPacketId.HelloServer)) {
                HelloServer.ReadData(Packet);
            }

            if (DataPacketIds.Contains(ServerPacketId.PlayerUpdate)) {
                PlayerUpdate.ReadData(Packet);
            }

            if (DataPacketIds.Contains(ServerPacketId.EntityUpdate)) {
                EntityUpdates.ReadData(Packet);
            }

            if (DataPacketIds.Contains(ServerPacketId.PlayerEnterScene)) {
                PlayerEnterScene.ReadData(Packet);
            }

            if (DataPacketIds.Contains(ServerPacketId.PlayerTeamUpdate)) {
                PlayerTeamUpdate.ReadData(Packet);
            }

            if (DataPacketIds.Contains(ServerPacketId.PlayerSkinUpdate)) {
                PlayerSkinUpdate.ReadData(Packet);
            }

            if (DataPacketIds.Contains(ServerPacketId.PlayerEmoteUpdate)) {
                PlayerEmoteUpdate.ReadData(Packet);
            }
        }

        public override bool ContainsReliableData() {
            return _containsReliableData;
        }

        public void SetLostReliableData(ServerUpdatePacket lostPacket) {
            if (lostPacket.DataPacketIds.Contains(ServerPacketId.HelloServer)) {
                Logger.Get().Info(this, "  Resending HelloServer data");

                DataPacketIds.Add(ServerPacketId.HelloServer);
                HelloServer = lostPacket.HelloServer;
            }

            if (lostPacket.DataPacketIds.Contains(ServerPacketId.PlayerEnterScene)) {
                Logger.Get().Info(this, "  Resending PlayerEnterScene data");

                DataPacketIds.Add(ServerPacketId.PlayerEnterScene);
                PlayerEnterScene = lostPacket.PlayerEnterScene;
            }

            if (lostPacket.DataPacketIds.Contains(ServerPacketId.PlayerLeaveScene)) {
                Logger.Get().Info(this, "  Resending PlayerLeaveScene data");

                DataPacketIds.Add(ServerPacketId.PlayerLeaveScene);
            }

            if (lostPacket.DataPacketIds.Contains(ServerPacketId.PlayerTeamUpdate)) {
                // Only update if the current packet does not already contain another team update
                // since we want the latest update to arrive
                if (!DataPacketIds.Contains(ServerPacketId.PlayerTeamUpdate)) {
                    Logger.Get().Info(this, "  Resending PlayerTeamUpdate data");

                    DataPacketIds.Add(ServerPacketId.PlayerTeamUpdate);
                    PlayerTeamUpdate = lostPacket.PlayerTeamUpdate;
                }
            }

            if (lostPacket.DataPacketIds.Contains(ServerPacketId.PlayerSkinUpdate)) {
                // Only update if the current packet does not already contain another skin update
                // since we want the latest update to arrive
                if (!DataPacketIds.Contains(ServerPacketId.PlayerSkinUpdate)) {
                    Logger.Get().Info(this, "  Resending PlayerSkinUpdate data");

                    DataPacketIds.Add(ServerPacketId.PlayerSkinUpdate);
                    PlayerSkinUpdate = lostPacket.PlayerSkinUpdate;
                }
            }
        }
    }

    public class ClientUpdatePacket : UpdatePacket {
        private bool _containsReliableData;

        public HashSet<ClientPacketId> DataPacketIds { get; }

        public PacketDataCollection<PlayerConnect> PlayerConnect { get; }

        public PacketDataCollection<ClientPlayerDisconnect> PlayerDisconnect { get; }

        public PacketDataCollection<ClientPlayerEnterScene> PlayerEnterScene { get; }

        public ClientAlreadyInScene AlreadyInScene { get; }
        
        public PacketDataCollection<ClientPlayerLeaveScene> PlayerLeaveScene { get; }

        public PacketDataCollection<PlayerUpdate> PlayerUpdates { get; }

        public PacketDataCollection<EntityUpdate> EntityUpdates { get; }

        public PacketDataCollection<GenericClientData> PlayerDeath { get; }

        public PacketDataCollection<ClientPlayerTeamUpdate> PlayerTeamUpdate { get; }

        public PacketDataCollection<ClientPlayerSkinUpdate> PlayerSkinUpdate { get; }

        public PacketDataCollection<ClientPlayerEmoteUpdate> PlayerEmoteUpdate { get; }

        public GameSettingsUpdate GameSettingsUpdate { get; private set; }

        public ClientUpdatePacket() : this(null) {
        }

        public ClientUpdatePacket(Packet packet) : base(packet) {
            DataPacketIds = new HashSet<ClientPacketId>();

            PlayerConnect = new PacketDataCollection<PlayerConnect>();
            PlayerDisconnect = new PacketDataCollection<ClientPlayerDisconnect>();
            PlayerEnterScene = new PacketDataCollection<ClientPlayerEnterScene>();
            AlreadyInScene = new ClientAlreadyInScene();
            PlayerLeaveScene = new PacketDataCollection<ClientPlayerLeaveScene>();
            
            PlayerUpdates = new PacketDataCollection<PlayerUpdate>();
            EntityUpdates = new PacketDataCollection<EntityUpdate>();

            PlayerDeath = new PacketDataCollection<GenericClientData>();
            PlayerTeamUpdate = new PacketDataCollection<ClientPlayerTeamUpdate>();
            PlayerSkinUpdate = new PacketDataCollection<ClientPlayerSkinUpdate>();
            PlayerEmoteUpdate = new PacketDataCollection<ClientPlayerEmoteUpdate>();

            GameSettingsUpdate = new GameSettingsUpdate();
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
                if (DataPacketIds.Contains((ClientPacketId) i)) {
                    dataPacketIdFlag |= currentTypeValue;
                }

                currentTypeValue *= 2;
            }

            packet.Write(dataPacketIdFlag);

            if (DataPacketIds.Contains(ClientPacketId.PlayerConnect)) {
                PlayerConnect.WriteData(packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.PlayerDisconnect)) {
                PlayerDisconnect.WriteData(packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.PlayerEnterScene)) {
                PlayerEnterScene.WriteData(packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.AlreadyInScene)) {
                AlreadyInScene.WriteData(packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.PlayerLeaveScene)) {
                PlayerLeaveScene.WriteData(packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.PlayerUpdate)) {
                PlayerUpdates.WriteData(packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.EntityUpdate)) {
                EntityUpdates.WriteData(packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.PlayerDeath)) {
                PlayerDeath.WriteData(packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.PlayerTeamUpdate)) {
                PlayerTeamUpdate.WriteData(packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.PlayerSkinUpdate)) {
                PlayerSkinUpdate.WriteData(packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.PlayerEmoteUpdate)) {
                PlayerEmoteUpdate.WriteData(packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.GameSettingsUpdated)) {
                GameSettingsUpdate.WriteData(packet);
            }

            _containsReliableData = DataPacketIds.Contains(ClientPacketId.PlayerConnect)
                                    || DataPacketIds.Contains(ClientPacketId.PlayerDisconnect)
                                    || DataPacketIds.Contains(ClientPacketId.PlayerEnterScene)
                                    || DataPacketIds.Contains(ClientPacketId.AlreadyInScene)
                                    || DataPacketIds.Contains(ClientPacketId.PlayerLeaveScene)
                                    || DataPacketIds.Contains(ClientPacketId.PlayerDeath)
                                    || DataPacketIds.Contains(ClientPacketId.PlayerTeamUpdate)
                                    || DataPacketIds.Contains(ClientPacketId.PlayerSkinUpdate)
                                    || DataPacketIds.Contains(ClientPacketId.GameSettingsUpdated);

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
                    DataPacketIds.Add((ClientPacketId) i);
                }

                // Increase the value of current bit
                currentTypeValue *= 2;
            }

            if (DataPacketIds.Contains(ClientPacketId.PlayerConnect)) {
                PlayerConnect.ReadData(Packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.PlayerDisconnect)) {
                PlayerDisconnect.ReadData(Packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.PlayerEnterScene)) {
                PlayerEnterScene.ReadData(Packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.AlreadyInScene)) {
                AlreadyInScene.ReadData(Packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.PlayerLeaveScene)) {
                PlayerLeaveScene.ReadData(Packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.PlayerUpdate)) {
                PlayerUpdates.ReadData(Packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.EntityUpdate)) {
                EntityUpdates.ReadData(Packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.PlayerDeath)) {
                PlayerDeath.ReadData(Packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.PlayerTeamUpdate)) {
                PlayerTeamUpdate.ReadData(Packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.PlayerSkinUpdate)) {
                PlayerSkinUpdate.ReadData(Packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.PlayerEmoteUpdate)) {
                PlayerEmoteUpdate.ReadData(Packet);
            }

            if (DataPacketIds.Contains(ClientPacketId.GameSettingsUpdated)) {
                GameSettingsUpdate.ReadData(Packet);
            }
        }

        public override bool ContainsReliableData() {
            return _containsReliableData;
        }

        // TODO: make sure that resent data does not overwrite newer instance of later sent reliable data
        public void SetLostReliableData(ClientUpdatePacket lostPacket) {
            if (lostPacket.DataPacketIds.Contains(ClientPacketId.PlayerConnect)) {
                Logger.Get().Info(this, "  Resending PlayerConnect data");

                DataPacketIds.Add(ClientPacketId.PlayerConnect);

                PlayerConnect.DataInstances.AddRange(lostPacket.PlayerConnect.DataInstances);
            }

            if (lostPacket.DataPacketIds.Contains(ClientPacketId.PlayerDisconnect)) {
                Logger.Get().Info(this, "  Resending PlayerDisconnect data");

                DataPacketIds.Add(ClientPacketId.PlayerDisconnect);

                PlayerDisconnect.DataInstances.AddRange(lostPacket.PlayerDisconnect.DataInstances);
            }

            if (lostPacket.DataPacketIds.Contains(ClientPacketId.PlayerEnterScene)) {
                Logger.Get().Info(this, "  Resending PlayerEnterScene data");

                DataPacketIds.Add(ClientPacketId.PlayerEnterScene);

                PlayerEnterScene.DataInstances.AddRange(lostPacket.PlayerEnterScene.DataInstances);
            }

            if (lostPacket.DataPacketIds.Contains(ClientPacketId.AlreadyInScene)) {
                Logger.Get().Info(this, "  Resending PlayerAlreadyInScene data");

                DataPacketIds.Add(ClientPacketId.AlreadyInScene);

                AlreadyInScene.PlayerEnterSceneList.AddRange(lostPacket.AlreadyInScene
                    .PlayerEnterSceneList);
            }

            if (lostPacket.DataPacketIds.Contains(ClientPacketId.PlayerLeaveScene)) {
                Logger.Get().Info(this, "  Resending PlayerLeaveScene data");

                DataPacketIds.Add(ClientPacketId.PlayerLeaveScene);

                PlayerLeaveScene.DataInstances.AddRange(lostPacket.PlayerLeaveScene.DataInstances);
            }

            if (lostPacket.DataPacketIds.Contains(ClientPacketId.PlayerDeath)) {
                Logger.Get().Info(this, "  Resending PlayerDeath data");

                DataPacketIds.Add(ClientPacketId.PlayerDeath);

                PlayerDeath.DataInstances.AddRange(lostPacket.PlayerDeath.DataInstances);
            }

            if (lostPacket.DataPacketIds.Contains(ClientPacketId.PlayerTeamUpdate)) {
                Logger.Get().Info(this, "  Resending PlayerTeamUpdate data");

                DataPacketIds.Add(ClientPacketId.PlayerTeamUpdate);

                PlayerTeamUpdate.DataInstances.AddRange(lostPacket.PlayerTeamUpdate.DataInstances);
            }

            if (lostPacket.DataPacketIds.Contains(ClientPacketId.PlayerSkinUpdate)) {
                // Only update if the current packet does not already contain another skin update
                // since we want the latest update to arrive
                if (!DataPacketIds.Contains(ClientPacketId.PlayerSkinUpdate)) {
                    Logger.Get().Info(this, "  Resending PlayerSkinUpdate data");

                    DataPacketIds.Add(ClientPacketId.PlayerSkinUpdate);

                    PlayerSkinUpdate.DataInstances.AddRange(lostPacket.PlayerSkinUpdate.DataInstances);
                }
            }

            if (lostPacket.DataPacketIds.Contains(ClientPacketId.GameSettingsUpdated)) {
                // Only update if the current packet does not already contain another settings update
                // since we want the latest update to arrive
                if (!DataPacketIds.Contains(ClientPacketId.GameSettingsUpdated)) {
                    Logger.Get().Info(this, "  Resending GameSettingsUpdated data");

                    DataPacketIds.Add(ClientPacketId.GameSettingsUpdated);

                    GameSettingsUpdate = lostPacket.GameSettingsUpdate;
                }
            }
        }
    }
}