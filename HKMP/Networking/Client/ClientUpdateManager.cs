using System.Collections.Generic;
using Hkmp.Animation;
using Hkmp.Game;
using Hkmp.Game.Client.Entity;
using Hkmp.Math;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Client {
    public class ClientUpdateManager : UdpUpdateManager<ServerUpdatePacket> {
        public ClientUpdateManager(UdpNetClient udpNetClient) : base(udpNetClient.UdpClient) {
        }

        protected override void SendPacket(Packet.Packet packet) {
            if (!UdpClient.Client.Connected) {
                return;
            }

            UdpClient.BeginSend(packet.ToArray(), packet.Length(), null, null);
        }

        public override void ResendReliableData(ServerUpdatePacket lostPacket) {
            lock (Lock) {
                CurrentUpdatePacket.SetLostReliableData(lostPacket);
            }
        }

        public void UpdatePlayerPosition(Vector2 position) {
            lock (Lock) {
                CurrentUpdatePacket.DataPacketIds.Add(ServerPacketId.PlayerUpdate);

                CurrentUpdatePacket.PlayerUpdate.UpdateTypes.Add(PlayerUpdateType.Position);
                CurrentUpdatePacket.PlayerUpdate.Position = position;
            }
        }

        public void UpdatePlayerScale(bool scale) {
            lock (Lock) {
                CurrentUpdatePacket.DataPacketIds.Add(ServerPacketId.PlayerUpdate);

                CurrentUpdatePacket.PlayerUpdate.UpdateTypes.Add(PlayerUpdateType.Scale);
                CurrentUpdatePacket.PlayerUpdate.Scale = scale;
            }
        }

        public void UpdatePlayerMapPosition(Vector2 mapPosition) {
            lock (Lock) {
                CurrentUpdatePacket.DataPacketIds.Add(ServerPacketId.PlayerUpdate);

                CurrentUpdatePacket.PlayerUpdate.UpdateTypes.Add(PlayerUpdateType.MapPosition);
                CurrentUpdatePacket.PlayerUpdate.MapPosition = mapPosition;
            }
        }

        public void UpdatePlayerAnimation(AnimationClip clip, int frame = 0, bool[] effectInfo = null) {
            lock (Lock) {
                CurrentUpdatePacket.DataPacketIds.Add(ServerPacketId.PlayerUpdate);

                CurrentUpdatePacket.PlayerUpdate.UpdateTypes.Add(PlayerUpdateType.Animation);

                // Create a new animation info instance
                var animationInfo = new AnimationInfo {
                    ClipId = (ushort) clip,
                    Frame = (byte) frame,
                    EffectInfo = effectInfo
                };

                // And add it to the list of animation info instances
                CurrentUpdatePacket.PlayerUpdate.AnimationInfos.Add(animationInfo);
            }
        }

        private EntityUpdate FindOrCreateEntityUpdate(EntityType entityType, byte entityId) {
            // Try to find an already existing instance with the same type and id
            EntityUpdate entityUpdate = null;
            foreach (var existingEntityUpdate in CurrentUpdatePacket.EntityUpdates.DataInstances) {
                if (existingEntityUpdate.EntityType == (byte) entityType && existingEntityUpdate.Id == entityId) {
                    entityUpdate = existingEntityUpdate;
                    break;
                }
            }

            // If no existing instance was found, create one and add it to the list
            if (entityUpdate == null) {
                entityUpdate = new EntityUpdate {
                    EntityType = (byte) entityType,
                    Id = entityId
                };

                CurrentUpdatePacket.EntityUpdates.DataInstances.Add(entityUpdate);
            }

            return entityUpdate;
        }

        public void UpdateEntityPosition(EntityType entityType, byte entityId, Vector2 position) {
            lock (Lock) {
                CurrentUpdatePacket.DataPacketIds.Add(ServerPacketId.EntityUpdate);

                var entityUpdate = FindOrCreateEntityUpdate(entityType, entityId);

                entityUpdate.UpdateTypes.Add(EntityUpdateType.Position);
                entityUpdate.Position = position;
            }
        }
        
        public void UpdateEntityScale(EntityType entityType, byte entityId, bool scale) {
            lock (Lock) {
                CurrentUpdatePacket.DataPacketIds.Add(ServerPacketId.EntityUpdate);

                var entityUpdate = FindOrCreateEntityUpdate(entityType, entityId);
                
                entityUpdate.UpdateTypes.Add(EntityUpdateType.Scale);
                entityUpdate.Scale = scale;
            }
        }

        public void UpdateEntityState(EntityType entityType, byte entityId, byte state) {
            lock (Lock) {
                CurrentUpdatePacket.DataPacketIds.Add(ServerPacketId.EntityUpdate);

                var entityUpdate = FindOrCreateEntityUpdate(entityType, entityId);

                entityUpdate.UpdateTypes.Add(EntityUpdateType.State);
                entityUpdate.State = state;
            }
        }

        public void UpdateEntityStateAndVariables(EntityType entityType, byte entityId, byte state,
            List<byte> fsmVariables) {
            lock (Lock) {
                CurrentUpdatePacket.DataPacketIds.Add(ServerPacketId.EntityUpdate);

                var entityUpdate = FindOrCreateEntityUpdate(entityType, entityId);

                entityUpdate.UpdateTypes.Add(EntityUpdateType.State);
                entityUpdate.State = state;

                entityUpdate.UpdateTypes.Add(EntityUpdateType.Variables);
                entityUpdate.Variables.AddRange(fsmVariables);
            }
        }

        public void SetPlayerDisconnect() {
            lock (Lock) {
                CurrentUpdatePacket.DataPacketIds.Add(ServerPacketId.PlayerDisconnect);
            }
        }

        public void SetTeamUpdate(Team team) {
            lock (Lock) {
                CurrentUpdatePacket.DataPacketIds.Add(ServerPacketId.PlayerTeamUpdate);

                CurrentUpdatePacket.PlayerTeamUpdate.Team = team;
            }
        }

        public void SetSkinUpdate(byte skinId) {
            lock (Lock) {
                CurrentUpdatePacket.DataPacketIds.Add(ServerPacketId.PlayerSkinUpdate);

                CurrentUpdatePacket.PlayerSkinUpdate.SkinId = skinId;
            }
        }

        public void SetEmoteUpdate(byte emoteId) {
            lock (Lock) {
                CurrentUpdatePacket.DataPacketIds.Add(ServerPacketId.PlayerEmoteUpdate);

                CurrentUpdatePacket.PlayerEmoteUpdate.EmoteId = emoteId;
            }
        }

        public void SetHelloServerData(
            string username,
            string sceneName,
            Vector2 position,
            bool scale,
            ushort animationClipId
        ) {
            lock (Lock) {
                CurrentUpdatePacket.DataPacketIds.Add(ServerPacketId.HelloServer);

                CurrentUpdatePacket.HelloServer.Username = username;
                CurrentUpdatePacket.HelloServer.SceneName = sceneName;
                CurrentUpdatePacket.HelloServer.Position = position;
                CurrentUpdatePacket.HelloServer.Scale = scale;
                CurrentUpdatePacket.HelloServer.AnimationClipId = animationClipId;
            }
        }

        public void SetEnterSceneData(
            string sceneName,
            Vector2 position,
            bool scale,
            ushort animationClipId
        ) {
            lock (Lock) {
                CurrentUpdatePacket.DataPacketIds.Add(ServerPacketId.PlayerEnterScene);

                CurrentUpdatePacket.PlayerEnterScene.NewSceneName = sceneName;
                CurrentUpdatePacket.PlayerEnterScene.Position = position;
                CurrentUpdatePacket.PlayerEnterScene.Scale = scale;
                CurrentUpdatePacket.PlayerEnterScene.AnimationClipId = animationClipId;
            }
        }

        public void SetLeftScene() {
            lock (Lock) {
                CurrentUpdatePacket.DataPacketIds.Add(ServerPacketId.PlayerLeaveScene);
            }
        }

        public void SetDisconnect() {
            lock (Lock) {
                CurrentUpdatePacket.DataPacketIds.Add(ServerPacketId.PlayerDisconnect);
            }
        }

        public void SetDeath() {
            lock (Lock) {
                CurrentUpdatePacket.DataPacketIds.Add(ServerPacketId.PlayerDeath);
            }
        }
    }
}