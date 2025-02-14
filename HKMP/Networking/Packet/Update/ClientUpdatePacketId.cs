namespace Hkmp.Networking.Packet.Update;

/// <summary>
/// Enumeration of packet IDs for the update packet for server to client communication.
/// </summary>
internal enum ClientUpdatePacketId {
    /// <summary>
    /// Indicates slice data from a chunk for large data transfer.
    /// </summary>
    Slice = 0,

    /// <summary>
    /// Indicates the acknowledgement for a slice from a chunk for large data transfer.
    /// </summary>
    SliceAck = 1,

    /// <summary>
    /// Indicating that a client has connected.
    /// </summary>
    PlayerConnect = 2,

    /// <summary>
    /// Indicating that a client is disconnecting.
    /// </summary>
    PlayerDisconnect = 3,

    /// <summary>
    /// Indicating the client is (forcefully) disconnected from the server.
    /// </summary>
    ServerClientDisconnect = 4,

    /// <summary>
    /// Notify that a player has entered the current scene.
    /// </summary>
    PlayerEnterScene = 5,

    /// <summary>
    /// Notify that a player is already in the scene we just entered.
    /// </summary>
    PlayerAlreadyInScene = 6,

    /// <summary>
    /// Notify that a player has left the current scene.
    /// </summary>
    PlayerLeaveScene = 7,

    /// <summary>
    /// Update of realtime player values.
    /// </summary>
    PlayerUpdate = 8,

    /// <summary>
    /// Update of player map position.
    /// </summary>
    PlayerMapUpdate = 9,
    
    /// <summary>
    /// Notify that an entity has spawned.
    /// </summary>
    EntitySpawn = 10,

    /// <summary>
    /// Update of realtime entity values.
    /// </summary>
    EntityUpdate = 11,
    
    /// <summary>
    /// Update of realtime reliable entity values.
    /// </summary>
    ReliableEntityUpdate = 12,

    /// <summary>
    /// Notify that the player becomes scene host of their current scene.
    /// </summary>
    SceneHostTransfer = 13,

    /// <summary>
    /// Notify that a player has died.
    /// </summary>
    PlayerDeath = 14,

    /// <summary>
    /// Notify that a player has changed teams.
    /// </summary>
    PlayerTeamUpdate = 15,

    /// <summary>
    /// Notify that a player has changed skins.
    /// </summary>
    PlayerSkinUpdate = 16,

    /// <summary>
    /// Notify that the gameplay settings have updated.
    /// </summary>
    ServerSettingsUpdated = 17,

    /// <summary>
    /// Player sent chat message.
    /// </summary>
    ChatMessage = 18,
    
    /// <summary>
    /// Value in the save file has updated.
    /// </summary>
    SaveUpdate = 19,
}
