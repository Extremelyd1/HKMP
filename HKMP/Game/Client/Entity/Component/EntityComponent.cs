using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Hkmp.Game.Client.Entity.Component;

/// <summary>
/// Class for a generalizable part of an entity that requires networking for a specific feature.
/// </summary>
internal abstract class EntityComponent {
    /// <summary>
    /// The net client for networking.
    /// </summary>
    private readonly NetClient _netClient;
    /// <summary>
    /// The ID of the entity.
    /// </summary>
    private readonly byte _entityId;

    /// <summary>
    /// Host-client pair of the game objects of the entity.
    /// </summary>
    protected readonly HostClientPair<GameObject> GameObject;

    /// <summary>
    /// Whether the entity is controlled.
    /// </summary>
    public bool IsControlled { get; set; }

    protected EntityComponent(
        NetClient netClient,
        byte entityId,
        HostClientPair<GameObject> gameObject
    ) {
        _netClient = netClient;
        _entityId = entityId;

        GameObject = gameObject;

        IsControlled = true;
    }

    /// <summary>
    /// Send the given <see cref="EntityNetworkData"/>.
    /// </summary>
    /// <param name="data">The data to send.</param>
    protected void SendData(EntityNetworkData data) {
        _netClient.UpdateManager.AddEntityData(_entityId, data);
    }

    /// <summary>
    /// Initializes the entity component when the client user is the scene host.
    /// </summary>
    public abstract void InitializeHost();
    /// <summary>
    /// Update the entity component with the given data.
    /// </summary>
    /// <param name="data">The data to update with.</param>
    public abstract void Update(EntityNetworkData data);
    /// <summary>
    /// Destroy the entity component.
    /// </summary>
    public abstract void Destroy();
}

/// <summary>
/// Enum for data types.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
internal enum EntityComponentType : byte {
    Fsm = 0,
    Death,
    Invincibility,
    Rotation,
    Collider,
    DamageHero,
    MeshRenderer,
    Velocity,
    GravityScale,
    ZPosition,
    Climber,
    EnemySpawner,
    ChildrenActivation,
    SpawnJar,
}