using System.Collections.Generic;
using Hkmp.Logging;
using Hkmp.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
    /// Try to get the entity registry entry given a game object name and a FSM name.
    /// </summary>
    /// <param name="gameObjectName">The name of the game object.</param>
    /// <param name="fsmName">The name of the FSM.</param>
    /// <param name="foundEntry">The entry if it is found; otherwise null.</param>
    /// <returns>True if the entry was found; otherwise false.</returns>
    public static bool TryGetEntry(string gameObjectName, string fsmName, out EntityRegistryEntry foundEntry) {
        foreach (var entry in Entries) {
            if (!entry.FsmName.Equals(fsmName)) {
                continue;
            }

            if (gameObjectName.Contains(entry.BaseObjectName)) {
                foundEntry = entry;
                return true;
            }
        }

        foundEntry = null;
        return false;
    }

    /// <summary>
    /// Try to get the entity registry entry given a game object name and an entity type.
    /// </summary>
    /// <param name="gameObjectName">The name of the game object.</param>
    /// <param name="type">The type of the entity.</param>
    /// <param name="foundEntry">The entry if it is found; otherwise null.</param>
    /// <returns>True if the entry was found; otherwise false.</returns>
    public static bool TryGetEntry(string gameObjectName, EntityType type, out EntityRegistryEntry foundEntry) {
        foreach (var entry in Entries) {
            if (!entry.Type.Equals(type)) {
                continue;
            }

            if (gameObjectName.Contains(entry.BaseObjectName)) {
                foundEntry = entry;
                return true;
            }
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
}
