namespace HKMP.Networking.Packet {
    public enum PacketId {
        // Server-bound
        // Initial hello, sent when the player first connects
        HelloServer = 1,
        
        // Server-bound
        // Indicating that client is disconnecting
        Disconnect,
        
        // Client-bound
        // Indicating that server is shutting down
        Shutdown,
        
        // Server-bound
        // Notify that the client has changed scenes
        SceneChange,
        
        // Client-bound
        // Notify that a player has entered the current scene
        PlayerEnterScene,
        
        // Client-bound
        // Notify that a player has left the current scene
        PlayerLeaveScene,
        
        // Client and Server-bound
        // Update of player position
        PlayerPositionUpdate,
    }
}