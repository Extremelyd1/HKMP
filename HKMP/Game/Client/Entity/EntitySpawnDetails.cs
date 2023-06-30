using HutongGames.PlayMaker;
using UnityEngine;

namespace Hkmp.Game.Client.Entity; 

/// <summary>
/// Class that holds the details for how an entity was spawned on the host client. 
/// </summary>
internal class EntitySpawnDetails {
    /// <summary>
    /// The type of the spawn.
    /// </summary>
    public EntitySpawnType Type { get; init; }
    
    /// <summary>
    /// The FSM action responsible for spawning the entity.
    /// </summary>
    public FsmStateAction Action { get; init; }
    
    /// <summary>
    /// The game object that was spawned.
    /// </summary>
    public GameObject GameObject { get; init; }
}

/// <summary>
/// Enumeration of types of possible spawns for the entity.
/// </summary>
internal enum EntitySpawnType {
    FsmAction,
    EnemySpawnerComponent,
    SpawnJarComponent
}
