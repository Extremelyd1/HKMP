namespace HKMP.Networking.Packet {
    public enum PacketId {
        // Server-bound
        // Initial hello, sent when the player first connects
        HelloServer = 1,
        
        // Client-bound
        // Indicating that a client has connected
        PlayerConnect,
        
        // Server-bound
        // Indicating that client is disconnecting
        PlayerDisconnect,
        
        // Client-bound
        // Indicating that server is shutting down
        ServerShutdown,
        
        // Client-bound
        // Notify that a list of players is already in the entered scene
        AlreadyInScene,
        
        // Client-bound
        // Notify that a player has entered the current scene
        PlayerEnterScene,
        
        // Client-bound
        // Notify that a player has left the current scene
        PlayerLeaveScene,
        
        // Client/server-bound
        // Update of realtime player values
        PlayerUpdate,

        // Client/server-bound
        // Notify that a player has died
        PlayerDeath,
        
        // Client/server-bound
        // Notify that a player has changed teams
        PlayerTeamUpdate,

        PlayerSkinUpdate,

        // Client-bound
        // Notify that the gameplay settings have updated
        GameSettingsUpdated,
        
        // Client/server-bound
        // Notify that the player spawned their Dreamshield
        DreamshieldSpawn,
        
        // Client/server-bound
        // Notify that the player despawned their Dreamshield
        DreamshieldDespawn,
        
        // Client/server-bound
        // Notify that the player's Dreamshield updated
        DreamshieldUpdate,
    }
}