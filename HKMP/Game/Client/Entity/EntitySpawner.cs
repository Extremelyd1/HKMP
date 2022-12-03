using System.Collections.Generic;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
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
    /// <param name="clientFsms">The list of client FSMs to spawn the game object.</param>
    /// <returns>The game object for the spawned entity.</returns>
    public static GameObject SpawnEntityGameObject(
        EntityType spawningType, 
        EntityType spawnedType,
        List<PlayMakerFSM> clientFsms
    ) {
        Logger.Info($"Trying to spawn entity game object for: {spawningType}, {spawnedType}");
        
        if (spawningType == EntityType.ElderBaldur && spawnedType == EntityType.Baldur) {
            return SpawnBaldurGameObject(clientFsms);
        }

        return null;
    }

    private static GameObject SpawnBaldurGameObject(List<PlayMakerFSM> clientFsms) {
        var fsm = clientFsms[0];

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
    
}
