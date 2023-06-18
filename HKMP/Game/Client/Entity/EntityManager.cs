using System.Collections.Generic;
using System.Linq;
using Hkmp.Game.Client.Entity.Action;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using HutongGames.PlayMaker;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Entity;

/// <summary>
/// Manager class that handles entity creation, updating, networking and destruction.
/// </summary>
internal class EntityManager {
    /// <summary>
    /// The net client for networking.
    /// </summary>
    private readonly NetClient _netClient;

    /// <summary>
    /// Dictionary mapping entity IDs to their respective entity instances.
    /// </summary>
    private readonly Dictionary<byte, Entity> _entities;

    /// <summary>
    /// Whether the client user is the scene host.
    /// </summary>
    private bool _isSceneHost;

    /// <summary>
    /// Queue of entity updates that have not been applied yet because of a missing entity.
    /// Usually this occurs because the entities are loaded later than the updates are received when the local player
    /// enters a new scene.
    /// </summary>
    private readonly Queue<EntityUpdate> _receivedUpdates;

    public EntityManager(NetClient netClient) {
        _netClient = netClient;
        _entities = new Dictionary<byte, Entity>();
        _receivedUpdates = new Queue<EntityUpdate>();
        
        EntityProcessor.Initialize(_entities, netClient);

        EntityFsmActions.EntitySpawnEvent += OnGameObjectSpawned;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
    }

    /// <summary>
    /// Initializes the entity manager if we are the scene host.
    /// </summary>
    public void InitializeSceneHost() {
        Logger.Info("Releasing control of all registered entities");

        _isSceneHost = true;

        foreach (var entity in _entities.Values) {
            entity.InitializeHost();
        }
    }

    /// <summary>
    /// Initializes the entity manager if we are a scene client.
    /// </summary>
    public void InitializeSceneClient() {
        Logger.Info("Taking control of all registered entities");

        _isSceneHost = false;
    }

    /// <summary>
    /// Updates the entity manager if we become the scene host.
    /// </summary>
    public void BecomeSceneHost() {
        Logger.Info("Becoming scene host");

        _isSceneHost = true;

        foreach (var entity in _entities.Values) {
            entity.MakeHost();
        }
    }

    /// <summary>
    /// Spawn an entity with the given ID and type, that was spawned from the given entity type.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="spawningType">The type of the entity that spawned the new entity.</param>
    /// <param name="spawnedType">The type of the spawned entity.</param>
    public void SpawnEntity(byte id, EntityType spawningType, EntityType spawnedType) {
        Logger.Info($"Trying to spawn entity with ID {id} with types: {spawningType}, {spawnedType}");

        // If an entity with the new ID already exists, we return
        if (_entities.ContainsKey(id)) {
            Logger.Info($"  Entity with ID {id} already exists, assuming it has been spawned by action");
            return;
        }

        // Find the list of client FSMs that correspond to an entity with the given type in our current scene
        // Doesn't matter which instance of entity it is, because the FSMs will be the same
        var clientFsms = _entities.Values.FirstOrDefault(
            e => e.Type == spawningType
        )?.GetClientFsms();
        
        // If no such FSMs exist we return again, because we can't spawn the new entity
        if (clientFsms == null) {
            Logger.Warn($"Could not find entity with same type for spawning");
            return;
        }

        var gameObject = EntitySpawner.SpawnEntityGameObject(spawningType, spawnedType, clientFsms);

        var processor = new EntityProcessor {
            GameObject = gameObject,
            IsSceneHost = _isSceneHost,
            LateLoad = true,
            SpawnedId = id
        }.Process();

        if (!processor.Success) {
            Logger.Warn($"Could not process game object of spawned entity: {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Method for handling received entity updates.
    /// </summary>
    /// <param name="entityUpdate">The entity update to handle.</param>
    /// <param name="alreadyInSceneUpdate">Whether this is the update from the already in scene packet.</param>
    public void HandleEntityUpdate(EntityUpdate entityUpdate, bool alreadyInSceneUpdate = false) {
        if (_isSceneHost) {
            return;
        }

        if (!_entities.TryGetValue(entityUpdate.Id, out var entity)) {
            Logger.Debug($"Could not find entity ({entityUpdate.Id}) to apply update for; storing update for now");
            _receivedUpdates.Enqueue(entityUpdate);
            
            return;
        }
        
        if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Position)) {
            entity.UpdatePosition(entityUpdate.Position);
        }

        if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Scale)) {
            entity.UpdateScale(entityUpdate.Scale);
        }
            
