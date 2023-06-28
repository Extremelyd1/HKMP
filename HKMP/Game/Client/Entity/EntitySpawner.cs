using System.Collections.Generic;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using Modding;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Entity; 

/// <summary>
/// Static class that has implementations for spawning entities that are usually spawned by other entities in-game.
/// </summary>
internal static class EntitySpawner {
    /// <summary>
    /// Spawn the game object for an entity with the given type that is spawned from the other given type.
    /// </summary>
    /// <param name="spawningType">The type of the entity that spawns the new entity.</param>
    /// <param name="spawnedType">The type of the spawned entity.</param>
    /// <param name="clientObject">The client game object from the spawning entity.</param>
    /// <param name="clientFsms">The list of client FSMs from the spawning entity.</param>
    /// <returns>The game object for the spawned entity.</returns>
    public static GameObject SpawnEntityGameObject(
        EntityType spawningType, 
        EntityType spawnedType,
        GameObject clientObject,
        List<PlayMakerFSM> clientFsms
    ) {
        Logger.Info($"Trying to spawn entity game object for: {spawningType}, {spawnedType}");
        
        if (spawningType == EntityType.ElderBaldur && spawnedType == EntityType.Baldur) {
            return SpawnBaldurGameObject(clientFsms[0]);
        } 
        
        if (spawningType == EntityType.VengeflyKing && spawnedType == EntityType.VengeflySummon) {
            return SpawnVengeflySummonObject(clientFsms[0]);
        }

        if (spawningType == EntityType.VengeflySummon && spawnedType == EntityType.Vengefly) {
            return SpawnVengeflyObjectFromSummon(clientObject);
        }

        if (spawningType == EntityType.Sporg && spawnedType == EntityType.SporgSpore) {
            return SpawnSporgSpore(clientFsms[0]);
        }

        if (spawningType == EntityType.OomaCorpse && spawnedType == EntityType.OomaCore) {
            return SpawnOomaCoreObject(clientFsms[0]);
        }

        if (spawnedType == EntityType.SoulOrb) {
            if (spawningType == EntityType.SoulTwister) {
                return SpawnSoulTwisterOrbObject(clientFsms[0]);
            }
            if (spawningType == EntityType.SoulWarrior) {
                return SpawnSoulWarriorOrbObject(clientFsms[0]);
            }
            if (spawningType == EntityType.SoulMaster) {
                return SpawnSoulMasterOrbObject(clientFsms[0]);
            }
            if (spawningType == EntityType.SoulMasterOrbSpinner) {
                return SpawnOrbSpinnerOrbObject(clientFsms[2]);
            }

            if (spawningType == EntityType.SoulMasterPhase2) {
                return SpawnSoulMaster2OrbObject(clientFsms[0]);
            }
        }

        return null;
    }

    private static GameObject SpawnFromCreateObject(CreateObject action) {
        var gameObject = action.gameObject.Value;

        var position = Vector3.zero;
        var euler = Vector3.zero;
        if (action.spawnPoint.Value != null) {
            position = action.spawnPoint.Value.transform.position;

            if (!action.position.IsNone) {
                position += action.position.Value;
            }

            if (!action.rotation.IsNone) {
                euler = action.rotation.Value;
            } else {
                euler = action.spawnPoint.Value.transform.eulerAngles;
            }
        } else {
            if (!action.position.IsNone) {
                position = action.position.Value;
            }

            if (!action.rotation.IsNone) {
                euler = action.rotation.Value;
            }
        }

        var createdObject = Object.Instantiate(gameObject, position, Quaternion.Euler(euler));

        return createdObject;
    }

    private static GameObject SpawnFromGlobalPool(SpawnObjectFromGlobalPool action, GameObject gameObject) {
        var position = Vector3.zero;
        var euler = Vector3.up;
        if (action.spawnPoint.Value != null) {
            position = action.spawnPoint.Value.transform.position;
            if (!action.position.IsNone) {
                position += action.position.Value;
            }

            if (action.rotation.IsNone) {
                euler = action.spawnPoint.Value.transform.eulerAngles;
            } else {
                euler = action.rotation.Value;
            }
        } else {
            if (!action.position.IsNone) {
                position = action.position.Value;
            }

            if (!action.rotation.IsNone) {
                euler = action.rotation.Value;
            }
        }

        var spawnedObject = gameObject.Spawn(position, Quaternion.Euler(euler));

        return spawnedObject;
    }

