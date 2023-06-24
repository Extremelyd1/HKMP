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
            var baldurFsm = clientFsms[0];
            return SpawnBaldurGameObject(baldurFsm);
        } 
        
        if (spawningType == EntityType.VengeflyKing && spawnedType == EntityType.VengeflySummon) {
            var vengeflyFsm = clientFsms[0];
            return SpawnVengeflySummonObject(vengeflyFsm);
        }

        if (spawningType == EntityType.VengeflySummon && spawnedType == EntityType.Vengefly) {
            return SpawnVengeflyObjectFromSummon(clientObject);
        }

        return null;
    }

    private static GameObject SpawnBaldurGameObject(PlayMakerFSM fsm) {
        var setGameObjectAction = fsm.GetFirstAction<SetGameObject>("Roller");
        var spawnAction = fsm.GetFirstAction<SpawnObjectFromGlobalPool>("Fire");

        var gameObject = setGameObjectAction.gameObject.Value;

        var position = Vector3.zero;
        var euler = Vector3.up;
        if (spawnAction.spawnPoint.Value != null) {
            position = spawnAction.spawnPoint.Value.transform.position;
            if (!spawnAction.position.IsNone) {
                position += spawnAction.position.Value;
            }

            if (spawnAction.rotation.IsNone) {
                euler = spawnAction.spawnPoint.Value.transform.eulerAngles;
            } else {
                euler = spawnAction.rotation.Value;
            }
        } else {
            if (!spawnAction.position.IsNone) {
                position = spawnAction.position.Value;
            }

            if (!spawnAction.rotation.IsNone) {
                euler = spawnAction.rotation.Value;
            }
        }

        var spawnedObject = gameObject.Spawn(position, Quaternion.Euler(euler));
        spawnAction.storeObject.Value = spawnedObject;

        return spawnedObject;
    }

    private static GameObject SpawnVengeflySummonObject(PlayMakerFSM fsm) {
        var action = fsm.GetFirstAction<CreateObject>("Summon");

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
        action.storeObject.Value = createdObject;

        return createdObject;
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
}
