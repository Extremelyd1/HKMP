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
        ushort entityId,
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
            case EntityComponentType.EnemySpawner:
                var spawnerClient = objects.Client.GetComponent<EnemySpawner>();
                var spawnerHost = objects.Host.GetComponent<EnemySpawner>();

                return new EnemySpawnerComponent(netClient, entityId, objects, new HostClientPair<EnemySpawner> {
                    Client = spawnerClient,
                    Host = spawnerHost
                });
            case EntityComponentType.ChildrenActivation:
                return new ChildrenActivationComponent(netClient, entityId, objects);
            case EntityComponentType.SpawnJar:
                var spawnJarClient = objects.Client.GetComponent<SpawnJarControl>();
                var spawnJarHost = objects.Host.GetComponent<SpawnJarControl>();
                
                return new SpawnJarComponent(netClient, entityId, objects, new HostClientPair<SpawnJarControl> {
                    Client = spawnJarClient,
                    Host = spawnJarHost
                });
            case EntityComponentType.SpriteRenderer:
                var spriteRendererClient = objects.Client.GetComponent<SpriteRenderer>();
                var spriteRendererHost = objects.Host.GetComponent<SpriteRenderer>();

                return new SpriteRendererComponent(netClient, entityId, objects, new HostClientPair<SpriteRenderer> {
                    Client = spriteRendererClient,
                    Host = spriteRendererHost
                });
            case EntityComponentType.ChallengePrompt:
                return new ChallengePromptComponent(netClient, entityId, objects);
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, $"Could not instantiate entity component for type: {type}");
        }
    }
}
