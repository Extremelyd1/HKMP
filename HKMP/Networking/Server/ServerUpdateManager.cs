using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using HKMP.Game;
using HKMP.Game.Client.Entity;
using HKMP.Networking.Packet;
using HKMP.Networking.Packet.Data;
using UnityEngine;

namespace HKMP.Networking.Server {
    public class ServerUpdateManager : UdpUpdateManager<ClientUpdatePacket> {

        private readonly IPEndPoint _endPoint;
        
        private ClientUpdatePacket UpdatePacket => (ClientUpdatePacket) CurrentUpdatePacket;

        public ServerUpdateManager(UdpClient udpClient, IPEndPoint endPoint) : base(udpClient) {
            _endPoint = endPoint;
        }
        
        protected override void SendPacket(Packet.Packet packet) {
            UdpClient.BeginSend(packet.ToArray(), packet.Length(), _endPoint, null, null);
        }
        
        public override void ResendReliableData(ClientUpdatePacket lostPacket) {
            lock (CurrentUpdatePacket) {
                CurrentUpdatePacket.SetLostReliableData(lostPacket);
            }
        }

        public void AddPlayerConnectData(ushort id, string username) {
            lock (CurrentUpdatePacket) {
                UpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerConnect);

                UpdatePacket.PlayerConnect.DataInstances.Add(new PlayerConnect {
                    Id = id,
                    Username = username
                });
            }
        }

        public void AddPlayerDisconnectData(ushort id, string username) {
            lock (CurrentUpdatePacket) {
                UpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerDisconnect);

                UpdatePacket.PlayerDisconnect.DataInstances.Add(new ClientPlayerDisconnect {
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
            ushort animationClipId
        ) {
            lock (CurrentUpdatePacket) {
                UpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerEnterScene);

                UpdatePacket.PlayerEnterScene.DataInstances.Add(new ClientPlayerEnterScene {
                    Id = id,
                    Username = username,
                    Position = position,
                    Scale = scale,
                    Team = team,
                    AnimationClipId = animationClipId
                });
            }
        }

        public void AddPlayerLeaveSceneData(ushort id) {
            lock (CurrentUpdatePacket) {
                UpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerLeaveScene);

                UpdatePacket.PlayerLeaveScene.DataInstances.Add(new GenericClientData {
                    Id = id
                });
            }
        }

        private PlayerUpdate FindOrCreatePlayerUpdate(ushort id) {
            // Try to find an already existing instance with the same id
            PlayerUpdate playerUpdate = null;
            foreach (var existingPlayerUpdate in UpdatePacket.PlayerUpdates.DataInstances) {
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

                UpdatePacket.PlayerUpdates.DataInstances.Add(playerUpdate);
            }

            return playerUpdate;
        }

        public void UpdatePlayerPosition(ushort id, Vector3 position) {
            lock (CurrentUpdatePacket) {
                UpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerUpdate);

                var playerUpdate = FindOrCreatePlayerUpdate(id);

                playerUpdate.UpdateTypes.Add(PlayerUpdateType.Position);
                playerUpdate.Position = position;
            }
        }

        public void UpdatePlayerScale(ushort id, bool scale) {
            lock (CurrentUpdatePacket) {
                UpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerUpdate);

                var playerUpdate = FindOrCreatePlayerUpdate(id);

                playerUpdate.UpdateTypes.Add(PlayerUpdateType.Scale);
                playerUpdate.Scale = scale;
            }
        }

        public void UpdatePlayerMapPosition(ushort id, Vector3 mapPosition) {
            lock (CurrentUpdatePacket) {
                UpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerUpdate);

                var playerUpdate = FindOrCreatePlayerUpdate(id);

                playerUpdate.UpdateTypes.Add(PlayerUpdateType.MapPosition);
                playerUpdate.MapPosition = mapPosition;
            }
        }

        public void UpdatePlayerAnimation(ushort id, ushort clipId, byte frame, bool[] effectInfo) {
            lock (CurrentUpdatePacket) {
                UpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerUpdate);

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
            foreach (var existingEntityUpdate in UpdatePacket.EntityUpdates.DataInstances) {
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

                UpdatePacket.EntityUpdates.DataInstances.Add(entityUpdate);
            }

            return entityUpdate;
        }

        public void UpdateEntityPosition(EntityType entityType, byte entityId, Vector3 position) {
            lock (CurrentUpdatePacket) {
                UpdatePacket.DataPacketIds.Add(ClientPacketId.EntityUpdate);

                var entityUpdate = FindOrCreateEntityUpdate(entityType, entityId);

                entityUpdate.UpdateTypes.Add(EntityUpdateType.Position);
                entityUpdate.Position = position;
            }
        }

        public void UpdateEntityState(EntityType entityType, byte entityId, byte stateIndex) {
            lock (CurrentUpdatePacket) {
                UpdatePacket.DataPacketIds.Add(ClientPacketId.EntityUpdate);

                var entityUpdate = FindOrCreateEntityUpdate(entityType, entityId);

                entityUpdate.UpdateTypes.Add(EntityUpdateType.State);
                entityUpdate.StateIndex = stateIndex;
            }
        }

        public void UpdateEntityVariables(EntityType entityType, byte entityId, List<byte> fsmVariables) {
            lock (CurrentUpdatePacket) {
                UpdatePacket.DataPacketIds.Add(ClientPacketId.EntityUpdate);

                var entityUpdate = FindOrCreateEntityUpdate(entityType, entityId);

                entityUpdate.UpdateTypes.Add(EntityUpdateType.Variables);
                entityUpdate.FsmVariables.AddRange(fsmVariables);
            }
        }

        public void AddPlayerDeathData(ushort id) {
            lock (CurrentUpdatePacket) {
                UpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerDeath);

                UpdatePacket.PlayerDeath.DataInstances.Add(new GenericClientData {
                    Id = id
                });
            }
        }

        public void AddPlayerTeamUpdateData(ushort id, string username, Team team) {
            lock (CurrentUpdatePacket) {
                UpdatePacket.DataPacketIds.Add(ClientPacketId.PlayerTeamUpdate);

                UpdatePacket.PlayerTeamUpdate.DataInstances.Add(new ClientPlayerTeamUpdate {
                    Id = id,
                    Username = username,
                    Team = team
                });
            }
        }

        public void UpdateGameSettings(Game.Settings.GameSettings gameSettings) {
            lock (CurrentUpdatePacket) {
                UpdatePacket.DataPacketIds.Add(ClientPacketId.GameSettingsUpdated);

                UpdatePacket.GameSettingsUpdate.GameSettings = gameSettings;
            }
        }

        public void SetShutdown() {
            lock (CurrentUpdatePacket) {
                UpdatePacket.DataPacketIds.Add(ClientPacketId.ServerShutdown);
            }
        }
    }
}