        if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Animation)) {
            entity.UpdateAnimation(
                entityUpdate.AnimationId, 
                (tk2dSpriteAnimationClip.WrapMode) entityUpdate.AnimationWrapMode,
                alreadyInSceneUpdate
            );
        }

        if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Active)) {
            entity.UpdateIsActive(entityUpdate.IsActive);
        }

        if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Data)) {
            entity.UpdateData(entityUpdate.GenericData);
        }

        if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.HostFsm)) {
            entity.UpdateHostFsmData(entityUpdate.HostFsmData);
        }
    }

    /// <summary>
    /// Callback method for when a game object is spawned from an existing entity.
    /// </summary>
    /// <param name="action">The action from which the game object was spawned.</param>
    /// <param name="gameObject">The game object that was spawned.</param>
    private void OnGameObjectSpawned(FsmStateAction action, GameObject gameObject) {
        if (_entities.Values.Any(existingEntity => existingEntity.Object.Host == gameObject)) {
            Logger.Debug("Spawned object was already a registered entity");
            return;
        }

        var processor = new EntityProcessor {
            GameObject = gameObject,
            IsSceneHost = _isSceneHost,
            LateLoad = true
        }.Process();

        if (!processor.Success) {
            return;
        }

        if (!_isSceneHost) {
            Logger.Warn("Game object was spawned while not scene host, this shouldn't happen");
            return;
        }
        
        // Since an entity was created and we are the scene host, we need to notify the server
        var spawningObjectName = action.Fsm.GameObject.name;
        if (EntityRegistry.TryGetEntry(action.Fsm.GameObject, out var entry)) {
            var topLevelEntity = processor.Entities[0];
            
            Logger.Info(
                $"Notifying server of entity ({spawningObjectName}, {entry.Type}) spawning entity ({gameObject.name}, {topLevelEntity.Type}) with ID {topLevelEntity.Id}");
            _netClient.UpdateManager.SetEntitySpawn(
                topLevelEntity.Id, 
                entry.Type, 
                topLevelEntity.Type
            );
        }
    }

    /// <summary>
    /// Check to see if there are received un-applied entity updates.
    /// </summary>
    private void CheckReceivedUpdates() {
        while (_receivedUpdates.Count != 0) {
            var update = _receivedUpdates.Dequeue();
            
            if (_entities.TryGetValue(update.Id, out _)) {
                Logger.Debug("Found un-applied entity update, applying now");

                HandleEntityUpdate(update);
            }
        }
    }

    /// <summary>
    /// Callback method for when the scene changes. Will clear existing entities and start checking for
    /// new entities.
    /// </summary>
    /// <param name="oldScene">The old scene.</param>
    /// <param name="newScene">The new scene.</param>
    private void OnSceneChanged(Scene oldScene, Scene newScene) {
        Logger.Info($"Scene changed, clearing registered entities");
            
        foreach (var entity in _entities.Values) {
            entity.Destroy();
        }

        _entities.Clear();

        if (!_netClient.IsConnected) {
            return;
        }

        FindEntitiesInScene(newScene, false);
        
        // Since we have tried finding entities in the scene, we also check whether there are un-applied updates for
        // those entities
        CheckReceivedUpdates();
    }

    /// <summary>
    /// Callback method for when a scene is loaded.
    /// </summary>
    /// <param name="scene">The scene that is loaded.</param>
    /// <param name="mode">The load scene mode.</param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        var currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        // If this scene is a boss or boss-defeated scene it starts with the same name, so we skip all other
        // loaded scenes
        if (!scene.name.StartsWith(currentSceneName)) {
            return;
        }

        Logger.Info($"Additional scene loaded ({scene.name}), looking for entities");

        FindEntitiesInScene(scene, true);

        // Since we have tried finding entities in the scene, we also check whether there are un-applied updates for
        // those entities
        CheckReceivedUpdates();
    }

    /// <summary>
    /// Find entities to register in the given scene.
    /// </summary>
    /// <param name="scene">The scene to find entities in.</param>
    /// <param name="lateLoad">Whether this scene was loaded late.</param>
    private void FindEntitiesInScene(Scene scene, bool lateLoad) {
        // Find all PlayMakerFSM components
        // Filter out FSMs with GameObjects not in the current scene
        // Project each FSM to their GameObject
        // Project each GameObject into its children including itself
        // Concatenate all GameObjects for Climber components
        // Filter out GameObjects not in the current scene
        var objectsToCheck = Object.FindObjectsOfType<PlayMakerFSM>()
            .Where(fsm => fsm.gameObject.scene == scene)
            .Select(fsm => fsm.gameObject)
            .SelectMany(obj => obj.GetChildren().Prepend(obj))
            .Concat(Object.FindObjectsOfType<Climber>().Select(climber => climber.gameObject))
            .Where(obj => obj.scene == scene)
            .Distinct();

        foreach (var obj in objectsToCheck) {
            new EntityProcessor {
                GameObject = obj,
                IsSceneHost = _isSceneHost,
                LateLoad = lateLoad
            }.Process();
        }
    }
}