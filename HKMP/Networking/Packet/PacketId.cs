namespace Hkmp.Networking.Packet {
    public enum ClientPacketId {
        // A response to the login request to indicate whether the client is allowed to connect
        LoginResponse = 0,
        
        // A response to the HelloServer after a succeeding login
        HelloClient,
        
        // Indicating that a client has connected
        PlayerConnect,

        // Indicating that a client is disconnecting
        PlayerDisconnect,

        // Indicating that server is shutting down
        ServerShutdown,

        // Notify that a player has entered the current scene
        PlayerEnterScene,

        // Notify that a player is already in the scene we just entered
        PlayerAlreadyInScene,

        // Notify that a player has left the current scene
        PlayerLeaveScene,

        // Update of realtime player values
        PlayerUpdate,

        // Update of realtime entity values
        EntityUpdate,

        // Notify that a player has died
        PlayerDeath,

        // Notify that a player has changed teams
        PlayerTeamUpdate,

        // Notify that a player has changed skins
        PlayerSkinUpdate,

        // Notify that the gameplay settings have updated
        GameSettingsUpdated
    }

    public enum ServerPacketId {
        // Login packet that indicates that a new client wants to connect
        LoginRequest = 0,
        
        // Initial hello, sent when login succeeds
        HelloServer,

        // Indicating that a client is disconnecting
        PlayerDisconnect,

        // Update of realtime player values
        PlayerUpdate,

        // Update of realtime entity values
        EntityUpdate,

        // Notify that the player has entered a new scene
        PlayerEnterScene,

        // Notify that the player has left their current scene
        PlayerLeaveScene,

        // Notify that a player has died
        PlayerDeath,

        // Notify that a player has changed teams
        PlayerTeamUpdate,

        // Notify that a player has changed skins
        PlayerSkinUpdate
    }
}