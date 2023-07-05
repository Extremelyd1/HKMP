using System;
using System.Collections.Generic;
using System.Linq;
using Hkmp.Game.Client.Entity.Component;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Entity; 

/// <summary>
/// Data processing class that receives a set of parameters and processing a given game object into an entity if
/// applicable. Will recursively process children of the game object as well and report success and the list of
/// entities created.
/// </summary>
internal class EntityProcessor {
    /// <summary>
    /// Reference to the dictionary of entities from the entity manager.
    /// </summary>
    private static Dictionary<ushort, Entity> _entities;
    /// <summary>
    /// The net client used to pass onto constructed entities.
    /// </summary>
    private static NetClient _netClient;

    /// <summary>
    /// The last used entity ID.
    /// </summary>
    private static ushort _lastId;
    
    /// <summary>
    /// The game object to process.
    /// </summary>
    public GameObject GameObject { get; init; }
    /// <summary>
    /// Whether the local client is the scene host.
    /// </summary>
    public bool IsSceneHost { get; init; }
    /// <summary>
    /// Whether the processing of this entity should happen under the assumption that was a late load of the
    /// game object.
    /// </summary>
    public bool LateLoad { get; init; }
    /// <summary>
    /// Whether the game object was spawned and should have the designated ID.
    /// </summary>
    public ushort? SpawnedId { get; init; }

    /// <summary>
    /// The list of entities that were created during the processing.
    /// </summary>
    public List<Entity> Entities { get; } = new();
    /// <summary>
    /// Whether the processing of the entity was a success.
    /// </summary>
    public bool Success => Entities.Count > 0;

    /// <summary>
    /// Initialize the entity processor with a reference to the entity dict and the net client.
    /// </summary>
    /// <param name="entities">A reference to the dictionary of entities from the entity manager.</param>
    /// <param name="netClient">The net client instance to pass onto constructed entities.</param>
    public static void Initialize(Dictionary<ushort, Entity> entities, NetClient netClient) {
        _entities = entities;
        _netClient = netClient;

        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (_, _) => {
            _lastId = 0;
        };
    }

    /// <summary>
    /// Process the game object set in this instance with the parameter set in this instance.
    /// </summary>
    /// <returns>The instance of this class for convenience.</returns>
    public EntityProcessor Process() {
        Process(GameObject);
        return this;
    }

    /// <summary>
    /// Process the given game object to (potentially) become an entity. Check for child objects as well and will
    /// recursively process those.
    /// </summary>
    /// <param name="gameObject">The game object to process.</param>
    /// <param name="entries">Entity registry entries to check against for the entity or null to use all
    /// top-level entries.</param>
    /// <param name="parentClientObject">The client object of the parent entity for this entity or null if no such
    /// parent exists.</param>
    private void Process(
        GameObject gameObject, 
        IEnumerable<EntityRegistryEntry> entries = null,
        GameObject parentClientObject = null
    ) {
        EntityRegistryEntry foundEntry;

        // If the given entries are null, query the entity registry for all top-level entries
        // Otherwise only use the given entries (used for child entities)
        if (entries == null) {
            if (!EntityRegistry.TryGetEntry(gameObject, out foundEntry)) {
                return;
            }
        } else {
            if (!EntityRegistry.TryGetEntry(entries, gameObject, out foundEntry)) {
                return;
            }
        }

        ushort id;
        
        // If a spawned ID is defined we check whether an entity with the given ID already exists
        // Otherwise we find a new ID that isn't used yet
        if (SpawnedId.HasValue) {
            id = SpawnedId.Value;
            
            if (_entities.ContainsKey(id)) {
                Logger.Warn($"Tried registering entity with forced ID ({id}), but an entity with the ID already exists");
                return;
            }
        } else {
            if (_entities.Count >= ushort.MaxValue) {
                Logger.Error("Could not register entity because ID space is full!");
                return;
            }

            // First find a usable ID that is not registered to an entity already
            while (_entities.ContainsKey(_lastId)) {
                _lastId++;
            }

            id = _lastId;
            _lastId++;
        }

        // Get the array of component types for the entity or create an empty one if it is null
        var componentTypes = foundEntry.ComponentTypes ?? Array.Empty<EntityComponentType>();
        
        // Depending on whether a parent object was given we create the entity with this parent object
        Entity entity;
        if (parentClientObject == null) {
            Logger.Info($"Registering entity ({foundEntry.Type}) '{gameObject.name}' with ID '{id}'");

            entity = new Entity(
                _netClient,
                id,
                foundEntry.Type,
                gameObject,
                types: componentTypes
            );
        } else {
            Logger.Info($"Registering entity ({foundEntry.Type}) '{gameObject.name}' with ID '{id}' with parent: {parentClientObject.name}");
            
            // Find the correct child of the client object of the parent entity
            var clientObject = parentClientObject.GetChildren()
                .FirstOrDefault(c => {
                    if (Entities.Any(processedEntity => processedEntity.Object.Client == c)) {
                        return false;
                    }

                    return c.name.Contains(foundEntry.BaseObjectName);
                });
            if (clientObject == null) {
                Logger.Warn("Could not find child of client object of parent entity");
                return;
            }
            
            Logger.Debug($"Found child of client object of parent entity: {clientObject.name}, {clientObject.GetInstanceID()}");

            entity = new Entity(
                _netClient,
                id,
                foundEntry.Type,
                gameObject,
                clientObject,
                componentTypes
            );
        }

        _entities[id] = entity;

        Entities.Add(entity);

        // If this entry has child entries, we recursively check the children of the object as well
        if (foundEntry.Children != null) {
            foreach (var childObj in gameObject.GetChildren()) {
                Process(childObj, foundEntry.Children, entity.Object.Client);
            }
        }
        
        if (LateLoad) {
            if (IsSceneHost) {
                // Since this is a late load it needs to be initialized as host if we are the scene host
                entity.InitializeHost();
            } else {
                // Since this is a late load we need to update the 'active' state of the entity
                entity.UpdateIsActive(true);
            }
        }
    }
}
