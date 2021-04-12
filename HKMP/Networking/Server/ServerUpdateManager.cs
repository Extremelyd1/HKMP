using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using HKMP.Game;
using HKMP.Game.Client.Entity;
using HKMP.Networking.Packet;
using HKMP.Networking.Packet.Data;
using HKMP.ServerKnights;
using UnityEngine;

namespace HKMP.Networking.Server {
    public class ServerUpdateManager : UdpUpdateManager<ClientUpdatePacket> {

        private readonly IPEndPoint _endPoint;
        
        public ServerUpdateManager(UdpClient udpClient, IPEndPoint endPoint) : base(udpClient) {
            _endPoint = endPoint;
        }
        
        protected override void SendPacket(Packet.Packet packet) {
            if (!UdpClient.Client.Connected) {
                return;
            }
        
            UdpClient.BeginSend(packet.ToArray(), packet.Length(), _endPoint, null, null);
        }
        
        public override void ResendReliableData(ClientUpdatePacket lostPacket) {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.SetLostReliableData(lostPacket);
            }
        }

        public void AddPlayerConnectData(ushort id, string username) {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerConnect);

                CurrentUpdatePacket.PlayerConnect.DataInstances.Add(new PlayerConnect {
                    Id = id,
                    Username = username
                });
            }
        }

        public void AddPlayerDisconnectData(ushort id, string username) {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerDisconnect);

                CurrentUpdatePacket.PlayerDisconnect.DataInstances.Add(new ClientPlayerDisconnect {
                    Id = id,
                    Username = username
                });
            }
        }

        public void AddPlayerEnterSceneData(
            ushort id,
            string username,
            Vector3 position,
            bool scale,
            Team team,
            ushort skin,
            ushort animationClipId
        ) {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerEnterScene);

                CurrentUpdatePacket.PlayerEnterScene.DataInstances.Add(new ClientPlayerEnterScene {
                    Id = id,
                    Username = username,
                    Position = position,
                    Scale = scale,
                    Team = team,
                    Skin = skin,
                    AnimationClipId = animationClipId
                });
            }
        }

        public void AddPlayerAlreadyInSceneData(
            ushort id,
            string username,
            Vector3 position,
            bool scale,
            Team team,
            ushort skin,
            ushort animationClipId
        ) {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerAlreadyInScene);

                CurrentUpdatePacket.PlayerAlreadyInScene.PlayerEnterSceneList.Add(new ClientPlayerEnterScene {
                    Id = id,
                    Username = username,
                    Position = position,
                    Scale = scale,
                    Team = team,
                    Skin = skin,
                    AnimationClipId = animationClipId
                });
            }
        }

        public void SetAlreadyInSceneHost() {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerAlreadyInScene);

                CurrentUpdatePacket.PlayerAlreadyInScene.SceneHost = true;
            }
        }

        public void AddPlayerLeaveSceneData(ushort id) {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerLeaveScene);

                CurrentUpdatePacket.PlayerLeaveScene.DataInstances.Add(new GenericClientData {
                    Id = id
                });
            }
        }

        private PlayerUpdate FindOrCreatePlayerUpdate(ushort id) {
            // Try to find an already existing instance with the same id
            PlayerUpdate playerUpdate = null;
            foreach (var existingPlayerUpdate in CurrentUpdatePacket.PlayerUpdates.DataInstances) {
                if (existingPlayerUpdate.Id == id) {
                    playerUpdate = existingPlayerUpdate;
                    break;
                }
            }
            
            // If no existing instance was found, create one and add it to the list
            if (playerUpdate == null) {
                playerUpdate = new PlayerUpdate {
                    Id = id
                };

                CurrentUpdatePacket.PlayerUpdates.DataInstances.Add(playerUpdate);
            }

            return playerUpdate;
        }

        public void UpdatePlayerPosition(ushort id, Vector3 position) {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerUpdate);

                var playerUpdate = FindOrCreatePlayerUpdate(id);

                playerUpdate.UpdateTypes.Add(PlayerUpdateType.Position);
                playerUpdate.Position = position;
            }
        }

        public void UpdatePlayerScale(ushort id, bool scale) {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerUpdate);

                var playerUpdate = FindOrCreatePlayerUpdate(id);

                playerUpdate.UpdateTypes.Add(PlayerUpdateType.Scale);
                playerUpdate.Scale = scale;
            }
        }

        public void UpdatePlayerMapPosition(ushort id, Vector3 mapPosition) {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerUpdate);

                var playerUpdate = FindOrCreatePlayerUpdate(id);

                playerUpdate.UpdateTypes.Add(PlayerUpdateType.MapPosition);
                playerUpdate.MapPosition = mapPosition;
            }
        }

        public void UpdatePlayerAnimation(ushort id, ushort clipId, byte frame, bool[] effectInfo) {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerUpdate);

                var playerUpdate = FindOrCreatePlayerUpdate(id);

                playerUpdate.UpdateTypes.Add(PlayerUpdateType.Animation);

                var animationInfo = new AnimationInfo {
                    ClipId = clipId,
                    Frame = frame,
                    EffectInfo = effectInfo
                };

                playerUpdate.AnimationInfos.Add(animationInfo);
            }
        }

        private EntityUpdate FindOrCreateEntityUpdate(EntityType entityType, byte entityId) {
            // Try to find an already existing instance with the same type and id
            EntityUpdate entityUpdate = null;
            foreach (var existingEntityUpdate in CurrentUpdatePacket.EntityUpdates.DataInstances) {
                if (existingEntityUpdate.EntityType.Equals(entityType) && existingEntityUpdate.Id == entityId) {
                    entityUpdate = existingEntityUpdate;
                    break;
                }
            }

            // If no existing instance was found, create one and add it to the list
            if (entityUpdate == null) {
                entityUpdate = new EntityUpdate {
                    EntityType = entityType,
                    Id = entityId
                };

                CurrentUpdatePacket.EntityUpdates.DataInstances.Add(entityUpdate);
            }

            return entityUpdate;
        }

        public void UpdateEntityPosition(EntityType entityType, byte entityId, Vector3 position) {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.DataPacketIds.Add(ClientPacketId.EntityUpdate);

                var entityUpdate = FindOrCreateEntityUpdate(entityType, entityId);

                entityUpdate.UpdateTypes.Add(EntityUpdateType.Position);
                entityUpdate.Position = position;
            }
        }

        public void UpdateEntityState(EntityType entityType, byte entityId, byte stateIndex) {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.DataPacketIds.Add(ClientPacketId.EntityUpdate);

                var entityUpdate = FindOrCreateEntityUpdate(entityType, entityId);

                entityUpdate.UpdateTypes.Add(EntityUpdateType.State);
                entityUpdate.State = stateIndex;
            }
        }

        public void UpdateEntityVariables(EntityType entityType, byte entityId, List<byte> fsmVariables) {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.DataPacketIds.Add(ClientPacketId.EntityUpdate);

                var entityUpdate = FindOrCreateEntityUpdate(entityType, entityId);

                entityUpdate.UpdateTypes.Add(EntityUpdateType.Variables);
                entityUpdate.Variables.AddRange(fsmVariables);
            }
        }

        public void AddPlayerDeathData(ushort id) {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerDeath);

                CurrentUpdatePacket.PlayerDeath.DataInstances.Add(new GenericClientData {
                    Id = id
                });
            }
        }

        public void AddPlayerTeamUpdateData(ushort id, string username, Team team) {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerTeamUpdate);

                CurrentUpdatePacket.PlayerTeamUpdate.DataInstances.Add(new ClientPlayerTeamUpdate {
                    Id = id,
                    Username = username,
                    Team = team
                });
            }
        }

        public void AddServerKnightsUpdateData(ushort id, string username, ushort skin , ushort emote) {
            Logger.Info(this,$"writing to client ${id} skin ${skin}");
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.DataPacketIds.Add(ClientPacketId.ServerKnightUpdate);
                CurrentUpdatePacket.ServerKnightUpdate.DataInstances.Add(new ClientServerKnightUpdate {
                    Id = id,
                    Username = username,
                    Skin = skin,
                    Emote = emote
                });
            }
        }


        public void ServerKnightSession(serverJson serverKnightSession) {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.DataPacketIds.Add(ClientPacketId.ServerKnightSession);
                CurrentUpdatePacket.ServerKnightSession.setSession(serverKnightSession);
            }
        }

        public void UpdateGameSettings(Game.Settings.GameSettings gameSettings) {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.DataPacketIds.Add(ClientPacketId.GameSettingsUpdated);

                CurrentUpdatePacket.GameSettingsUpdate.GameSettings = gameSettings;
            }
        }

        public void SetShutdown() {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.DataPacketIds.Add(ClientPacketId.ServerShutdown);
            }
        }
    }
}