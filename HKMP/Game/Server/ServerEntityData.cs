using System.Collections.Generic;
using Hkmp.Game.Client.Entity;
using Hkmp.Math;
using Hkmp.Networking.Packet.Data;
using JetBrains.Annotations;

namespace Hkmp.Game.Server;

/// <summary>
/// Class containing all the relevant data managed by the server about an entity.
/// </summary>
internal class ServerEntityData {
    /// <summary>
    /// Whether this entity spawned while in a scene already.
    /// </summary>
    public bool Spawned { get; set; }
    /// <summary>
    /// The type of the entity that spawned the new entity.
    /// </summary>
    public EntityType SpawningType { get; set; }
    /// <summary>
    /// The type of the entity that was spawned.
    /// </summary>
    public EntityType SpawnedType { get; set; }

    /// <summary>
    /// The last position of the entity.
    /// </summary>
    [CanBeNull]
    public Vector2 Position { get; set; }
    /// <summary>
    /// The last scale of the entity.
    /// </summary>
    public bool? Scale { get; set; }
    /// <summary>
    /// The ID of the last played animation.
    /// </summary>
    public byte? AnimationId { get; set; }
    /// <summary>
    /// The wrap mode of the last played animation.
    /// </summary>
    public byte AnimationWrapMode { get; set; }
    /// <summary>
    /// Whether the entity is active.
    /// </summary>
    public bool? IsActive { get; set; }
    
    /// <summary>
    /// Generic data associated with this entity.
    /// </summary>
    public List<EntityNetworkData> GenericData { get; }
    
    /// <summary>
    /// Host FSM data to keep track of for transferring scene host.
    /// </summary>
    public Dictionary<byte, EntityHostFsmData> HostFsmData { get; }

    public ServerEntityData() {
        GenericData = new List<EntityNetworkData>();
        HostFsmData = new Dictionary<byte, EntityHostFsmData>();
    }
}
