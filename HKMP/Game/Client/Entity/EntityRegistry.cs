using System.Collections.Generic;
using System.Linq;
using Hkmp.Game.Client.Entity.Component;
using Hkmp.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Entity; 

/// <summary>
/// Static class that manages loading and storing of entity data. Such as names of game objects, names of FSMs and
/// corresponding types.
/// </summary>
internal static class EntityRegistry {
    /// <summary>
    /// The file path of the embedded resource file for the entity registry.
    /// </summary>
    private const string EntityRegistryFilePath = "Hkmp.Resource.entity-registry.json";
    
    /// <summary>
    /// List of all entity registry entries that are loaded from the embedded file.
    /// </summary>
    private static List<EntityRegistryEntry> Entries { get; }

    static EntityRegistry() {
        Entries = FileUtil.LoadObjectFromEmbeddedJson<List<EntityRegistryEntry>>(EntityRegistryFilePath);
        if (Entries == null) {
            Logger.Warn("Could not load entity registry");
        }
    }

    /// <summary>
    /// Try to get the corresponding entry from the given enumerable of entries and the given game object.
    /// </summary>
    /// <param name="gameObject">The game object to find the entry for.</param>
    /// <param name="foundEntry">The entry if it was found; otherwise null.</param>
    /// <returns>true if the entry was found; otherwise false.</returns>
    public static bool TryGetEntry(
        GameObject gameObject,
        out EntityRegistryEntry foundEntry
    ) {
        return TryGetEntry(Entries, gameObject, out foundEntry);
    }

    /// <summary>
    /// Try to get the corresponding entry from the given enumerable of entries and the given game object.
    /// </summary>
    /// <param name="entries">The enumerable of entries to check for.</param>
    /// <param name="gameObject">The game object to find the entry for.</param>
    /// <param name="foundEntry">The entry if it was found; otherwise null.</param>
    /// <returns>true if the entry was found; otherwise false.</returns>
    public static bool TryGetEntry(
        IEnumerable<EntityRegistryEntry> entries, 
        GameObject gameObject, 
        out EntityRegistryEntry foundEntry
    ) {
        foreach (var entry in entries) {
            if (!gameObject.name.Contains(entry.BaseObjectName)) {
                continue;
            }

            // If the entry has an FSM name defined and the child object does not have any FSM components
            // that match this name, we continue
            if (entry.FsmName != null && !gameObject.GetComponents<PlayMakerFSM>().Any(
                    childFsm => childFsm.Fsm.Name.Equals(entry.FsmName)
            )) {
                continue;
            }

            // If the entry has a parent name defined, we need to check if the parent of the game object matches it
            if (entry.ParentName != null) {
                var parent = gameObject.transform.parent;
                // No parent at all, so it trivially doesn't match the name
                if (parent == null) {
                    continue;
                }

                if (!parent.gameObject.name.Contains(entry.ParentName)) {
                    continue;
                }
            }

            // Specifically check for entries that don't have a defined FSM whether they contain the
            // correct component(s)
            if (entry.Type == EntityType.Tiktik) {
                if (gameObject.GetComponent<Climber>() == null) {
                    continue;
                }
            } else if (entry.Type == EntityType.VengeflySummon) {
                if (gameObject.GetComponent<EnemySpawner>() == null) {
                    continue;
                }
            } else if (entry.Type == EntityType.CollectorJar) {
                if (gameObject.GetComponent<SpawnJarControl>() == null) {
                    continue;
                }
            } else if (entry.Type == EntityType.Garpede) {
                if (gameObject.GetComponent<BigCentipede>() == null) {
                    continue;
                }
            } else if (entry.Type == EntityType.ShadowCreeper) {
                if (gameObject.GetComponent<Crawler>() == null) {
                    continue;
                }
            } else if (entry.Type == EntityType.GrimmFireball) {
                if (gameObject.GetComponent<GrimmballControl>() == null) {
                    continue;
                }
            }

            foundEntry = entry;
            return true;
        }

        foundEntry = null;
        return false;
    }
}

/// <summary>
/// Class representing a single entry in the entity registry that contains the relevant data for an entity type.
/// </summary>
internal class EntityRegistryEntry {
    /// <summary>
    /// The base of the game object name of the entity.
    /// For example: "Zombie Leaper", which in-game can be represented as "Zombie Leaper (Clone) (1)"
    /// </summary>
    [JsonProperty("base_object_name")]
    public string BaseObjectName { get; set; }
    
    /// <summary>
    /// The type of the entity.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    [JsonProperty("type")]
    public EntityType Type { get; set; }
    
    /// <summary>
    /// The name of the FSM that this entity has. Can be empty if the entity does not have a FSM.
    /// </summary>
    [JsonProperty("fsm_name")]
    public string FsmName { get; set; }
    
    /// <summary>
    /// The name of the parent of this object. Can be empty if there is no parent or it is not relevant.
    /// </summary>
    [JsonProperty("parent_name")]
    public string ParentName { get; set; }

    /// <summary>
    /// Array of additional entity component that should be initialized for the entity.
    /// </summary>
    [JsonProperty("components")]
    public EntityComponentType[] ComponentTypes { get; set; }

    /// <summary>
    /// List of entries that are children of this entry.
    /// </summary>
    [JsonProperty("children")]
    public List<EntityRegistryEntry> Children { get; set; }
}
