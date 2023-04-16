namespace Hkmp.Networking.Packet;

/// <summary>
/// Enumeration of packet IDs for server to client communication.
/// </summary>
internal enum ClientPacketId {
    /// <summary>
    /// A response to the login request to indicate whether the client is allowed to connect.
    /// </summary>
    LoginResponse = 0,

    /// <summary>
    /// A response to the HelloServer after a succeeding login.
    /// </summary>
    HelloClient,

    /// <summary>
    /// Indicating that a client has connected.
    /// </summary>
    PlayerConnect,

    /// <summary>
    /// Indicating that a client is disconnecting.
    /// </summary>
    PlayerDisconnect,

    /// <summary>
    /// Indicating the client is (forcefully) disconnected from the server.
    /// </summary>
    ServerClientDisconnect,

    /// <summary>
    /// Notify that a player has entered the current scene.
    /// </summary>
    PlayerEnterScene,

    /// <summary>
    /// Notify that a player is already in the scene we just entered.
    /// </summary>
    PlayerAlreadyInScene,

    /// <summary>
    /// Notify that a player has left the current scene.
    /// </summary>
    PlayerLeaveScene,

    /// <summary>
    /// Update of realtime player values.
    /// </summary>
    PlayerUpdate,

    /// <summary>
    /// Update of player map position.
    /// </summary>
    PlayerMapUpdate,

    /// <summary>
    /// Update of realtime entity values.
    /// </summary>
    EntityUpdate,

    /// <summary>
    /// Notify that a player has died.
    /// </summary>
    PlayerDeath,

    /// <summary>
    /// Notify that a player has changed teams.
    /// </summary>
    PlayerTeamUpdate,

    /// <summary>
    /// Notify that a player has changed skins.
    /// </summary>
    PlayerSkinUpdate,

    /// <summary>
    /// Notify that the gameplay settings have updated.
    /// </summary>
    ServerSettingsUpdated,

    /// <summary>
    /// Player sent chat message.
    /// </summary>
    ChatMessage = 15
}

/// <summary>
/// Enumeration of packet IDs for client to server communication.
/// </summary>
public enum ServerPacketId {
    /// <summary>
    /// Login packet that indicates that a new client wants to connect.
    /// </summary>
    LoginRequest = 0,

    /// <summary>
    /// Initial hello, sent when login succeeds.
    /// </summary>
    HelloServer,

    /// <summary>
    /// Indicating that a client is disconnecting.
    /// </summary>
    PlayerDisconnect,

    /// <summary>
    /// Update of realtime player values.
    /// </summary>
    PlayerUpdate,

    /// <summary>
    /// Update of player map position.
    /// </summary>
    PlayerMapUpdate,

    /// <summary>
    /// Update of realtime entity values.
    /// </summary>
    EntityUpdate,

    /// <summary>
    /// Notify that the player has entered a new scene.
    /// </summary>
    PlayerEnterScene,

    /// <summary>
    /// Notify that the player has left their current scene.
    /// </summary>
    PlayerLeaveScene,

    /// <summary>
    /// Notify that a player has died.
    /// </summary>
    PlayerDeath,

    /// <summary>
    /// Notify that a player has changed teams.
    /// </summary>
    PlayerTeamUpdate,

    /// <summary>
    /// Notify that a player has changed skins.
    /// </summary>
    PlayerSkinUpdate,

    /// <summary>
    /// Player sent chat message.
    /// </summary>
    ChatMessage = 11
}