    private static GameObject SpawnBaldurGameObject(PlayMakerFSM fsm) {
        var setGameObjectAction = fsm.GetFirstAction<SetGameObject>("Roller");
        var spawnAction = fsm.GetFirstAction<SpawnObjectFromGlobalPool>("Fire");

        var gameObject = setGameObjectAction.gameObject.Value;

        return SpawnFromGlobalPool(spawnAction, gameObject);
    }

    private static GameObject SpawnVengeflySummonObject(PlayMakerFSM fsm) {
        var action = fsm.GetFirstAction<CreateObject>("Summon");

        return SpawnFromCreateObject(action);
    }

    private static GameObject SpawnVengeflyObjectFromSummon(GameObject spawningObject) {
        var enemySpawner = spawningObject.GetComponent<EnemySpawner>();
        if (enemySpawner == null) {
            Logger.Error("Could not create Vengefly object from summon because EnemySpawner component is null");
            return null;
        }

        // We check if the object has been created already in the Awake() of the EnemySpawner
        // If not, we have to instantiate a new object and return that instead
        var spawnedEnemy = ReflectionHelper.GetField<EnemySpawner, GameObject>(enemySpawner, "spawnedEnemy");
        if (spawnedEnemy == null) {
            spawnedEnemy = Object.Instantiate(enemySpawner.enemyPrefab);
        }

        spawnedEnemy.SetActive(true);
        
        return spawnedEnemy;
    }
    
    private static GameObject SpawnOomaCoreObject(PlayMakerFSM fsm) {
        var action = fsm.GetAction<CreateObject>("Explode", 3);

        return SpawnFromCreateObject(action);
    }

    private static GameObject SpawnSporgSpore(PlayMakerFSM fsm) {
        var spawnAction = fsm.GetFirstAction<SpawnObjectFromGlobalPool>("Fire");
        var gameObject = spawnAction.gameObject.Value;

        return SpawnFromGlobalPool(spawnAction, gameObject);
    }
    
    private static GameObject SpawnSoulTwisterOrbObject(PlayMakerFSM fsm) {
        var spawnAction = fsm.GetFirstAction<SpawnObjectFromGlobalPool>("Fire");
        var gameObject = spawnAction.gameObject.Value;

        return SpawnFromGlobalPool(spawnAction, gameObject);
    }
    
    private static GameObject SpawnSoulWarriorOrbObject(PlayMakerFSM fsm) {
        var spawnAction = fsm.GetFirstAction<SpawnObjectFromGlobalPool>("Shoot");
        var gameObject = spawnAction.gameObject.Value;

        return SpawnFromGlobalPool(spawnAction, gameObject);
    }
    
    private static GameObject SpawnSoulMasterOrbObject(PlayMakerFSM fsm) {
        var spawnAction = fsm.GetFirstAction<SpawnObjectFromGlobalPool>("Shot");
        var gameObject = spawnAction.gameObject.Value;

        return SpawnFromGlobalPool(spawnAction, gameObject);
    }
    
    private static GameObject SpawnOrbSpinnerOrbObject(PlayMakerFSM fsm) {
        var spawnAction = fsm.GetFirstAction<SpawnObjectFromGlobalPool>("Spawn");
        var gameObject = spawnAction.gameObject.Value;

        return SpawnFromGlobalPool(spawnAction, gameObject);
    }
    
    private static GameObject SpawnSoulMaster2OrbObject(PlayMakerFSM fsm) {
        var spawnAction = fsm.GetFirstAction<SpawnObjectFromGlobalPool>("Spawn Fireball");
        var gameObject = spawnAction.gameObject.Value;

        return SpawnFromGlobalPool(spawnAction, gameObject);
    }
}
