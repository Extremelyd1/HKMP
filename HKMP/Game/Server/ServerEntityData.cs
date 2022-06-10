using System.Collections.Generic;
using Hkmp.Math;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Game.Server; 

/// <summary>
/// Class containing all the relevant data managed by the server about an entity.
/// </summary>
internal class ServerEntityData {
    /// <summary>
    /// The last position of the entity.
    /// </summary>
    public Vector2 Position { get; set; }
    /// <summary>
    /// The last scale of the entity.
    /// </summary>
    public bool Scale { get; set; }
    /// <summary>
    /// The ID of the last played animation.
    /// </summary>
    public byte? AnimationId { get; set; }
    /// <summary>
    /// The wrap mode of the last played animation.
    /// </summary>
    public byte AnimationWrapMode { get; set; }
    
    /// <summary>
    /// Generic data associated with this entity.
    /// </summary>
    public List<EntityNetworkData> GenericData { get; set; }

    public ServerEntityData() {
        GenericData = new List<EntityNetworkData>();
    }
}