namespace HKMP.Networking.Packet {
    public enum PacketId {
        // Server-bound
        // Initial hello, sent when the player first connects
        HelloServer = 1,
        
        // Client/server-bound
        // Notify that the client is still alive
        HeartBeat,
        
        // Server-bound
        // Indicating that client is disconnecting
        PlayerDisconnect,
        
        // Client-bound
        // Indicating that server is shutting down
        ServerShutdown,
        
        // Server-bound
        // Notify that the client has changed scenes
        PlayerChangeScene,
        
        // Client-bound
        // Notify that a player has entered the current scene
        PlayerEnterScene,
        
        // Client-bound
        // Notify that a player has left the current scene
        PlayerLeaveScene,
        
        // Client/server-bound
        // Update of player position
        PlayerPositionUpdate,
        
        // Client/server-bound
        // Update of player scale (mostly for flipping knight textures)
        PlayerScaleUpdate,
        
        // Client/server-bound
        // Update of the player's map location
        PlayerMapUpdate,
        
        // Client/server-bound
        // Update of player animation
        PlayerAnimationUpdate,
        
        // Client/server-bound
        // Notify that a player has died
        PlayerDeath,
        
        // Client-bound
        // Notify that the gameplay settings have updated
        GameSettingsUpdated,
        
        // Client/server-bound
        // Notify that the player spawned their Dreamshield
        DreamshieldSpawn,
        
        // Client/server-bound
        // Notify that the player despawned their Dreamshield
        DreamshieldDespawn,
    }
}