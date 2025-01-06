using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Hkmp.Game;
using Hkmp.Game.Client.Entity;
using Hkmp.Game.Settings;
using Hkmp.Math;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using Org.BouncyCastle.Tls;

namespace Hkmp.Networking.Server;

/// <summary>
/// Specialization of <see cref="UdpUpdateManager{TOutgoing,TPacketId}"/> for server to client packet sending.
/// </summary>
internal class ServerUpdateManager : UdpUpdateManager<ClientUpdatePacket, ClientPacketId> {
    /// <summary>
    /// The endpoint of the client.
    /// </summary>
    private readonly IPEndPoint _endPoint;

    /// <summary>
    /// Construct the update manager with the given details.
    /// </summary>
    /// <param name="udpSocket">The underlying UDP socket for this client.</param>
    /// <param name="endPoint">The endpoint of the client.</param>
    public ServerUpdateManager(DtlsTransport dtlsTransport, IPEndPoint endPoint) : base(dtlsTransport) {
        _endPoint = endPoint;
    }

    /// <inheritdoc />
    public override void ResendReliableData(ClientUpdatePacket lostPacket) {
        lock (Lock) {
            CurrentUpdatePacket.SetLostReliableData(lostPacket);
        }
    }

    /// <summary>
    /// Find or create a packet data instance in the current packet that matches the given ID of a client.
    /// </summary>
    /// <param name="id">The ID of the client in the generic client data.</param>
    /// <param name="packetId">The ID of the packet data.</param>
    /// <typeparam name="T">The type of the generic client packet data.</typeparam>
    /// <returns>An instance of the packet data in the packet.</returns>
    private T FindOrCreatePacketData<T>(ushort id, ClientPacketId packetId) where T : GenericClientData, new() {
        return FindOrCreatePacketData(
            packetId,
            packetData => packetData.Id == id,
            () => new T {
                Id = id
            }
        );
    }

