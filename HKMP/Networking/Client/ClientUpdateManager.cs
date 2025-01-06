using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Hkmp.Animation;
using Hkmp.Game;
using Hkmp.Game.Client.Entity;
using Hkmp.Math;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using Org.BouncyCastle.Tls;

namespace Hkmp.Networking.Client;

/// <summary>
/// Specialization of <see cref="UdpUpdateManager{TOutgoing,TPacketId}"/> for client to server packet sending.
/// </summary>
internal class ClientUpdateManager : UdpUpdateManager<ServerUpdatePacket, ServerPacketId> {
    /// <summary>
    /// Construct the update manager with a UDP net client.
    /// </summary>
    /// <param name="udpSocket">The UDP socket for the local client.</param>
    public ClientUpdateManager(DtlsTransport dtlsTransport) : base(dtlsTransport) {
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
    public void SetEntitySpawn(ushort id, EntityType spawningType, EntityType spawnedType) {
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
    /// <typeparam name="T">The type of the entity update. Either <see cref="EntityUpdate"/> or
    /// <see cref="ReliableEntityUpdate"/>.</typeparam>
    /// <returns>The existing or new EntityUpdate instance.</returns>
    private T FindOrCreateEntityUpdate<T>(ushort entityId) where T : BaseEntityUpdate, new() {
        var entityUpdate = default(T);
        PacketDataCollection<T> entityUpdateCollection;

        var packetId = typeof(T) == typeof(EntityUpdate) 
            ? ServerPacketId.EntityUpdate 
            : ServerPacketId.ReliableEntityUpdate;

        // First check whether there actually exists entity data at all
        if (CurrentUpdatePacket.TryGetSendingPacketData(
                packetId,
                out var packetData
        )) {
            // And if there exists data already, try to find a match for the entity type and id
            entityUpdateCollection = (PacketDataCollection<T>) packetData;
            foreach (var existingPacketData in entityUpdateCollection.DataInstances) {
                var existingEntityUpdate = (T) existingPacketData;
                if (existingEntityUpdate.Id == entityId) {
                    entityUpdate = existingEntityUpdate;
                    break;
                }
            }
        } else {
            // If no data exists yet, we instantiate the data collection class and put it at the respective key
            entityUpdateCollection = new PacketDataCollection<T>();
            CurrentUpdatePacket.SetSendingPacketData(packetId, entityUpdateCollection);
        }

        // If no existing instance was found, create one and add it to the (newly created) collection
        if (entityUpdate == null) {
            if (typeof(T) == typeof(EntityUpdate)) {
                entityUpdate = (T) (object) new EntityUpdate {
                    Id = entityId
                };
            } else {
                entityUpdate = (T) (object) new ReliableEntityUpdate {
                    Id = entityId
                };
            }


            entityUpdateCollection.DataInstances.Add(entityUpdate);
        }

        return entityUpdate;
    }

    /// <summary>
    /// Update an entity's position in the current packet.
    /// </summary>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="position">The new position of the entity.</param>
    public void UpdateEntityPosition(ushort entityId, Vector2 position) {
        lock (Lock) {
            var entityUpdate = FindOrCreateEntityUpdate<EntityUpdate>(entityId);

            entityUpdate.UpdateTypes.Add(EntityUpdateType.Position);
            entityUpdate.Position = position;
        }
    }

    /// <summary>
    /// Update an entity's scale in the current packet.
    /// </summary>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="scale">The scale data of the entity.</param>
    public void UpdateEntityScale(ushort entityId, EntityUpdate.ScaleData scale) {
        lock (Lock) {
            var entityUpdate = FindOrCreateEntityUpdate<EntityUpdate>(entityId);

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
    public void UpdateEntityAnimation(ushort entityId, byte animationId, byte animationWrapMode) {
        lock (Lock) {
            var entityUpdate = FindOrCreateEntityUpdate<EntityUpdate>(entityId);

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
    public void UpdateEntityIsActive(ushort entityId, bool isActive) {
        lock (Lock) {
            var entityUpdate = FindOrCreateEntityUpdate<ReliableEntityUpdate>(entityId);

            entityUpdate.UpdateTypes.Add(EntityUpdateType.Active);
            entityUpdate.IsActive = isActive;
        }
    }
    
    /// <summary>
    /// Add data to an entity's update in the current packet.
    /// </summary>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="data">The entity network data to add.</param>
    public void AddEntityData(ushort entityId, EntityNetworkData data) {
        lock (Lock) {
            var entityUpdate = FindOrCreateEntityUpdate<ReliableEntityUpdate>(entityId);

            entityUpdate.UpdateTypes.Add(EntityUpdateType.Data);
            entityUpdate.GenericData.Add(data);
        }
    }

    /// <summary>
    /// Add host entity FSM data to the current packet.
    /// </summary>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="fsmIndex">The index of the FSM of the entity.</param>
    /// <param name="data">The host FSM data to add.</param>
    public void AddEntityHostFsmData(ushort entityId, byte fsmIndex, EntityHostFsmData data) {
        lock (Lock) {
            var entityUpdate = FindOrCreateEntityUpdate<ReliableEntityUpdate>(entityId);

            entityUpdate.UpdateTypes.Add(EntityUpdateType.HostFsm);

            if (entityUpdate.HostFsmData.TryGetValue(fsmIndex, out var existingData)) {
                existingData.MergeData(data);
            } else {
                entityUpdate.HostFsmData.Add(fsmIndex, data);
            }
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
    /// Set hello server data in the current packet.
    /// </summary>
    /// <param name="username">The username of the player.</param>
    public void SetHelloServerData(string username) {
        lock (Lock) {
            CurrentUpdatePacket.SetSendingPacketData(
                ServerPacketId.HelloServer,
                new HelloServer {
                    Username = username
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

    /// <summary>
    /// Set save update data.
    /// </summary>
    /// <param name="index">The index of the save data entry.</param>
    /// <param name="value">The array of bytes that represents the changed value.</param>
    public void SetSaveUpdate(ushort index, byte[] value) {
        lock (Lock) {
            PacketDataCollection<SaveUpdate> saveUpdateCollection;

            if (CurrentUpdatePacket.TryGetSendingPacketData(ServerPacketId.SaveUpdate, out var packetData)) {
                saveUpdateCollection = (PacketDataCollection<SaveUpdate>) packetData;
            } else {
                saveUpdateCollection = new PacketDataCollection<SaveUpdate>();
                CurrentUpdatePacket.SetSendingPacketData(ServerPacketId.SaveUpdate, saveUpdateCollection);
            }
            
            saveUpdateCollection.DataInstances.Add(new SaveUpdate {
                SaveDataIndex = index,
                Value = value
            });
        }
    }
}
