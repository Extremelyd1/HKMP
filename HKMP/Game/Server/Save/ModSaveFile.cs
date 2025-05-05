using System.Collections.Generic;
using Hkmp.Game.Client.Save;
using Hkmp.Util;
using Newtonsoft.Json;

namespace Hkmp.Game.Server.Save;

/// <summary>
/// Class for serialization and deserialization of save data from a server to the local save file.
/// See <see cref="ServerSaveData"/> for the representation of the same data of the running server.
/// </summary>
internal class ModSaveFile {
    /// <summary>
    /// The player specific save data mapped to player's auth keys.
    /// </summary>
    [JsonProperty("playerSaveData")]
    public Dictionary<string, SaveData> PlayerSaveData { get; set; }

    public ModSaveFile() {
        PlayerSaveData = new Dictionary<string, SaveData>();
    }

    /// <summary>
    /// Convert this class to an encoded ServerSaveData.
    /// </summary>
    /// <returns>The converted ServerSaveData instance.</returns>
    public virtual ServerSaveData ToServerSaveData() {
        // Create new instance of server save data, which we return at the end
        var serverSaveData = new ServerSaveData();

        foreach (var authKey in PlayerSaveData.Keys) {
            serverSaveData.PlayerSaveData[authKey] = EncodeUtil.ConvertToServerSaveData(PlayerSaveData[authKey]);
        }
        
        return serverSaveData;
    }

    /// <summary>
    /// Get an instance of this class with the decoded data from the given ServerSaveData. 
    /// </summary>
    /// <param name="serverSaveData">The encoded ServerSaveData.</param>
    /// <returns>An instance of this class.</returns>
    public static ModSaveFile FromServerSaveData(ServerSaveData serverSaveData) {
        // Create new instance of this class, which we return at the end
        var modSaveFile = new ModSaveFile();

        var playerSaveData = serverSaveData.PlayerSaveData;
        foreach (var authKey in playerSaveData.Keys) {
            var saveData = EncodeUtil.ConvertFromServerSaveData(playerSaveData[authKey]);
            // Store the entries in the player save data dictionary of the instance
            modSaveFile.PlayerSaveData[authKey] = saveData;
        }

        return modSaveFile;
    }

    /// <summary>
    /// Serializable save data that contains PlayerData and SceneData similar to the HK save file.
    /// </summary>
    public class SaveData {
        /// <summary>
        /// PlayerData entries that use a custom serialization.
        /// <seealso cref="PlayerSaveDataConverter"/>
        /// </summary>
        [JsonProperty("playerData")]
        public PlayerDataEntries PlayerDataEntries { get; set; }

        /// <summary>
        /// SceneData instance that contains geo rocks and persistent items.
        /// </summary>
        [JsonProperty("sceneData")]
        public SceneData SceneData { get; set; }

        public SaveData() {
            PlayerDataEntries = [];
            SceneData = new SceneData();
        }
    }

    /// <summary>
    /// Serializable SceneData class that contains geo rocks, persistent integers, and persistent booleans.
    /// </summary>
    public class SceneData {
        /// <summary>
        /// List of individual geo rocks.
        /// </summary>
        [JsonProperty("geoRocks")]
        public List<GeoRockData> GeoRockData { get; set; }
        /// <summary>
        /// List of persistent booleans.
        /// </summary>
        [JsonProperty("persistentBoolItems")]
        public List<PersistentBoolData> PersistentBoolData { get; set; }
        /// <summary>
        /// List of persistent integers.
        /// </summary>
        [JsonProperty("persistentIntItems")]
        public List<PersistentIntData> PersistentIntData { get; set; }

        public SceneData() {
            GeoRockData = [];
            PersistentBoolData = [];
            PersistentIntData = [];
        }
    }

    /// <summary>
    /// Base class for serializable scene data items, such as geo rocks or persistent items.
    /// <seealso cref="GeoRockData"/>
    /// <seealso cref="PersistentBoolData"/>
    /// <seealso cref="PersistentIntData"/>
    /// </summary>
    public class SceneDataItem {
        /// <summary>
        /// The ID of the item.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// The scene name the item is in.
        /// </summary>
        [JsonProperty("sceneName")]
        public string SceneName { get; set; }

        /// <summary>
        /// Get a persistent item key for this item with the correct ID and scene name.
        /// </summary>
        /// <returns>An instance of <see cref="PersistentItemKey"/>.</returns>
        public PersistentItemKey GetKey() {
            return new PersistentItemKey {
                Id = Id,
                SceneName = SceneName
            };
        }
    }

    /// <summary>
    /// Serializable geo rock data.
    /// </summary>
    public class GeoRockData : SceneDataItem {
        /// <summary>
        /// The number of hits left for this geo rock.
        /// </summary>
        [JsonProperty("hitsLeft")]
        public int HitsLeft { get; set; }
    }
    
    /// <summary>
    /// Serializable persistent boolean.
    /// </summary>
    public class PersistentBoolData : SceneDataItem {
        /// <summary>
        /// Whether the item was activated.
        /// </summary>
        [JsonProperty("activated")]
        public bool Activated { get; set; }
    }
    
    /// <summary>
    /// Serializable persistent integer.
    /// </summary>
    public class PersistentIntData : SceneDataItem {
        /// <summary>
        /// The value of the item.
        /// </summary>
        [JsonProperty("value")]
        public int Value { get; set; }
    }

    /// <summary>
    /// List of <see cref="PlayerDataEntry"/> with a custom converter to make sure JSON (de)serialization is handled correctly.
    /// </summary>
    [JsonConverter(typeof(PlayerSaveDataConverter))]
    public class PlayerDataEntries : List<PlayerDataEntry>;

    /// <summary>
    /// A single entry for a mod save file with a name and corresponding value that can be any type.
    /// </summary>
    public class PlayerDataEntry {
        /// <summary>
        /// The name of the PlayerData variable.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The value of the PlayerData variable as an object.
        /// </summary>
        public object Value { get; set; }
    }
}