    /// <summary>
    /// Find or create a packet data instance in the current packet that matches a function.
    /// </summary>
    /// <param name="packetId">The ID of the packet data.</param>
    /// <param name="findFunc">The function to match the packet data.</param>
    /// <param name="constructFunc">The function to construct the packet data if it does not exist.</param>
    /// <typeparam name="T">The type of the generic client packet data.</typeparam>
    /// <returns>An instance of the packet data in the packet.</returns>
    private T FindOrCreatePacketData<T>(
        ClientPacketId packetId, 
        Func<T, bool> findFunc, 
        Func<T> constructFunc
    ) where T : IPacketData, new() {
        PacketDataCollection<T> packetDataCollection;
        IPacketData packetData = null;

        // First check whether there actually exists a data collection for this packet ID
        if (CurrentUpdatePacket.TryGetSendingPacketData(packetId, out var iPacketDataAsCollection)) {
            // And if so, try to find the packet data with the requested client ID
            packetDataCollection = (PacketDataCollection<T>) iPacketDataAsCollection;

            foreach (T existingPacketData in packetDataCollection.DataInstances) {
                if (findFunc(existingPacketData)) {
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
            packetData = constructFunc.Invoke();

            packetDataCollection.DataInstances.Add(packetData);
        }

        return (T) packetData;
    }

    /// <summary>
    /// Set login response data in the current packet.
    /// </summary>
    /// <param name="loginResponse">The login response data.</param>
    public void SetLoginResponse(LoginResponse loginResponse) {
        lock (Lock) {
            CurrentUpdatePacket.SetSendingPacketData(ClientPacketId.LoginResponse, loginResponse);
        }
    }

    /// <summary>
    /// Set hello client data in the current packet.
    /// </summary>
    /// <param name="currentSave">Dictionary containing current save data of the server.</param>
    /// <param name="clientInfo">The list of pairs of client IDs and usernames.</param>
    public void SetHelloClientData(Dictionary<ushort, byte[]> currentSave, List<(ushort, string)> clientInfo) {
        lock (Lock) {
            var helloClient = new HelloClient {
                ClientInfo = clientInfo,
                CurrentSave = new CurrentSave {
                    SaveData = currentSave
                }
            };
            CurrentUpdatePacket.SetSendingPacketData(ClientPacketId.HelloClient, helloClient);
        }
    }

    /// <summary>
    /// Add player connect data to the current packet.
    /// </summary>
    /// <param name="id">The ID of the player connecting.</param>
    /// <param name="username">The username of the player connecting.</param>
    public void AddPlayerConnectData(ushort id, string username) {
        lock (Lock) {
            var playerConnect = FindOrCreatePacketData<PlayerConnect>(id, ClientPacketId.PlayerConnect);
            playerConnect.Id = id;
            playerConnect.Username = username;
        }
    }

    /// <summary>
    /// Add player disconnect data to the current packet.
    /// </summary>
    /// <param name="id">The ID of the player disconnecting.</param>
    /// <param name="username">The username of the player disconnecting.</param>
    /// <param name="timedOut">Whether the player timed out or disconnected normally.</param>
    public void AddPlayerDisconnectData(ushort id, string username, bool timedOut = false) {
        lock (Lock) {
            var playerDisconnect =
                FindOrCreatePacketData<ClientPlayerDisconnect>(id, ClientPacketId.PlayerDisconnect);
            playerDisconnect.Id = id;
            playerDisconnect.Username = username;
            playerDisconnect.TimedOut = timedOut;
        }
    }

    /// <summary>
    /// Add player enter scene data to the current packet.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="username">The username of the player.</param>
    /// <param name="position">The position of the player.</param>
    /// <param name="scale">The scale of the player.</param>
    /// <param name="team">The team of the player.</param>
    /// <param name="skinId">The ID of the skin of the player.</param>
    /// <param name="animationClipId">The ID of the animation clip of the player.</param>
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

    /// <summary>
    /// Add player already in scene data to the current packet.
    /// </summary>
    /// <param name="playerEnterSceneList">An enumerable of ClientPlayerEnterScene instances to add.</param>
    /// <param name="entitySpawnList">An enumerable of EntitySpawn instances to add.</param> 
    /// <param name="entityUpdateList">An enumerable of EntityUpdate instances to add.</param>
    /// <param name="reliableEntityUpdateList">An enumerable of ReliableEntityUpdate instances to add.</param>
    /// <param name="sceneHost">Whether the player is the scene host.</param>
    public void AddPlayerAlreadyInSceneData(
        IEnumerable<ClientPlayerEnterScene> playerEnterSceneList,
        IEnumerable<EntitySpawn> entitySpawnList,
        IEnumerable<EntityUpdate> entityUpdateList,
        IEnumerable<ReliableEntityUpdate> reliableEntityUpdateList,
        bool sceneHost
    ) {
        lock (Lock) {
            var alreadyInScene = new ClientPlayerAlreadyInScene {
                SceneHost = sceneHost
            };
            alreadyInScene.PlayerEnterSceneList.AddRange(playerEnterSceneList);
            alreadyInScene.EntitySpawnList.AddRange(entitySpawnList);
            alreadyInScene.EntityUpdateList.AddRange(entityUpdateList);
            alreadyInScene.ReliableEntityUpdateList.AddRange(reliableEntityUpdateList);

            CurrentUpdatePacket.SetSendingPacketData(ClientPacketId.PlayerAlreadyInScene, alreadyInScene);
        }
    }

    /// <summary>
    /// Add player leave scene data to the current packet.
    /// </summary>
    /// <param name="id">The ID of the leaving player.</param>
    public void AddPlayerLeaveSceneData(ushort id) {
        lock (Lock) {
            var playerLeaveScene = FindOrCreatePacketData<ClientPlayerLeaveScene>(id, ClientPacketId.PlayerLeaveScene);
            playerLeaveScene.Id = id;
        }
    }

    /// <summary>
    /// Update a player's position in the current packet.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="position">The position of the player.</param>
    public void UpdatePlayerPosition(ushort id, Vector2 position) {
        lock (Lock) {
            var playerUpdate = FindOrCreatePacketData<PlayerUpdate>(id, ClientPacketId.PlayerUpdate);
            playerUpdate.UpdateTypes.Add(PlayerUpdateType.Position);
            playerUpdate.Position = position;
        }
    }

    /// <summary>
    /// Update a player's scale in the current packet.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="scale">The scale of the player.</param>
    public void UpdatePlayerScale(ushort id, bool scale) {
        lock (Lock) {
            var playerUpdate = FindOrCreatePacketData<PlayerUpdate>(id, ClientPacketId.PlayerUpdate);
            playerUpdate.UpdateTypes.Add(PlayerUpdateType.Scale);
            playerUpdate.Scale = scale;
        }
    }

    /// <summary>
    /// Update a player's map position in the current packet.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="mapPosition">The map position of the player.</param>
    public void UpdatePlayerMapPosition(ushort id, Vector2 mapPosition) {
        lock (Lock) {
            var playerUpdate = FindOrCreatePacketData<PlayerUpdate>(id, ClientPacketId.PlayerUpdate);
            playerUpdate.UpdateTypes.Add(PlayerUpdateType.MapPosition);
            playerUpdate.MapPosition = mapPosition;
        }
    }

    /// <summary>
    /// Update whether the player has a map icon.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="hasIcon">Whether the player has a map icon.</param>
    public void UpdatePlayerMapIcon(ushort id, bool hasIcon) {
        lock (Lock) {
            var playerMapUpdate = FindOrCreatePacketData<PlayerMapUpdate>(id, ClientPacketId.PlayerMapUpdate);
            playerMapUpdate.HasIcon = hasIcon;
        }
    }

    /// <summary>
    /// Update a player's animation in the current packet.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="clipId">The ID of the animation clip.</param>
    /// <param name="frame">The frame of the animation.</param>
    /// <param name="effectInfo">Boolean array containing effect info.</param>
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

    /// <summary>
    /// Set entity spawn data for an entity that spawned.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="spawningType">The type of the entity that spawned the new entity.</param>
    /// <param name="spawnedType">The type of the entity that was spawned.</param>
    public void SetEntitySpawn(ushort id, EntityType spawningType, EntityType spawnedType) {
        lock (Lock) {
            PacketDataCollection<EntitySpawn> entitySpawnCollection;

            if (CurrentUpdatePacket.TryGetSendingPacketData(ClientPacketId.EntitySpawn, out var packetData)) {
                entitySpawnCollection = (PacketDataCollection<EntitySpawn>) packetData;
            } else {
                entitySpawnCollection = new PacketDataCollection<EntitySpawn>();
                CurrentUpdatePacket.SetSendingPacketData(ClientPacketId.EntitySpawn, entitySpawnCollection);
            }
            
            entitySpawnCollection.DataInstances.Add(new EntitySpawn {
                Id = id,
                SpawningType = spawningType,
                SpawnedType = spawnedType
            });
        }
    }

    /// <summary>
    /// Find or create an entity update instance in the current packet.
    /// </summary>
    /// <param name="entityId">The ID of the entity.</param>
    /// <typeparam name="T">The type of the entity update. Either <see cref="EntityUpdate"/> or
    /// <see cref="ReliableEntityUpdate"/>.</typeparam>
    /// <returns>An instance of the entity update in the packet.</returns>
    private T FindOrCreateEntityUpdate<T>(ushort entityId) where T : BaseEntityUpdate, new() {
        var entityUpdate = default(T);
        PacketDataCollection<T> entityUpdateCollection;
        
        var packetId = typeof(T) == typeof(EntityUpdate) 
            ? ClientPacketId.EntityUpdate 
            : ClientPacketId.ReliableEntityUpdate;

        // First check whether there actually exists entity data at all
        if (CurrentUpdatePacket.TryGetSendingPacketData(
                packetId,
                out var packetData)
           ) {
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
    /// Update an entity's position in the packet.
    /// </summary>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="position">The position of the entity.</param>
    public void UpdateEntityPosition(ushort entityId, Vector2 position) {
        lock (Lock) {
            var entityUpdate = FindOrCreateEntityUpdate<EntityUpdate>(entityId);

            entityUpdate.UpdateTypes.Add(EntityUpdateType.Position);
            entityUpdate.Position = position;
        }
    }
        
    /// <summary>
    /// Update an entity's scale in the packet.
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
    /// Update an entity's animation in the packet.
    /// </summary>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="animationId">The animation ID of the entity.</param>
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
    /// <param name="data">The list of entity network data to add.</param>
    public void AddEntityData(ushort entityId, List<EntityNetworkData> data) {
        lock (Lock) {
            var entityUpdate = FindOrCreateEntityUpdate<ReliableEntityUpdate>(entityId);

            entityUpdate.UpdateTypes.Add(EntityUpdateType.Data);
            entityUpdate.GenericData.AddRange(data);
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
    /// Set that the receiving player should become scene host of their current scene.
    /// </summary>
    /// <param name="sceneName">The name of the scene in which the player becomes scene host.</param>
    public void SetSceneHostTransfer(string sceneName) {
        lock (Lock) {
            CurrentUpdatePacket.SetSendingPacketData(ClientPacketId.SceneHostTransfer, new HostTransfer {
                SceneName = sceneName
            });
        }
    }

    /// <summary>
    /// Add player death data to the current packet.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    public void AddPlayerDeathData(ushort id) {
        lock (Lock) {
            var playerDeath = FindOrCreatePacketData<GenericClientData>(id, ClientPacketId.PlayerDeath);
            playerDeath.Id = id;
        }
    }

    /// <summary>
    /// Add a team update to the current packet for the receiving player.
    /// </summary>
    /// <param name="team">The team of the player.</param>
    public void AddPlayerTeamUpdateData(Team team) {
        lock (Lock) {
            var playerTeamUpdate = FindOrCreatePacketData(
                ClientPacketId.PlayerTeamUpdate,
                packetData => packetData.Self,
                () => new ClientPlayerTeamUpdate {
                    Self = true
                }
            );
            playerTeamUpdate.Team = team;
        }
    }
    
    /// <summary>
    /// Add a player team update to the current packet for another player.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="team">The team of the player.</param>
    public void AddOtherPlayerTeamUpdateData(ushort id, Team team) {
        lock (Lock) {
            var playerTeamUpdate = FindOrCreatePacketData(
                ClientPacketId.PlayerTeamUpdate,
                packetData => packetData.Id == id && !packetData.Self,
                () => new ClientPlayerTeamUpdate {
                    Id = id
                }
            );
            playerTeamUpdate.Team = team;
        }
    }

    /// <summary>
    /// Add a skin update to the current packet for the receiving player.
    /// </summary>
    /// <param name="skinId">The ID of the skin of the player.</param>
    public void AddPlayerSkinUpdateData(byte skinId) {
        lock (Lock) {
            var playerSkinUpdate = FindOrCreatePacketData(
                ClientPacketId.PlayerSkinUpdate,
                packetData => packetData.Self,
                () => new ClientPlayerSkinUpdate {
                    Self = true
                }
            );
            playerSkinUpdate.SkinId = skinId;
        }
    }
    
    /// <summary>
    /// Add a skin update to the current packet for another player.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="skinId">The ID of the skin of the player.</param>
    public void AddOtherPlayerSkinUpdateData(ushort id, byte skinId) {
        lock (Lock) {
            var playerSkinUpdate = FindOrCreatePacketData(
                ClientPacketId.PlayerSkinUpdate,
                packetData => packetData.Id == id && !packetData.Self,
                () => new ClientPlayerSkinUpdate {
                    Id = id
                }
            );
            playerSkinUpdate.SkinId = skinId;
        }
    }

    /// <summary>
    /// Update the server settings in the current packet.
    /// </summary>
    /// <param name="serverSettings">The ServerSettings instance.</param>
    public void UpdateServerSettings(ServerSettings serverSettings) {
        lock (Lock) {
            CurrentUpdatePacket.SetSendingPacketData(
                ClientPacketId.ServerSettingsUpdated,
                new ServerSettingsUpdate {
                    ServerSettings = serverSettings
                }
            );
        }
    }

    /// <summary>
    /// Set that the client is disconnected from the server with the given reason.
    /// </summary>
    /// <param name="reason">The reason for the disconnect.</param>
    public void SetDisconnect(DisconnectReason reason) {
        lock (Lock) {
            CurrentUpdatePacket.SetSendingPacketData(
                ClientPacketId.ServerClientDisconnect,
                new ServerClientDisconnect {
                    Reason = reason
                }
            );
        }
    }

    /// <summary>
    /// Add a chat message to the current packet.
    /// </summary>
    /// <param name="message">The string message.</param>
    public void AddChatMessage(string message) {
        lock (Lock) {
            PacketDataCollection<ChatMessage> packetDataCollection;

            if (CurrentUpdatePacket.TryGetSendingPacketData(ClientPacketId.ChatMessage, out var packetData)) {
                packetDataCollection = (PacketDataCollection<ChatMessage>) packetData;
            } else {
                packetDataCollection = new PacketDataCollection<ChatMessage>();

                CurrentUpdatePacket.SetSendingPacketData(ClientPacketId.ChatMessage, packetDataCollection);
            }

            packetDataCollection.DataInstances.Add(new ChatMessage {
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

            if (CurrentUpdatePacket.TryGetSendingPacketData(ClientPacketId.SaveUpdate, out var packetData)) {
                saveUpdateCollection = (PacketDataCollection<SaveUpdate>) packetData;
            } else {
                saveUpdateCollection = new PacketDataCollection<SaveUpdate>();
                CurrentUpdatePacket.SetSendingPacketData(ClientPacketId.SaveUpdate, saveUpdateCollection);
            }
            
            saveUpdateCollection.DataInstances.Add(new SaveUpdate {
                SaveDataIndex = index,
                Value = value
            });
        }
    }
}
