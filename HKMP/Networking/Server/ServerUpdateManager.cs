using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Hkmp.Game;
using Hkmp.Game.Settings;
using Hkmp.Math;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;

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
    public ServerUpdateManager(Socket udpSocket, IPEndPoint endPoint) : base(udpSocket) {
        _endPoint = endPoint;
    }

    /// <inheritdoc />
    protected override void SendPacket(Packet.Packet packet) {
        UdpSocket.SendToAsync(new ArraySegment<byte>(packet.ToArray()), SocketFlags.None, _endPoint);
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
    /// <param name="clientInfo">The list of pairs of client IDs and usernames.</param>
    public void SetHelloClientData(List<(ushort, string)> clientInfo) {
        lock (Lock) {
            var helloClient = new HelloClient {
                ClientInfo = clientInfo
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
    /// <param name="sceneHost">Whether the player is the scene host.</param>
    public void AddPlayerAlreadyInSceneData(
        IEnumerable<ClientPlayerEnterScene> playerEnterSceneList,
        bool sceneHost
    ) {
        lock (Lock) {
            var alreadyInScene = new ClientPlayerAlreadyInScene {
                SceneHost = sceneHost
            };
            alreadyInScene.PlayerEnterSceneList.AddRange(playerEnterSceneList);

            CurrentUpdatePacket.SetSendingPacketData(ClientPacketId.PlayerAlreadyInScene, alreadyInScene);
        }
    }

    /// <summary>
    /// Add player leave scene data to the current packet.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    public void AddPlayerLeaveSceneData(ushort id) {
        lock (Lock) {
            var playerLeaveScene = FindOrCreatePacketData<GenericClientData>(id, ClientPacketId.PlayerLeaveScene);
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
    /// Find or create an entity update instance in the current packet.
    /// </summary>
    /// <param name="entityType">The type of the entity.</param>
    /// <param name="entityId">The ID of the entity.</param>
    /// <returns>An instance of the entity update in the packet.</returns>
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

    /// <summary>
    /// Update an entity's position in the packet.
    /// </summary>
    /// <param name="entityType">The type of the entity.</param>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="position">The position of the entity.</param>
    public void UpdateEntityPosition(byte entityType, byte entityId, Vector2 position) {
        lock (Lock) {
            var entityUpdate = FindOrCreateEntityUpdate(entityType, entityId);

            entityUpdate.UpdateTypes.Add(EntityUpdateType.Position);
            entityUpdate.Position = position;
        }
    }

    /// <summary>
    /// Update an entity's state in the packet.
    /// </summary>
    /// <param name="entityType">The type of the entity.</param>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="stateIndex">The state index of the entity.</param>
    public void UpdateEntityState(byte entityType, byte entityId, byte stateIndex) {
        lock (Lock) {
            var entityUpdate = FindOrCreateEntityUpdate(entityType, entityId);

            entityUpdate.UpdateTypes.Add(EntityUpdateType.State);
            entityUpdate.State = stateIndex;
        }
    }

    /// <summary>
    /// Update an entity's variables in the packet.
    /// </summary>
    /// <param name="entityType">The type of the entity.</param>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="fsmVariables">The variables of the entity.</param>
    public void UpdateEntityVariables(byte entityType, byte entityId, List<byte> fsmVariables) {
        lock (Lock) {
            var entityUpdate = FindOrCreateEntityUpdate(entityType, entityId);

            entityUpdate.UpdateTypes.Add(EntityUpdateType.Variables);
            entityUpdate.Variables.AddRange(fsmVariables);
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
    /// Add a player team update to the current packet.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="username">The username of the player.</param>
    /// <param name="team">The team of the player.</param>
    public void AddPlayerTeamUpdateData(ushort id, string username, Team team) {
        lock (Lock) {
            var playerTeamUpdate =
                FindOrCreatePacketData<ClientPlayerTeamUpdate>(id, ClientPacketId.PlayerTeamUpdate);
            playerTeamUpdate.Id = id;
            playerTeamUpdate.Username = username;
            playerTeamUpdate.Team = team;
        }
    }

    /// <summary>
    /// Add a player skin update to the current packet.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="skinId">The ID of the skin of the player.</param>
    public void AddPlayerSkinUpdateData(ushort id, byte skinId) {
        lock (Lock) {
            var playerSkinUpdate =
                FindOrCreatePacketData<ClientPlayerSkinUpdate>(id, ClientPacketId.PlayerSkinUpdate);
            playerSkinUpdate.Id = id;
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
}
