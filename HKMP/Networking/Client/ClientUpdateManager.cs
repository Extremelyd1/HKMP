using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Hkmp.Animation;
using Hkmp.Game;
using Hkmp.Game.Client.Entity;
using Hkmp.Math;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Client {
    /// <summary>
    /// Specialization of <see cref="UdpUpdateManager{TOutgoing,TPacketId}"/> for client to server packet sending.
    /// </summary>
    internal class ClientUpdateManager : UdpUpdateManager<ServerUpdatePacket, ServerPacketId> {
        /// <summary>
        /// The endpoint of the client.
        /// </summary>
        private readonly IPEndPoint _endPoint;
        
        /// <summary>
        /// Construct the update manager with a UDP net client.
        /// </summary>
        /// <param name="udpSocket">The UDP socket for the local client.</param>
        /// <param name="endPoint">The endpoint of the server.</param>
        public ClientUpdateManager(Socket udpSocket, IPEndPoint endPoint) : base(udpSocket) {
            _endPoint = endPoint;
        }

        /// <inheritdoc />
        protected override void SendPacket(Packet.Packet packet) {
            UdpSocket.SendToAsync(new ArraySegment<byte>(packet.ToArray()), SocketFlags.None, _endPoint);
        }

        /// <inheritdoc />
        public override void ResendReliableData(ServerUpdatePacket lostPacket) {
            lock (Lock) {
                CurrentUpdatePacket.SetLostReliableData(lostPacket);
            }
        }

        /// <summary>
        /// Find an existing or create a new PlayerUpdate instance in the current update packet.
        /// </summary>
        /// <returns>The existing or new PlayerUpdate instance.</returns>
        private PlayerUpdate FindOrCreatePlayerUpdate() {
            if (!CurrentUpdatePacket.TryGetSendingPacketData(
                    ServerPacketId.PlayerUpdate,
                    out var packetData)) {
                packetData = new PlayerUpdate();
                CurrentUpdatePacket.SetSendingPacketData(ServerPacketId.PlayerUpdate, packetData);
            }

            return (PlayerUpdate) packetData;
        }

        /// <summary>
        /// Set the login request data in the current packet.
        /// </summary>
        /// <param name="username">The username of the client.</param>
        /// <param name="authKey">The auth key of the client.</param>
        /// <param name="addonData">A list of addon data of the client.</param>
        public void SetLoginRequestData(string username, string authKey, List<AddonData> addonData) {
            lock (Lock) {
                var loginRequest = new LoginRequest {
                    Username = username,
                    AuthKey = authKey
                };
                loginRequest.AddonData.AddRange(addonData);

                CurrentUpdatePacket.SetSendingPacketData(ServerPacketId.LoginRequest, loginRequest);
            }
        }

        /// <summary>
        /// Update the player position in the current packet.
        /// </summary>
        /// <param name="position">Vector2 representing the new position.</param>
        public void UpdatePlayerPosition(Vector2 position) {
            lock (Lock) {
                var playerUpdate = FindOrCreatePlayerUpdate();
                playerUpdate.UpdateTypes.Add(PlayerUpdateType.Position);
                playerUpdate.Position = position;
            }
        }

        /// <summary>
        /// Update the player scale in the current packet.
        /// </summary>
        /// <param name="scale">The boolean scale.</param>
        public void UpdatePlayerScale(bool scale) {
            lock (Lock) {
                var playerUpdate = FindOrCreatePlayerUpdate();
                playerUpdate.UpdateTypes.Add(PlayerUpdateType.Scale);
                playerUpdate.Scale = scale;
            }
        }

        /// <summary>
        /// Update the player map position in the current packet.
        /// </summary>
        /// <param name="mapPosition">Vector2 representing the new map position.</param>
        public void UpdatePlayerMapPosition(Vector2 mapPosition) {
            lock (Lock) {
                var playerUpdate = FindOrCreatePlayerUpdate();
                playerUpdate.UpdateTypes.Add(PlayerUpdateType.MapPosition);
                playerUpdate.MapPosition = mapPosition;
            }
        }

        /// <summary>
        /// Update whether the player has a map icon.
        /// </summary>
        /// <param name="hasIcon">Whether the player has a map icon.</param>
        public void UpdatePlayerMapIcon(bool hasIcon) {
            lock (Lock) {
                if (!CurrentUpdatePacket.TryGetSendingPacketData(
                        ServerPacketId.PlayerMapUpdate,
                        out var packetData
                    )) {
                    packetData = new PlayerMapUpdate();
                    CurrentUpdatePacket.SetSendingPacketData(ServerPacketId.PlayerMapUpdate, packetData);
                }

                ((PlayerMapUpdate) packetData).HasIcon = hasIcon;
            }
        }

        /// <summary>
        /// Update the player animation in the current packet.
        /// </summary>
        /// <param name="clip">The animation clip.</param>
        /// <param name="frame">The frame of the animation.</param>
        /// <param name="effectInfo">Boolean array of effect info.</param>
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

        /// <summary>
        /// Set entity spawn data for an entity that spawned later in the scene.
        /// </summary>
        /// <param name="id">The ID of the entity.</param>
        /// <param name="spawningType">The type of the entity that spawned the new entity.</param>
        /// <param name="spawnedType">The type of the entity that was spawned.</param>
        public void SetEntitySpawn(byte id, EntityType spawningType, EntityType spawnedType) {
            lock (Lock) {
                PacketDataCollection<EntitySpawn> entitySpawnCollection;

                // Check if there is an existing data collection or create one if not
                if (CurrentUpdatePacket.TryGetSendingPacketData(ServerPacketId.EntitySpawn, out var packetData)) {
                    entitySpawnCollection = (PacketDataCollection<EntitySpawn>) packetData;
                } else {
                    entitySpawnCollection = new PacketDataCollection<EntitySpawn>();
                    CurrentUpdatePacket.SetSendingPacketData(ServerPacketId.EntitySpawn, entitySpawnCollection);
                }
                
                entitySpawnCollection.DataInstances.Add(new EntitySpawn {
                    Id = id,
                    SpawningType = spawningType,
                    SpawnedType = spawnedType
                });
            }
        }

        /// <summary>
        /// Find an existing or create a new EntityUpdate instance in the current update packet.
        /// </summary>
        /// <param name="entityId">The ID of the entity.</param>
        /// <returns>The existing or new EntityUpdate instance.</returns>
        private EntityUpdate FindOrCreateEntityUpdate(byte entityId) {
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
                    if (existingEntityUpdate.Id == entityId) {
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
                    Id = entityId
                };


                entityUpdateCollection.DataInstances.Add(entityUpdate);
            }

            return entityUpdate;
        }

        /// <summary>
        /// Update an entity's position in the current packet.
        /// </summary>
        /// <param name="entityId">The ID of the entity.</param>
        /// <param name="position">The new position of the entity.</param>
        public void UpdateEntityPosition(byte entityId, Vector2 position) {
            lock (Lock) {
                var entityUpdate = FindOrCreateEntityUpdate(entityId);

                entityUpdate.UpdateTypes.Add(EntityUpdateType.Position);
                entityUpdate.Position = position;
            }
        }

        /// <summary>
        /// Update an entity's scale in the current packet.
        /// </summary>
        /// <param name="entityId">The ID of the entity.</param>
        /// <param name="scale">The new scale of the entity.</param>
        public void UpdateEntityScale(byte entityId, bool scale) {
            lock (Lock) {
                var entityUpdate = FindOrCreateEntityUpdate(entityId);

                entityUpdate.UpdateTypes.Add(EntityUpdateType.Scale);
                entityUpdate.Scale = scale;
            }
        }

        /// <summary>
        /// Update an entity's animation ID in the current packet.
        /// </summary>
        /// <param name="entityId">The ID of the entity.</param>
        /// <param name="animationId">The new animation ID of the entity.</param>
        /// <param name="animationWrapMode">The wrap mode of the animation of the entity.</param>
        public void UpdateEntityAnimation(byte entityId, byte animationId, byte animationWrapMode) {
            lock (Lock) {
                var entityUpdate = FindOrCreateEntityUpdate(entityId);

                entityUpdate.UpdateTypes.Add(EntityUpdateType.Animation);
                entityUpdate.AnimationId = animationId;
                entityUpdate.AnimationWrapMode = animationWrapMode;
            }
        }

        /// <summary>
        /// Update whether an entity is active or not.
        /// </summary>
        /// <param name="entityId">The ID of the entity.</param>
        /// <param name="isActive">Whether the entity is active or not.</param>
        public void UpdateEntityIsActive(byte entityId, bool isActive) {
            lock (Lock) {
                var entityUpdate = FindOrCreateEntityUpdate(entityId);

                entityUpdate.UpdateTypes.Add(EntityUpdateType.Active);
                entityUpdate.IsActive = isActive;
            }
        }

        /// <summary>
        /// Add data to an entity's update in the current packet.
        /// </summary>
        /// <param name="entityId">The ID of the entity.</param>
        /// <param name="data">The entity network data to add.</param>
        public void AddEntityData(byte entityId, EntityNetworkData data) {
            lock (Lock) {
                var entityUpdate = FindOrCreateEntityUpdate(entityId);

                entityUpdate.UpdateTypes.Add(EntityUpdateType.Data);
                entityUpdate.GenericData.Add(data);
            }
        }

        /// <summary>
        /// Set that the player has disconnected in the current packet.
        /// </summary>
        public void SetPlayerDisconnect() {
            lock (Lock) {
                CurrentUpdatePacket.SetSendingPacketData(ServerPacketId.PlayerDisconnect, new EmptyData());
            }
        }

        /// <summary>
        /// Set a team update in the current packet.
        /// </summary>
        /// <param name="team">The new team of the player.</param>
        public void SetTeamUpdate(Team team) {
            lock (Lock) {
                CurrentUpdatePacket.SetSendingPacketData(
                    ServerPacketId.PlayerTeamUpdate,
                    new ServerPlayerTeamUpdate { Team = team }
                );
            }
        }

        /// <summary>
        /// Set a skin update in the current packet.
        /// </summary>
        /// <param name="skinId">The ID of the skin of the player.</param>
        public void SetSkinUpdate(byte skinId) {
            lock (Lock) {
                CurrentUpdatePacket.SetSendingPacketData(
                    ServerPacketId.PlayerSkinUpdate,
                    new ServerPlayerSkinUpdate { SkinId = skinId }
                );
            }
        }

        /// <summary>
        /// Set hello server data in the current packet.
        /// </summary>
        /// <param name="username">The username of the player.</param>
        /// <param name="sceneName">The name of the current scene of the player.</param>
        /// <param name="position">The position of the player.</param>
        /// <param name="scale">The scale of the player.</param>
        /// <param name="animationClipId">The animation clip ID of the player.</param>
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

        /// <summary>
        /// Set enter scene data in the current packet.
        /// </summary>
        /// <param name="sceneName">The name of the entered scene.</param>
        /// <param name="position">The position of the player.</param>
        /// <param name="scale">The scale of the player.</param>
        /// <param name="animationClipId">The animation clip ID of the player.</param>
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

        /// <summary>
        /// Set that the player has left the current scene in the current packet.
        /// </summary>
        public void SetLeftScene() {
            lock (Lock) {
                CurrentUpdatePacket.SetSendingPacketData(ServerPacketId.PlayerLeaveScene, new ReliableEmptyData());
            }
        }

        /// <summary>
        /// Set that the player has died in the current packet.
        /// </summary>
        public void SetDeath() {
            lock (Lock) {
                CurrentUpdatePacket.SetSendingPacketData(ServerPacketId.PlayerDeath, new ReliableEmptyData());
            }
        }

        /// <summary>
        /// Set a chat message in the current packet.
        /// </summary>
        /// <param name="message">The string message.</param>
        public void SetChatMessage(string message) {
            lock (Lock) {
                CurrentUpdatePacket.SetSendingPacketData(ServerPacketId.ChatMessage, new ChatMessage {
                    Message = message
                });
            }
        }
    }
}
