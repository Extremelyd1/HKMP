using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Hkmp.Game;
using Hkmp.Math;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking {
    public class ServerUpdateManager : UdpUpdateManager<ClientUpdatePacket, ClientPacketId> {
        private readonly IPEndPoint _endPoint;

        public ServerUpdateManager(UdpClient udpClient, IPEndPoint endPoint) : base(udpClient) {
            _endPoint = endPoint;
        }

        protected override void SendPacket(Packet.Packet packet) {
            UdpClient.Send(packet.ToArray(), packet.Length, _endPoint);
        }

        public override void ResendReliableData(ClientUpdatePacket lostPacket) {
            lock (Lock) {
                CurrentUpdatePacket.SetLostReliableData(lostPacket);
            }
        }
        
        private T FindOrCreatePacketData<T>(ushort id, ClientPacketId packetId) where T : GenericClientData, new() {
            PacketDataCollection<T> packetDataCollection;
            IPacketData packetData = null;
            
            // First check whether there actually exists a data collection for this packet ID
            if (CurrentUpdatePacket.TryGetSendingPacketData(packetId, out var iPacketDataAsCollection)) {
                // And if so, try to find the packet data with the requested client ID
                packetDataCollection = (PacketDataCollection<T>) iPacketDataAsCollection;

                foreach (var existingPacketData in packetDataCollection.DataInstances) {
                    if (((GenericClientData) existingPacketData).Id == id) {
                        packetData = existingPacketData;
                        break;
                    }
                }
            } else {
                // If no data collection exists, we create one instead
                packetDataCollection = new PacketDataCollection<T>();
                CurrentUpdatePacket.SetSendingPacketData(packetId, packetDataCollection);
            }

            // If no existing instance was found, create one and add it to the (newly created) collection
            if (packetData == null) {
                packetData = new T {
                    Id = id
                };

                packetDataCollection.DataInstances.Add(packetData);
            }

            return (T) packetData;
        }

        public void SetLoginResponseData(LoginResponseStatus status) {
            lock (Lock) {
                var loginResponse = new LoginResponse {
                    LoginResponseStatus = status
                };

                CurrentUpdatePacket.SetSendingPacketData(ClientPacketId.LoginResponse, loginResponse);
            }
        }

        public void AddPlayerConnectData(ushort id, string username) {
            lock (Lock) {
                var playerConnect = FindOrCreatePacketData<PlayerConnect>(id, ClientPacketId.PlayerConnect);
                playerConnect.Id = id;
                playerConnect.Username = username;
            }
        }

        public void AddPlayerDisconnectData(ushort id, string username, bool sceneHost, bool timedOut = false) {
            lock (Lock) {
                var playerDisconnect = FindOrCreatePacketData<ClientPlayerDisconnect>(id, ClientPacketId.PlayerDisconnect);
                playerDisconnect.Id = id;
                playerDisconnect.Username = username;
                playerDisconnect.SceneHost = sceneHost;
                playerDisconnect.TimedOut = timedOut;
            }
        }

        public void AddPlayerEnterSceneData(
            ushort id,
            string username,
            Vector2 position,
            bool scale,
            Team team,
            byte skinId,
            ushort animationClipId
        ) {
            lock (Lock) {
                var playerEnterScene =
                    FindOrCreatePacketData<ClientPlayerEnterScene>(id, ClientPacketId.PlayerEnterScene);
                playerEnterScene.Id = id;
                playerEnterScene.Username = username;
                playerEnterScene.Position = position;
                playerEnterScene.Scale = scale;
                playerEnterScene.Team = team;
                playerEnterScene.SkinId = skinId;
                playerEnterScene.AnimationClipId = animationClipId;
            }
        }

        public void AddAlreadyInSceneData(
            IEnumerable<ClientPlayerEnterScene> playerEnterSceneList,
            IEnumerable<EntityUpdate> entityUpdateList,
            bool sceneHost
        ) {
            lock (Lock) {
                var alreadyInScene = new ClientAlreadyInScene {
                    SceneHost = sceneHost
                };
                alreadyInScene.PlayerEnterSceneList.AddRange(playerEnterSceneList);
                alreadyInScene.EntityUpdates.AddRange(entityUpdateList);

                CurrentUpdatePacket.SetSendingPacketData(ClientPacketId.AlreadyInScene, alreadyInScene);
            }
        }

        public void AddPlayerLeaveSceneData(ushort id, bool sceneHost) {
            lock (Lock) {
                var playerLeaveScene = FindOrCreatePacketData<ClientPlayerLeaveScene>(id, ClientPacketId.PlayerLeaveScene);
                playerLeaveScene.Id = id;
                playerLeaveScene.SceneHost = sceneHost;
            }
        }

        public void UpdatePlayerPosition(ushort id, Vector2 position) {
            lock (Lock) {
                var playerUpdate = FindOrCreatePacketData<PlayerUpdate>(id, ClientPacketId.PlayerUpdate);
                playerUpdate.UpdateTypes.Add(PlayerUpdateType.Position);
                playerUpdate.Position = position;
            }
        }

        public void UpdatePlayerScale(ushort id, bool scale) {
            lock (Lock) {
                var playerUpdate = FindOrCreatePacketData<PlayerUpdate>(id, ClientPacketId.PlayerUpdate);
                playerUpdate.UpdateTypes.Add(PlayerUpdateType.Scale);
                playerUpdate.Scale = scale;
            }
        }

        public void UpdatePlayerMapPosition(ushort id, Vector2 mapPosition) {
            lock (Lock) {
                var playerUpdate = FindOrCreatePacketData<PlayerUpdate>(id, ClientPacketId.PlayerUpdate);
                playerUpdate.UpdateTypes.Add(PlayerUpdateType.MapPosition);
                playerUpdate.MapPosition = mapPosition;
            }
        }

        public void UpdatePlayerAnimation(ushort id, ushort clipId, byte frame, bool[] effectInfo) {
            lock (Lock) {
                var playerUpdate = FindOrCreatePacketData<PlayerUpdate>(id, ClientPacketId.PlayerUpdate);
                playerUpdate.UpdateTypes.Add(PlayerUpdateType.Animation);
                
                var animationInfo = new AnimationInfo {
                    ClipId = clipId,
                    Frame = frame,
                    EffectInfo = effectInfo
                };

                playerUpdate.AnimationInfos.Add(animationInfo);
            }
        }
        
        private EntityUpdate FindOrCreateEntityUpdate(byte entityType, byte entityId) {
            EntityUpdate entityUpdate = null;
            PacketDataCollection<EntityUpdate> entityUpdateCollection;
            
            // First check whether there actually exists entity data at all
            if (CurrentUpdatePacket.TryGetSendingPacketData(
                ClientPacketId.EntityUpdate,
                out var packetData)
            ) {
                // And if there exists data already, try to find a match for the entity type and id
                entityUpdateCollection = (PacketDataCollection<EntityUpdate>) packetData;
                foreach (var existingPacketData in entityUpdateCollection.DataInstances) {
                    var existingEntityUpdate = (EntityUpdate) existingPacketData;
                    if (existingEntityUpdate.EntityType.Equals(entityType) && existingEntityUpdate.Id == entityId) {
                        entityUpdate = existingEntityUpdate;
                        break;
                    }
                }
            } else {
                // If no data exists yet, we instantiate the data collection class and put it at the respective key
                entityUpdateCollection = new PacketDataCollection<EntityUpdate>();
                CurrentUpdatePacket.SetSendingPacketData(ClientPacketId.EntityUpdate, entityUpdateCollection);
            }

            // If no existing instance was found, create one and add it to the (newly created) collection
            if (entityUpdate == null) {
                entityUpdate = new EntityUpdate {
                    EntityType = entityType,
                    Id = entityId
                };

                entityUpdateCollection.DataInstances.Add(entityUpdate);
            }

            return entityUpdate;
        }

        public void UpdateEntityPosition(byte entityType, byte entityId, Vector2 position) {
            lock (Lock) {
                var entityUpdate = FindOrCreateEntityUpdate(entityType, entityId);

                entityUpdate.UpdateTypes.Add(EntityUpdateType.Position);
                entityUpdate.Position = position;
            }
        }

        public void UpdateEntityScale(byte entityType, byte entityId, bool scale) {
            lock (Lock) {
                var entityUpdate = FindOrCreateEntityUpdate(entityType, entityId);

                entityUpdate.UpdateTypes.Add(EntityUpdateType.Scale);
                entityUpdate.Scale = scale;
            }
        }

        public void UpdateEntityAnimation(
            byte entityType, 
            byte entityId, 
            byte animationIndex,
            byte[] animationInfo
        ) {
            lock (Lock) {
                var entityUpdate = FindOrCreateEntityUpdate(entityType, entityId);

                entityUpdate.UpdateTypes.Add(EntityUpdateType.Animation);

                var animation = new EntityAnimationInfo {
                    AnimationIndex = animationIndex,
                    AnimationInfo = animationInfo
                };

                entityUpdate.AnimationInfos.Add(animation);
            }
        }

        public void AddPlayerDeathData(ushort id) {
            lock (Lock) {
                var playerDeath = FindOrCreatePacketData<GenericClientData>(id, ClientPacketId.PlayerDeath);
                playerDeath.Id = id;
            }
        }

        public void AddPlayerTeamUpdateData(ushort id, string username, Team team) {
            lock (Lock) {
                var playerTeamUpdate =
                    FindOrCreatePacketData<ClientPlayerTeamUpdate>(id, ClientPacketId.PlayerTeamUpdate);
                playerTeamUpdate.Id = id;
                playerTeamUpdate.Username = username;
                playerTeamUpdate.Team = team;
            }
        }

        public void AddPlayerSkinUpdateData(ushort id, byte skinId) {
            lock (Lock) {
                var playerSkinUpdate =
                    FindOrCreatePacketData<ClientPlayerSkinUpdate>(id, ClientPacketId.PlayerSkinUpdate);
                playerSkinUpdate.Id = id;
                playerSkinUpdate.SkinId = skinId;
            }
        }

        public void UpdateGameSettings(Game.Settings.GameSettings gameSettings) {
            lock (Lock) {
                CurrentUpdatePacket.SetSendingPacketData(
                    ClientPacketId.GameSettingsUpdated,
                    new GameSettingsUpdate {
                        GameSettings = gameSettings
                    }
                );
            }
        }

        public void SetShutdown() {
            lock (Lock) {
                CurrentUpdatePacket.SetSendingPacketData(ClientPacketId.ServerShutdown, new EmptyData());
            }
        }
    }
}