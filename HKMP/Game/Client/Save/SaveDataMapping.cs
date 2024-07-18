using System.Collections.Generic;
using System.Linq;
using Hkmp.Collection;
using Hkmp.Logging;
using Hkmp.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Hkmp.Game.Client.Save; 

/// <summary>
/// Serializable data class that stores mappings for what scene data should be synchronised and their indices used for networking.
/// </summary>
internal class SaveDataMapping {
    /// <summary>
    /// The file path of the embedded resource file for save data.
    /// </summary>
    private const string SaveDataFilePath = "Hkmp.Resource.save-data.json";
    
    /// <summary>
    /// The static instance of the mapping.
    /// </summary>
    [JsonIgnore]
    private static SaveDataMapping _instance;
    
    /// <inheritdoc cref="_instance"/>
    [JsonIgnore]
    public static SaveDataMapping Instance {
        get {
            if (_instance == null) {
                _instance = FileUtil.LoadObjectFromEmbeddedJson<SaveDataMapping>(SaveDataFilePath);
                _instance.Initialize();
            }

            return _instance;
        }
    }

    /// <summary>
    /// Dictionary mapping player data values to booleans indicating whether they should be synchronised.
    /// </summary>
    [JsonProperty("playerData")]
    public Dictionary<string, SyncProperties> PlayerDataBools { get; private set; }
    
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
    private readonly List<KeyValuePair<PersistentItemData, SyncProperties>> _persistentBoolsDataValues;
#pragma warning restore 0649
    
    /// <summary>
    /// Dictionary mapping persistent bool data values to booleans indicating whether they should be synchronised.
    /// </summary>
    [JsonIgnore]
    public Dictionary<PersistentItemData, SyncProperties> PersistentBoolDataBools { get; private set; }

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
    private readonly List<KeyValuePair<PersistentItemData, SyncProperties>> _persistentIntDataValues;
#pragma warning restore 0649
    
    /// <summary>
    /// Dictionary mapping persistent int data values to booleans indicating whether they should be synchronised.
    /// </summary>
    [JsonIgnore]
    public Dictionary<PersistentItemData, SyncProperties> PersistentIntDataBools { get; private set; }

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

    /// <summary>
    /// Properties that denote when to sync values.
    /// </summary>
    internal class SyncProperties {
        /// <summary>
        /// Whether to sync this value. If true, the variable <seealso cref="SyncType"/> indicates where to store
        /// the synced values.
        /// </summary>
        public bool Sync { get; set; }
        /// <summary>
        /// The sync type of this value. Type Player is used for player specific values and Server is used for
        /// global world specific values.
        /// </summary>
        public SyncType SyncType { get; set; }
        /// <summary>
        /// Whether to ignore the check for scene host when sending/processing a save data for this value.
        /// </summary>
        public bool IgnoreSceneHost { get; set; }
    }

    /// <summary>
    /// The sync type for sync properties indicating whether values are global or player specific.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    internal enum SyncType {
        Player,
        Server
    }
}
