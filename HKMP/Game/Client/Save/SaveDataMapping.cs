using System.Collections.Generic;
using System.Linq;
using Hkmp.Collection;
using Hkmp.Logging;
using Newtonsoft.Json;

namespace Hkmp.Game.Client.Save; 

/// <summary>
/// Serializable data class that stores mappings for what scene data should be synchronised and their indices used for networking.
/// </summary>
internal class SaveDataMapping {
    /// <summary>
    /// Dictionary mapping player data values to booleans indicating whether they should be synchronised.
    /// </summary>
    [JsonProperty("playerData")]
    public Dictionary<string, bool> PlayerDataBools { get; private set; }
    
    /// <summary>
    /// Bi-directional lookup that maps save data names and their indices.
    /// </summary>
    [JsonIgnore]
    public BiLookup<string, ushort> PlayerDataIndices { get; private set; }

    /// <summary>
    /// Deserialized key-value pairs for the geo rock data in the JSON.
    /// </summary>
#pragma warning disable 0649
    [JsonProperty("geoRocks")]
    private readonly List<KeyValuePair<PersistentItemData, bool>> _geoRockDataValues;
#pragma warning restore 0649
    
    /// <summary>
    /// Dictionary mapping geo rock data values to booleans indicating whether they should be synchronised.
    /// </summary>
    [JsonIgnore]
    public Dictionary<PersistentItemData, bool> GeoRockDataBools { get; private set; }

    /// <summary>
    /// Bi-directional lookup that maps geo rock names and their indices.
    /// </summary>
    [JsonIgnore]
    public BiLookup<PersistentItemData, ushort> GeoRockDataIndices { get; private set; }
    
    /// <summary>
    /// Deserialized key-value pairs for the persistent bool data in the JSON.
    /// </summary>
#pragma warning disable 0649
    [JsonProperty("persistentBoolItems")]
    private readonly List<KeyValuePair<PersistentItemData, bool>> _persistentBoolsDataValues;
#pragma warning restore 0649
    
    /// <summary>
    /// Dictionary mapping persistent bool data values to booleans indicating whether they should be synchronised.
    /// </summary>
    [JsonIgnore]
    public Dictionary<PersistentItemData, bool> PersistentBoolDataBools { get; private set; }

    /// <summary>
    /// Bi-directional lookup that maps persistent bool names and their indices.
    /// </summary>
    [JsonIgnore]
    public BiLookup<PersistentItemData, ushort> PersistentBoolDataIndices { get; private set; }
    
    /// <summary>
    /// Deserialized key-value pairs for the persistent int data in the JSON.
    /// </summary>
#pragma warning disable 0649
    [JsonProperty("persistentIntItems")]
    private readonly List<KeyValuePair<PersistentItemData, bool>> _persistentIntDataValues;
#pragma warning restore 0649
    
    /// <summary>
    /// Dictionary mapping persistent int data values to booleans indicating whether they should be synchronised.
    /// </summary>
    [JsonIgnore]
    public Dictionary<PersistentItemData, bool> PersistentIntDataBools { get; private set; }

    /// <summary>
    /// Bi-directional lookup that maps persistent int names and their indices.
    /// </summary>
    [JsonIgnore]
    public BiLookup<PersistentItemData, ushort> PersistentIntDataIndices { get; private set; }

    /// <summary>
    /// Deserialized list of strings that represent variable names with the type of a string list.
    /// </summary>
    [JsonProperty("stringListVariables")]
    public readonly List<string> StringListVariables;

    /// <summary>
    /// Deserialized list of strings that represent variable names with the type of BossSequenceDoor.Completion.
    /// </summary>
    [JsonProperty("bossSequenceDoorCompletionVariables")]
    public readonly List<string> BossSequenceDoorCompletionVariables;
    
    /// <summary>
    /// Deserialized list of strings that represent variable names with the type of BossStatue.Completion.
    /// </summary>
    [JsonProperty("bossStatueCompletionVariables")]
    public readonly List<string> BossStatueCompletionVariables;

    /// <summary>
    /// Initializes the class by converting the deserialized data fields into the various dictionaries and lookups.
    /// </summary>
    public void Initialize() {
        if (PlayerDataBools == null) {
            Logger.Warn("Player data bools for save data is null");
            return;
        }

        if (_geoRockDataValues == null) {
            Logger.Warn("Geo rock data values for save data is null");
            return;
        }
        
        if (_persistentBoolsDataValues == null) {
            Logger.Warn("Persistent bools data values for save data is null");
            return;
        }
        
        if (_persistentIntDataValues == null) {
            Logger.Warn("Persistent int data values for save data is null");
            return;
        }
        
        PlayerDataIndices = new BiLookup<string, ushort>();
        ushort index = 0;
        foreach (var playerDataBool in PlayerDataBools.Keys) {
            PlayerDataIndices.Add(playerDataBool, index++);
        }

        GeoRockDataBools = _geoRockDataValues.ToDictionary(kv => kv.Key, kv => kv.Value);
        GeoRockDataIndices = new BiLookup<PersistentItemData, ushort>();
        foreach (var geoRockData in GeoRockDataBools.Keys) {
            GeoRockDataIndices.Add(geoRockData, index++);
        }

        PersistentBoolDataBools = _persistentBoolsDataValues.ToDictionary(kv => kv.Key, kv => kv.Value);
        PersistentBoolDataIndices = new BiLookup<PersistentItemData, ushort>();
        foreach (var persistentBoolData in PersistentBoolDataBools.Keys) {
            PersistentBoolDataIndices.Add(persistentBoolData, index++);
        }

        PersistentIntDataBools = _persistentIntDataValues.ToDictionary(kv => kv.Key, kv => kv.Value);
        PersistentIntDataIndices = new BiLookup<PersistentItemData, ushort>();
        foreach (var persistentIntData in PersistentIntDataBools.Keys) {
            PersistentIntDataIndices.Add(persistentIntData, index++);
        }
    }
}
