using System;
using System.Collections.Generic;
using Hkmp.Collection;
using Hkmp.Game.Client.Save;
using Hkmp.Util;
using Newtonsoft.Json;
using Logger = Hkmp.Logging.Logger;

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
            serverSaveData.PlayerSaveData[authKey] = ConvertToServerSaveData(PlayerSaveData[authKey]);
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
            var saveData = ConvertFromServerSaveData(playerSaveData[authKey]);
            // Store the entries in the player save data dictionary of the instance
            modSaveFile.PlayerSaveData[authKey] = saveData;
        }

        return modSaveFile;
    }

    /// <summary>
    /// Convert the given <see cref="SaveData"/> to a dictionary of raw indices and byte arrays.
    /// </summary>
    /// <param name="saveData">The save data to convert.</param>
    /// <returns>A dictionary of raw values.</returns>
    protected static Dictionary<ushort, byte[]> ConvertToServerSaveData(SaveData saveData) {
        // Create new dictionary for this player's specific save data
        var encodedSaveData = new Dictionary<ushort, byte[]>();

        // Loop through the entries in the player's save data
        foreach (var entry in saveData.PlayerDataEntries) {
            var name = entry.Name;
            var decodedObject = entry.Value;

            CheckEncodeAddData(name, SaveDataMapping.Instance.PlayerDataIndices, decodedObject);
        }

        var sceneData = saveData.SceneData;
        foreach (var geoRockData in sceneData.GeoRockData) {
            CheckEncodeAddData(geoRockData.GetKey(), SaveDataMapping.Instance.GeoRockIndices, geoRockData.HitsLeft);
        }

        foreach (var persistentBoolData in sceneData.PersistentBoolData) {
            CheckEncodeAddData(
                persistentBoolData.GetKey(), 
                SaveDataMapping.Instance.PersistentBoolIndices, 
                persistentBoolData.Activated
            );
        }

        foreach (var persistentIntData in sceneData.PersistentIntData) {
            CheckEncodeAddData(
                persistentIntData.GetKey(), 
                SaveDataMapping.Instance.PersistentIntIndices, 
                persistentIntData.Value
            );
        }

        return encodedSaveData;

        void CheckEncodeAddData<TKey>(TKey key, BiLookup<TKey, ushort> lookup, object decodedObject) {
            if (!lookup.TryGetValue(key, out var index)) {
                Logger.Warn($"Could not find index for data for key: {key}");
                return;
            }
            
            // Try to encode the value into our byte array representation
            byte[] encodedValue;
            try {
                encodedValue = EncodeUtil.EncodeSaveDataValue(decodedObject);
            } catch (Exception e) {
                Logger.Warn($"Could not encode save data value of type: {decodedObject.GetType()}, exception:\n{e}");
                return;
            }

            // Finally, store the value in the dictionary we created for the player
            encodedSaveData[index] = encodedValue;
        }
    }

    /// <summary>
    /// Convert the given dictionary of raw indices and byte arrays to <see cref="SaveData"/>.
    /// </summary>
    /// <param name="encodedSaveData">The dictionary containing raw values.</param>
    /// <returns>An instance of save data with the converted values.</returns>
    protected static SaveData ConvertFromServerSaveData(Dictionary<ushort, byte[]> encodedSaveData) {
        var saveData = new SaveData();
        
        foreach (var index in encodedSaveData.Keys) {
            var encodedValue = encodedSaveData[index];

            if (CheckDecodeAddData(
                index,
                SaveDataMapping.Instance.PlayerDataIndices,
                pdName => EncodeUtil.DecodeSaveDataValue(pdName, encodedValue),
                (pdName, decodedObj) => saveData.PlayerDataEntries.Add(new PlayerDataEntry {
                    Name = pdName,
                    Value = decodedObj
                })
            )) {
                continue;
            }

            if (CheckDecodeAddData(
                index,
                SaveDataMapping.Instance.GeoRockIndices,
                _ => encodedValue[0],
                (persistentItemData, decodedObj) => saveData.SceneData.GeoRockData.Add(new GeoRockData {
                    Id = persistentItemData.Id,
                    SceneName = persistentItemData.SceneName,
                    HitsLeft = decodedObj
                })
            )) {
                continue;
            }

            if (CheckDecodeAddData(
                index,
                SaveDataMapping.Instance.PersistentBoolIndices,
                _ => encodedValue[0] == 1,
                (persistentItemData, decodedObj) => saveData.SceneData.PersistentBoolData.Add(new PersistentBoolData {
                    Id = persistentItemData.Id,
                    SceneName = persistentItemData.SceneName,
                    Activated = decodedObj
                })
            )) {
                continue;
            }
            
            CheckDecodeAddData(
                index,
                SaveDataMapping.Instance.PersistentIntIndices,
                _ => (int) encodedValue[0],
                (persistentItemData, decodedObj) => saveData.SceneData.PersistentIntData.Add(new PersistentIntData {
                    Id = persistentItemData.Id,
                    SceneName = persistentItemData.SceneName,
                    Value = decodedObj
                })
            );
        }

        return saveData;

        bool CheckDecodeAddData<TKey, TDecoded>(ushort index, BiLookup<TKey, ushort> lookup, Func<TKey, TDecoded> decodeFunc, Action<TKey, TDecoded> addAction) {
            if (!lookup.TryGetValue(index, out var key)) {
                Logger.Warn($"Could not find key for index: {index}");
                return false;
            }
            
            Logger.Debug($"Trying to decode: {index}, {key}");

            TDecoded decodedObj;
            try {
                decodedObj = decodeFunc.Invoke(key);
            } catch (Exception e) {
                Logger.Warn($"Could not decode save data value with key: {key}, exception:\n{e}");
                return false;
            }
            
            Logger.Debug($"Successfully decoded, invoking add action: {decodedObj}");

            addAction.Invoke(key, decodedObj);

            return true;
        }
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
