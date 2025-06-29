namespace Hkmp.Networking.Packet.Update;

/// <summary>
/// Enumeration of packet IDs for the update packet for client to server communication.
/// </summary>
public enum ServerUpdatePacketId {
    /// <summary>
    /// Indicates slice data from a chunk for large data transfer.
    /// </summary>
    Slice = 0,

    /// <summary>
    /// Indicates the acknowledgement for a slice from a chunk for large data transfer.
    /// </summary>
    SliceAck = 1,

    /// <summary>
    /// Indicating that a client is disconnecting.
    /// </summary>
    PlayerDisconnect = 2,

    /// <summary>
    /// Update of realtime player values.
    /// </summary>
    PlayerUpdate = 3,

    /// <summary>
    /// Update of player map position.
    /// </summary>
    PlayerMapUpdate = 4,
    
    /// <summary>
    /// Notify that an entity has spawned.
    /// </summary>
    EntitySpawn = 5,

    /// <summary>
    /// Update of realtime entity values.
    /// </summary>
    EntityUpdate = 6,
    
    /// <summary>
    /// Update of realtime reliable entity values.
    /// </summary>
    ReliableEntityUpdate = 7,

    /// <summary>
    /// Notify that the player has entered a new scene.
    /// </summary>
    PlayerEnterScene = 8,

    /// <summary>
    /// Notify that the player has left their current scene.
    /// </summary>
    PlayerLeaveScene = 9,

    /// <summary>
    /// Notify that a player has died.
    /// </summary>
    PlayerDeath = 10,

    /// <summary>
    /// Player sent chat message.
    /// </summary>
    ChatMessage = 11,
    
    /// <summary>
    /// Value in the save file has updated.
    /// </summary>
    SaveUpdate = 12,
    
    /// <summary>
    /// Server settings are updated.
    /// </summary>
    ServerSettings = 13
}
