using System;
using System.Collections.Generic;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using Modding;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;
using Object = UnityEngine.Object;

namespace Hkmp.Game.Client.Entity; 

/// <summary>
/// Static class that has implementations for spawning entities that are usually spawned by other entities in-game.
/// </summary>
internal static class EntitySpawner {
    /// <summary>
    /// Prefab for the Vengefly that can be summoned from a jar in The Collector boss fight.
    /// </summary>
    private static GameObject _collectorVengeflyPrefab;
    /// <summary>
    /// Prefab for the Aspid Hunter that can be summoned from a jar in The Collector boss fight.
    /// </summary>
    private static GameObject _collectorAspidPrefab;
    /// <summary>
    /// Prefab for the Baldur that can be summoned from a jar in The Collector boss fight.
    /// </summary>
    private static GameObject _collectorBaldurPrefab;

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

        if (spawningType == EntityType.TheCollector && spawnedType == EntityType.CollectorJar) {
            return SpawnCollectorJarObject(clientFsms[0]);
        }

        if (spawningType == EntityType.CollectorJar) {
            return SpawnCollectorJarContents(clientObject, spawnedType);
        }

        if (spawningType == EntityType.DungDefender) {
            return SpawnDungBallObject(clientFsms[0], spawnedType);
        }

        if (spawningType == EntityType.Nosk && spawnedType == EntityType.NoskBlob) {
            return SpawnNoskBlobObject(clientFsms[0]);
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
    
    private static GameObject SpawnFromFlingGlobalPoolTime(
        FlingObjectsFromGlobalPoolTime action, 
        GameObject gameObject
    ) {
        var position = Vector3.zero;
        var zero = Vector3.zero;
        if (action.spawnPoint.Value != null) {
            position = action.spawnPoint.Value.transform.position;
            if (!action.position.IsNone) {
                position += action.position.Value;
            }
        } else {
            if (!action.position.IsNone) {
                position = action.position.Value;
            }
        }

        var spawnedObject = gameObject.Spawn(position, Quaternion.Euler(zero));
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
    
    private static GameObject SpawnCollectorJarObject(PlayMakerFSM fsm) {
        var setContents = fsm.GetFirstAction<SetSpawnJarContents>("Buzzer");
        _collectorVengeflyPrefab = setContents.enemyPrefab.Value;
        setContents = fsm.GetFirstAction<SetSpawnJarContents>("Spitter");
        _collectorAspidPrefab = setContents.enemyPrefab.Value;
        setContents = fsm.GetFirstAction<SetSpawnJarContents>("Roller");
        _collectorBaldurPrefab = setContents.enemyPrefab.Value;
        
        var spawnAction = fsm.GetFirstAction<SpawnObjectFromGlobalPool>("Spawn");
        var gameObject = spawnAction.gameObject.Value;

        return SpawnFromGlobalPool(spawnAction, gameObject);
    }

    private static GameObject SpawnCollectorJarContents(GameObject spawningObject, EntityType spawnedType) {
        var spawnJarControl = spawningObject.GetComponent<SpawnJarControl>();
        if (spawnJarControl == null) {
            Logger.Error("Could not find SpawnJarControl behaviour on spawning object");
            return null;
        }

        GameObject gameObject;
        int health;
        var position = spawnJarControl.transform.position;
        
        switch (spawnedType) {
            case EntityType.Vengefly:
                gameObject = _collectorVengeflyPrefab.Spawn(position);
                health = 8;
                break;
            case EntityType.AspidHunter:
                gameObject = _collectorAspidPrefab.Spawn(position);
                health = 15;
                break;
            case EntityType.Baldur:
                gameObject = _collectorBaldurPrefab.Spawn(position);
                health = 15;
                break;
            default:
                Logger.Error($"Could not spawn object from collector jar: {spawnedType}");
                return null;
        }

        var healthManager = gameObject.GetComponent<HealthManager>();
        if (healthManager != null) {
            healthManager.hp = health;
        }

        gameObject.tag = "Boss";

        return gameObject;
    }

    private static GameObject SpawnDungBallObject(PlayMakerFSM fsm, EntityType spawnedType) {
        SpawnObjectFromGlobalPool action;
        GameObject gameObject;
        
        if (spawnedType == EntityType.LargeDungBall) {
            action = fsm.GetFirstAction<SpawnObjectFromGlobalPool>("Throw 1");
            gameObject = action.gameObject.Value;
        } else if (spawnedType == EntityType.SmallDungBall) {
            action = fsm.GetFirstAction<SpawnObjectFromGlobalPool>("Erupt Out");
            gameObject = action.gameObject.Value;
        } else {
            throw new InvalidOperationException($"Could not spawn spawned type from Dung Defender: {spawnedType}");
        }

        return SpawnFromGlobalPool(action, gameObject);
    }
    
    private static GameObject SpawnNoskBlobObject(PlayMakerFSM fsm) {
        var action = fsm.GetFirstAction<FlingObjectsFromGlobalPoolTime>("Roof Drop");
        var gameObject = action.gameObject.Value;

        return SpawnFromFlingGlobalPoolTime(action, gameObject);
    }
}
