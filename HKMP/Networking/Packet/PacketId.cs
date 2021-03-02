namespace HKMP.Networking.Packet {
    public enum PacketId {
        // Server-bound
        // Initial hello, sent when the player first connects
        HelloServer = 1,
        
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
        
        // Server-bound
        // Update of player position
        ServerPlayerPositionUpdate,
        
        // Client-bound
        // Update of player position
        ClientPlayerPositionUpdate,
        
        // Server-bound
        // Update of player scale (mostly for flipping knight textures)
        ServerPlayerScaleUpdate,
        
        // Client-bound
        // Update of player scale (mostly for flipping knight textures)
        ClientPlayerScaleUpdate,
        
        // Server-bound
        // Update of player animation
        ServerPlayerAnimationUpdate,
        
        // Client-bound
        // Update of player animation
        ClientPlayerAnimationUpdate,
        
        // Server-bound
        // Notify that a player has died
        ServerPlayerDeath,
        
        // Client-bound
        // Notify that a player has died
        ClientPlayerDeath,
        
        // Client-bound
        // Notify that the gameplay settings have updated
        GameSettingsUpdated,
    }
}