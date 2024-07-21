using System;
using System.Collections.Generic;
using System.Linq;
using Hkmp.Game.Client.Entity.Action;
using Hkmp.Game.Client.Entity.Component;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using FindGameObject = On.HutongGames.PlayMaker.Actions.FindGameObject;
using Logger = Hkmp.Logging.Logger;
using Object = UnityEngine.Object;

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
    private readonly Dictionary<ushort, Entity> _entities;

    /// <summary>
    /// Whether the scene host is determined for this scene locally.
    /// </summary>
    public bool IsSceneHostDetermined { get; private set; }

    /// <summary>
    /// Whether the client user is the scene host.
    /// </summary>
    public bool IsSceneHost { get; private set; }

    /// <summary>
    /// Queue of entity updates that have not been applied yet because of a missing entity.
    /// Usually this occurs because the entities are loaded later than the updates are received when the local player
    /// enters a new scene.
    /// </summary>
    private readonly Queue<BaseEntityUpdate> _receivedUpdates;

    public EntityManager(NetClient netClient) {
        _netClient = netClient;
        _entities = new Dictionary<ushort, Entity>();
        _receivedUpdates = new Queue<BaseEntityUpdate>();
        
        EntityProcessor.Initialize(_entities, netClient);
        FsmActionHooks.Initialize();

        EntityFsmActions.EntitySpawnEvent += OnGameObjectSpawned;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
        
        FindGameObject.Find += OnFindGameObject;
    }

    /// <summary>
    /// Initializes the entity manager if we are the scene host.
    /// </summary>
    public void InitializeSceneHost() {
        Logger.Info("We are scene host, releasing control of all registered entities");

        IsSceneHost = true;

        foreach (var entity in _entities.Values) {
            entity.InitializeHost();
        }
        
        IsSceneHostDetermined = true;
        
        CheckReceivedUpdates();
    }

    /// <summary>
    /// Initializes the entity manager if we are a scene client.
    /// </summary>
    public void InitializeSceneClient() {
        Logger.Info("We are scene client, taking control of all registered entities");

        IsSceneHost = false;

        foreach (var entity in _entities.Values) {
            entity.InitializeClient();
        }
        
        IsSceneHostDetermined = true;
        
        CheckReceivedUpdates();
    }

    /// <summary>
    /// Updates the entity manager if we become the scene host.
    /// </summary>
    public void BecomeSceneHost() {
        Logger.Info("Becoming scene host");

        IsSceneHost = true;

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
    public void SpawnEntity(ushort id, EntityType spawningType, EntityType spawnedType) {
        Logger.Info($"Trying to spawn entity with ID {id} with types: {spawningType}, {spawnedType}");

        // If an entity with the new ID already exists, we return
        if (_entities.ContainsKey(id)) {
            Logger.Info($"  Entity with ID {id} already exists, assuming it has been spawned by action");
            return;
        }

        // Find an entity that has the same type as the spawning type. Doesn't matter if it is the correct instance,
        // because the FSMs and components will be identical
        var spawningEntity = _entities.Values.FirstOrDefault(
            e => e.Type == spawningType
        );

        if (spawningEntity == null) {
            Logger.Warn("Could not find entity with same type for spawning");
            return;
        }

        var spawnedObject = EntitySpawner.SpawnEntityGameObject(
            spawningType,
            spawnedType,
            spawningEntity.Object.Client,
            spawningEntity.GetClientFsms()
        );

        var processor = new EntityProcessor {
            GameObject = spawnedObject,
            IsSceneHost = IsSceneHost,
            IsSceneHostDetermined = IsSceneHostDetermined,
            LateLoad = true,
            SpawnedId = id
        }.Process();

        if (!processor.Success) {
            Logger.Warn($"Could not process game object of spawned entity: {spawnedObject.name}");
        }
    }
    
    /// <summary>
    /// Method for handling received entity updates.
    /// </summary>
    /// <param name="entityUpdate">The entity update to handle.</param>
    /// <param name="alreadyInSceneUpdate">Whether this is the update from the already in scene packet.</param>
    public bool HandleEntityUpdate(EntityUpdate entityUpdate, bool alreadyInSceneUpdate = false) {
        if (IsSceneHost) {
            return true;
        }

        if (!_entities.TryGetValue(entityUpdate.Id, out var entity) || !IsSceneHostDetermined) {
            if (IsSceneHostDetermined) {
                Logger.Debug($"Could not find entity ({entityUpdate.Id}) to apply update for; storing update for now");
            } else {
                Logger.Debug("Scene host is not determined yet to apply update; storing update for now");
            }

            _receivedUpdates.Enqueue(entityUpdate);
            
            return false;
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

        return true;
    }

    /// <summary>
    /// Method for handling received reliable entity updates.
    /// </summary>
    /// <param name="entityUpdate">The reliable entity update to handle.</param>
    /// <param name="alreadyInSceneUpdate">Whether this is the update from the already in scene packet.</param>
    public bool HandleReliableEntityUpdate(ReliableEntityUpdate entityUpdate, bool alreadyInSceneUpdate = false) {
        if (IsSceneHost) {
            return true;
        }
        
        if (!_entities.TryGetValue(entityUpdate.Id, out var entity) || !IsSceneHostDetermined) {
            if (IsSceneHostDetermined) {
                Logger.Debug($"Could not find entity ({entityUpdate.Id}) to apply update for; storing update for now");
            } else {
                Logger.Debug("Scene host is not determined yet to apply update; storing update for now");
            }
            
            _receivedUpdates.Enqueue(entityUpdate);

            return false;
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

        return true;
    }

    /// <summary>
    /// Callback method for when a game object is spawned from an existing entity.
    /// </summary>
    /// <param name="details">The entity spawn details containing how the entity was spawned.</param>
    /// <returns>Whether an entity was registered from this spawn.</returns>
    private bool OnGameObjectSpawned(EntitySpawnDetails details) {
        if (_entities.Values.Any(existingEntity => existingEntity.Object.Host == details.GameObject)) {
            Logger.Debug("Spawned object was already a registered entity");
            return true;
        }

        var processor = new EntityProcessor {
            GameObject = details.GameObject,
            IsSceneHost = IsSceneHost,
            IsSceneHostDetermined = IsSceneHostDetermined,
            LateLoad = true
        }.Process();

        if (!processor.Success) {
            return false;
        }

        if (!IsSceneHost) {
            Logger.Warn("Game object was spawned while not scene host, this shouldn't happen");
            return false;
        }
        
        string spawningObjectName;
        EntityType spawningType;
        var topLevelEntity = processor.Entities[0];

        if (details.Type == EntitySpawnType.FsmAction) {
            spawningObjectName = details.Action.Fsm.GameObject.name;
            if (EntityRegistry.TryGetEntry(details.Action.Fsm.GameObject, out var entry)) {
                spawningType = entry.Type;
            } else {
                Logger.Warn("Could not find registry entry for spawning type of object");
                return false;
            }
        } else if (details.Type == EntitySpawnType.EnemySpawnerComponent) {
            spawningObjectName = "Vengefly Summon";
            spawningType = EntityType.VengeflySummon;
        } else if (details.Type == EntitySpawnType.SpawnJarComponent) {
            spawningObjectName = "Spawn Jar";
            spawningType = EntityType.CollectorJar;
        } else {
            Logger.Error($"Invalid EntitySpawnDetails type: {details.Type}");
            return false;
        }

        Logger.Info(
            $"Notifying server of entity ({spawningObjectName}, {spawningType}) spawning entity ({details.GameObject.name}, {topLevelEntity.Type}) with ID {topLevelEntity.Id}");
        _netClient.UpdateManager.SetEntitySpawn(
            topLevelEntity.Id, 
            spawningType, 
            topLevelEntity.Type
        );

        return true;
    }

    /// <summary>
    /// Check to see if there are received un-applied entity updates.
    /// </summary>
    private void CheckReceivedUpdates() {
        while (_receivedUpdates.Count != 0) {
            var update = _receivedUpdates.Peek();
            
            if (_entities.TryGetValue(update.Id, out _)) {
                Logger.Debug("Found un-applied entity update, applying now");

                bool handled;
                if (update is EntityUpdate entityUpdate) {
                    handled = HandleEntityUpdate(entityUpdate);
                } else if (update is ReliableEntityUpdate reliableEntityUpdate) {
                    handled = HandleReliableEntityUpdate(reliableEntityUpdate);
                } else {
                    continue;
                }

                if (handled) {
                    _receivedUpdates.Dequeue();
                }
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
        Logger.Info("Scene changed, clearing registered entities");
            
        foreach (var entity in _entities.Values) {
            entity.Destroy();
        }

        _entities.Clear();
        
        MusicComponent.ClearInstance();

        if (!_netClient.IsConnected) {
            return;
        }

        IsSceneHostDetermined = false;

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
        // Find all EnemyDeathEffects components
        var objectsToCheck = Object.FindObjectsOfType<EnemyDeathEffects>()
            // Filter out EnemyDeathEffects components not in the current scene
            .Where(e => e.gameObject.scene == scene)
            // Project each death effect to their GameObject and the corpse of the pre-instantiated EnemyDeathEffects
            // component
            .SelectMany(enemyDeathEffects => {
                try {
                    enemyDeathEffects.PreInstantiate();
                } catch (Exception) {
                    // If we get an exception it might not be possible to pre-instantiate the enemy death effects
                    // This can happen when the object uses a PersonalObjectPool, which can't be pre-instantiated
                    // this early, so we return only the original gameobject
                    return new[] { enemyDeathEffects.gameObject };
                }

                var corpse = ReflectionHelper.GetField<EnemyDeathEffects, GameObject>(
                    enemyDeathEffects, 
                    "corpse"
                );

                return new[] { enemyDeathEffects.gameObject, corpse };
            })
            // Concatenate all GameObjects for PlayMakerFSM components in the current scene, and check whether it is the
            // FSM for a Colosseum Cage, in which case we pre-instantiate the enemy inside and concatenate it as well
            .Concat(Object.FindObjectsOfType<PlayMakerFSM>(true)
                .Where(fsm => fsm.gameObject.scene == scene)
                .SelectMany(fsm => {
                    if (!fsm.name.StartsWith("Colosseum Cage Small") &&
                        !fsm.name.StartsWith("Colosseum Cage Large") &&
                        !fsm.name.StartsWith("Colosseum Cage Zote")) {
                        return new[] { fsm.gameObject };
                    }
                
                    if (!fsm.Fsm.Name.Equals("Spawn") &&
                        !fsm.Fsm.Name.Equals("Control")
                    ) {
                        return new[] { fsm.gameObject };
                    }
                    
                    var createAction = fsm.GetFirstAction<CreateObject>("Init");
                    EntityFsmActions.ApplyNetworkDataFromAction(null, createAction);

                    createAction.Enabled = false;

                    var createdObject = createAction.storeObject.Value;
                    if (createdObject == null) {
                        return new[] { fsm.gameObject };
                    }

                    var fsmTransform = fsm.gameObject.transform;
                    
                    createdObject.transform.position = fsmTransform.position;
                    createdObject.transform.rotation = Quaternion.Euler(fsmTransform.eulerAngles);
                    createdObject.SetActive(false);

                    return new[] { fsm.gameObject, createdObject };
                })
            )
            // Project each GameObject into its children including itself
            .SelectMany(obj => obj == null ? Array.Empty<GameObject>() : obj.GetChildren().Prepend(obj))
            // Concatenate all GameObjects for Climber components (Tiktiks)
            .Concat(Object.FindObjectsOfType<Climber>(true).Select(climber => climber.gameObject))
            // Concatenate all GameObjects for Walker components (Amblooms)
            .Concat(Object.FindObjectsOfType<Walker>(true).Select(walker => walker.gameObject))
            // Concatenate all GameObjects for BigCentipede components (Garpedes)
            .Concat(Object.FindObjectsOfType<BigCentipede>(true).Select(centipede => centipede.gameObject))
            // Concatenate all GameObjects for CameraLockArea components
            .Concat(Object.FindObjectsOfType<CameraLockArea>(true).Select(cameraLockArea => cameraLockArea.gameObject))
            // Filter out GameObjects not in the current scene
            .Where(obj => obj.scene == scene)
            .Distinct();

        foreach (var obj in objectsToCheck) {
            new EntityProcessor {
                GameObject = obj,
                IsSceneHost = IsSceneHost,
                IsSceneHostDetermined = IsSceneHostDetermined,
                LateLoad = lateLoad
            }.Process();
        }
    }

    /// <summary>
    /// Callback method for when the find method of FindGameObject is called. This is to look for objects that are
    /// normally not found by the action due to our entity system making certain objects inactive. If we notice that
    /// the find failed, but the name to look for was one of our host objects in the entity system, we set that object
    /// instead.
    /// </summary>
    private void OnFindGameObject(FindGameObject.orig_Find orig, HutongGames.PlayMaker.Actions.FindGameObject self) {
        orig(self);

        // If the object was found after the method executed, we skip
        if (self.store.Value != null) {
            return;
        }
        
        Logger.Debug($"OnFindGameObject, find failed: looking for '{self.objectName.Value}'");
        
        // If the object to find is tagged we skip, since this doesn't happen in our case
        if (self.withTag.Value != "Untagged") {
            return;
        }

        // Check if the name we are looking for is one of our registered entity's host objects
        foreach (var entity in _entities.Values) {
            var obj = entity.Object.Host;
            if (obj != null && obj.name == self.objectName.Value) {
                // The host object of the entity matches the name the action was looking for, so we set the variable
                self.store.Value = obj;
                
                Logger.Debug($"  Name matches host object of entity: ({entity.Id}, {entity.Type})");
                return;
            }
        }
        
        Logger.Debug("  Name did not match any entity");
    }
}
