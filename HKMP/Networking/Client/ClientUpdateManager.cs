using System.Collections.Generic;
using Hkmp.Animation;
using Hkmp.Game;
using Hkmp.Game.Client.Entity;
using Hkmp.Math;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Client {
    public class ClientUpdateManager : UdpUpdateManager<ServerUpdatePacket, ServerPacketId> {
        public ClientUpdateManager(UdpNetClient udpNetClient) : base(udpNetClient.UdpClient) {
        }

        protected override void SendPacket(Packet.Packet packet) {
            if (!UdpClient.Client.Connected) {
                return;
            }

            UdpClient.Send(packet.ToArray(), packet.Length);
        }

        public override void ResendReliableData(ServerUpdatePacket lostPacket) {
            lock (Lock) {
                CurrentUpdatePacket.SetLostReliableData(lostPacket);
            }
        }

        private PlayerUpdate FindOrCreatePlayerUpdate() {
            if (!CurrentUpdatePacket.TryGetSendingPacketData(
                ServerPacketId.PlayerUpdate,
                out var packetData)) {
                packetData = new PlayerUpdate();
                CurrentUpdatePacket.SetSendingPacketData(ServerPacketId.PlayerUpdate, packetData);
            }

            return (PlayerUpdate) packetData;
        }

        public void SetLoginRequestData(string username) {
            lock (Lock) {
                var loginRequest = new LoginRequest {
                    Username = username
                };

                CurrentUpdatePacket.SetSendingPacketData(ServerPacketId.LoginRequest, loginRequest);
            }
        }

        public void UpdatePlayerPosition(Vector2 position) {
            lock (Lock) {
                var playerUpdate = FindOrCreatePlayerUpdate();
                playerUpdate.UpdateTypes.Add(PlayerUpdateType.Position);
                playerUpdate.Position = position;
            }
        }

        public void UpdatePlayerScale(bool scale) {
            lock (Lock) {
                var playerUpdate = FindOrCreatePlayerUpdate();
                playerUpdate.UpdateTypes.Add(PlayerUpdateType.Scale);
                playerUpdate.Scale = scale;
            }
        }

        public void UpdatePlayerMapPosition(Vector2 mapPosition) {
            lock (Lock) {
                var playerUpdate = FindOrCreatePlayerUpdate();
                playerUpdate.UpdateTypes.Add(PlayerUpdateType.MapPosition);
                playerUpdate.MapPosition = mapPosition;
            }
        }

        public void UpdatePlayerAnimation(AnimationClip clip, int frame = 0, bool[] effectInfo = null) {
            lock (Lock) {
                var playerUpdate = FindOrCreatePlayerUpdate();
                playerUpdate.UpdateTypes.Add(PlayerUpdateType.Animation);

                // Create a new animation info instance
                var animationInfo = new AnimationInfo {
                    ClipId = (ushort) clip,
                    Frame = (byte) frame,
                    EffectInfo = effectInfo
                };

                // And add it to the list of animation info instances
                playerUpdate.AnimationInfos.Add(animationInfo);
            }
        }

        private EntityUpdate FindOrCreateEntityUpdate(EntityType entityType, byte entityId) {
            EntityUpdate entityUpdate = null;
            PacketDataCollection<EntityUpdate> entityUpdateCollection;
            
            // First check whether there actually exists entity data at all
            if (CurrentUpdatePacket.TryGetSendingPacketData(
                ServerPacketId.EntityUpdate,
                out var packetData)
            ) {
                // And if there exists data already, try to find a match for the entity type and id
                entityUpdateCollection = (PacketDataCollection<EntityUpdate>) packetData;
                foreach (var existingPacketData in entityUpdateCollection.DataInstances) {
                    var existingEntityUpdate = (EntityUpdate) existingPacketData;
                    if (existingEntityUpdate.EntityType.Equals((byte) entityType) && existingEntityUpdate.Id == entityId) {
                        entityUpdate = existingEntityUpdate;
                        break;
                    }
                }
            } else {
                // If no data exists yet, we instantiate the data collection class and put it at the respective key
                entityUpdateCollection = new PacketDataCollection<EntityUpdate>();
                CurrentUpdatePacket.SetSendingPacketData(ServerPacketId.EntityUpdate, entityUpdateCollection);
            }

            // If no existing instance was found, create one and add it to the (newly created) collection
            if (entityUpdate == null) {
                entityUpdate = new EntityUpdate {
                    EntityType = (byte) entityType,
                    Id = entityId
                };

                
                entityUpdateCollection.DataInstances.Add(entityUpdate);
            }

            return entityUpdate;
        }

        public void UpdateEntityPosition(EntityType entityType, byte entityId, Vector2 position) {
            lock (Lock) {
                var entityUpdate = FindOrCreateEntityUpdate(entityType, entityId);

                entityUpdate.UpdateTypes.Add(EntityUpdateType.Position);
                entityUpdate.Position = position;
            }
        }
        
        public void UpdateEntityScale(EntityType entityType, byte entityId, bool scale) {
            lock (Lock) {
                var entityUpdate = FindOrCreateEntityUpdate(entityType, entityId);
                
                entityUpdate.UpdateTypes.Add(EntityUpdateType.Scale);
                entityUpdate.Scale = scale;
            }
        }

        public void UpdateEntityAnimation(
            EntityType entityType, 
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

        public void UpdateEntityState(
            EntityType entityType,
            byte entityId,
            byte state
        ) {
            lock (Lock) {
                var entityUpdate = FindOrCreateEntityUpdate(entityType, entityId);

                entityUpdate.UpdateTypes.Add(EntityUpdateType.State);
                entityUpdate.State = state;
            }
        }

        public void SetPlayerDisconnect() {
            lock (Lock) {
                CurrentUpdatePacket.SetSendingPacketData(ServerPacketId.PlayerDisconnect, new EmptyData());
            }
        }

        public void SetTeamUpdate(Team team) {
            lock (Lock) {
                CurrentUpdatePacket.SetSendingPacketData(
                    ServerPacketId.PlayerTeamUpdate, 
                    new ServerPlayerTeamUpdate { Team = team }
                );
            }
        }

        public void SetSkinUpdate(byte skinId) {
            lock (Lock) {
                CurrentUpdatePacket.SetSendingPacketData(
                    ServerPacketId.PlayerSkinUpdate, 
                    new ServerPlayerSkinUpdate { SkinId = skinId }
                );
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
                CurrentUpdatePacket.SetSendingPacketData(
                    ServerPacketId.HelloServer,
                    new HelloServer {
                        Username = username,
                        SceneName = sceneName,
                        Position = position,
                        Scale = scale,
                        AnimationClipId = animationClipId
                    }
                );
            }
        }

        public void SetEnterSceneData(
            string sceneName,
            Vector2 position,
            bool scale,
            ushort animationClipId
        ) {
            lock (Lock) {
                CurrentUpdatePacket.SetSendingPacketData(
                    ServerPacketId.PlayerEnterScene,
                    new ServerPlayerEnterScene {
                        NewSceneName = sceneName,
                        Position = position,
                        Scale = scale,
                        AnimationClipId = animationClipId
                    }
                );
            }
        }

        public void SetLeftScene() {
            lock (Lock) {
                CurrentUpdatePacket.SetSendingPacketData(ServerPacketId.PlayerLeaveScene, new ReliableEmptyData());
            }
        }

        public void SetDeath() {
            lock (Lock) {
                CurrentUpdatePacket.SetSendingPacketData(ServerPacketId.PlayerDeath, new ReliableEmptyData());
            }
        }
    }
}