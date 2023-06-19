using System;
using Hkmp.Networking.Client;
using UnityEngine;

namespace Hkmp.Game.Client.Entity.Component; 

/// <summary>
/// Factory class that instantiates <see cref="EntityComponent"/> by type and additional parameters.
/// </summary>
internal static class ComponentFactory {
    /// <summary>
    /// Instantiate an <see cref="EntityComponent"/> by their type.
    /// </summary>
    /// <param name="type">The type of the component.</param>
    /// <param name="netClient">The net client for passing to the constructor of the component.</param>
    /// <param name="entityId">The entity ID for passing to the constructor of the component.</param>
    /// <param name="objects">The host and client objects for passing to the constructor of the component.</param>
    /// <returns>The instantiated entity component.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the type is not one that can be instantiated
    /// here.</exception>
    public static EntityComponent InstantiateByType(
        EntityComponentType type,
        NetClient netClient,
        byte entityId,
        HostClientPair<GameObject> objects
    ) {
        Rigidbody2D rigidBody;
        
        switch (type) {
            case EntityComponentType.Rotation:
                return new RotationComponent(netClient, entityId, objects);
            case EntityComponentType.Velocity:
                rigidBody = objects.Host.GetComponent<Rigidbody2D>();
                return new VelocityComponent(netClient, entityId, objects, rigidBody);
            case EntityComponentType.GravityScale:
                rigidBody = objects.Host.GetComponent<Rigidbody2D>();
                return new GravityScaleComponent(netClient, entityId, objects, rigidBody);
            case EntityComponentType.ZPosition:
                return new ZPositionComponent(netClient, entityId, objects);
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, $"Could not instantiate entity component for type: {type}");
        }
    }
}
