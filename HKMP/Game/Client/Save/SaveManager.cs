using System;
using System.Collections.Generic;
using System.Linq;
using Hkmp.Collection;
using Hkmp.Game.Client.Entity;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Hkmp.Logging.Logger;
using Object = UnityEngine.Object;

namespace Hkmp.Game.Client.Save; 

/// <summary>
/// Class that manages save data synchronisation.
/// </summary>
internal class SaveManager {
    /// <summary>
    /// The file path of the embedded resource file for save data.
    /// </summary>
    private const string SaveDataFilePath = "Hkmp.Resource.save-data.json";

    /// <summary>
    /// The save data instances that contains mappings for what to sync and their indices.
    /// </summary>
    private static readonly SaveDataMapping SaveDataMapping;

    /// <summary>
    /// The net client instance to send save updates.
    /// </summary>
    private readonly NetClient _netClient;
    /// <summary>
    /// The packet manager instance to register a callback for when save updates are received.
    /// </summary>
    private readonly PacketManager _packetManager;
    /// <summary>
    /// The entity manager to check whether we are scene host.
    /// </summary>
    private readonly EntityManager _entityManager;
    
    /// <summary>
    /// List of data classes for each FSM that has a persistent int/bool or geo rock attached to it.
    /// </summary>
    private readonly List<PersistentFsmData> _persistentFsmData;

    /// <summary>
    /// Dictionary of hash codes for string list variables in the PlayerData for comparing changes against.
    /// </summary>
    private readonly Dictionary<string, int> _stringListHashes;

    /// <summary>
    /// Dictionary of BossSequenceDoor.Completion structs in the PlayerData for comparing changes against.
    /// </summary>
    private readonly Dictionary<string, BossSequenceDoor.Completion> _bsdCompHashes;
    
    /// <summary>
    /// Dictionary of BossStatue.Completion structs in the PlayerData for comparing changes against.
    /// </summary>
    private readonly Dictionary<string, BossStatue.Completion> _bsCompHashes;

    public SaveManager(NetClient netClient, PacketManager packetManager, EntityManager entityManager) {
        _netClient = netClient;
        _packetManager = packetManager;
        _entityManager = entityManager;

        _persistentFsmData = new List<PersistentFsmData>();
        _stringListHashes = new Dictionary<string, int>();
        _bsdCompHashes = new Dictionary<string, BossSequenceDoor.Completion>();
        _bsCompHashes = new Dictionary<string, BossStatue.Completion>();
    }

    /// <summary>
    /// Static constructor to load and initialize the save data mapping.
    /// </summary>
    static SaveManager() {
        SaveDataMapping = FileUtil.LoadObjectFromEmbeddedJson<SaveDataMapping>(SaveDataFilePath);
        SaveDataMapping.Initialize();
    }

    /// <summary>
    /// Initializes the save manager by loading the save data json.
    /// </summary>
    public void Initialize() {
        ModHooks.SetPlayerBoolHook += OnSetPlayerBoolHook;
        ModHooks.SetPlayerFloatHook += OnSetPlayerFloatHook;
        ModHooks.SetPlayerIntHook += OnSetPlayerIntHook;
        ModHooks.SetPlayerStringHook += OnSetPlayerStringHook;
        ModHooks.SetPlayerVariableHook += OnSetPlayerVariableHook;
        ModHooks.SetPlayerVector3Hook += OnSetPlayerVector3Hook;
        
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;

        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdatePersistents;
        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdateCompounds;
        
        _packetManager.RegisterClientPacketHandler<SaveUpdate>(ClientPacketId.SaveUpdate, UpdateSaveWithData);
    }

    /// <summary>
    /// Encode a given value into a byte array in the context of save data.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>A byte array containing the encoded value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the given value is out of range to be encoded.
    /// </exception>
    /// <exception cref="NotImplementedException">Thrown when the given value has a type that cannot be encoded due to
    /// missing implementation.</exception>
    private static byte[] EncodeValue(object value) {
        // Since all strings in the save data are scene names (or map scene names), we can convert them to indices
        byte[] EncodeString(string stringValue) {
            if (!EncodeUtil.GetSceneIndex(stringValue, out var index)) {
                // Logger.Info($"Could not encode string value: {stringValue}");
                // return Array.Empty<byte>();
                throw new Exception($"Could not encode string value: {stringValue}");
            }

            return BitConverter.GetBytes(index);
        }
        
        if (value is bool bValue) {
            return [(byte) (bValue ? 1 : 0)];
        }

        if (value is float fValue) {
            return BitConverter.GetBytes(fValue);
        }

        if (value is int iValue) {
            return BitConverter.GetBytes(iValue);
        }

        if (value is string sValue) {
            return EncodeString(sValue);
        }

        if (value is Vector3 vecValue) {
            return BitConverter.GetBytes(vecValue.x)
                .Concat(BitConverter.GetBytes(vecValue.y))
                .Concat(BitConverter.GetBytes(vecValue.z))
                .ToArray();
        }

        if (value is List<string> listValue) {
            if (listValue.Count > ushort.MaxValue) {
                throw new ArgumentOutOfRangeException($"Could not encode string list length: {listValue.Count}");
            }
            
            var length = (ushort) listValue.Count;
            
            IEnumerable<byte> byteArray = BitConverter.GetBytes(length);

            for (var i = 0; i < length; i++) {
                var encoded = EncodeString(listValue[i]);
                
                byteArray = byteArray.Concat(encoded);
            }

            return byteArray.ToArray();
        }

        if (value is BossSequenceDoor.Completion bsdCompValue) {
            // For now we only encode the bools of completion struct
            var firstBools = new[] {
                bsdCompValue.canUnlock, bsdCompValue.unlocked, bsdCompValue.completed, bsdCompValue.allBindings, bsdCompValue.noHits,
                bsdCompValue.boundNail, bsdCompValue.boundShell, bsdCompValue.boundCharms
            };

            var byte1 = EncodeUtil.GetByte(firstBools);

            var byte2 = (byte) (bsdCompValue.boundSoul ? 1 : 0);

            return [byte1, byte2];
        }

        if (value is BossStatue.Completion bsCompValue) {
            var bools = new[] {
                bsCompValue.hasBeenSeen, bsCompValue.isUnlocked, bsCompValue.completedTier1, bsCompValue.completedTier2,
                bsCompValue.completedTier3, bsCompValue.seenTier3Unlock, bsCompValue.usingAltVersion
            };

            return [EncodeUtil.GetByte(bools)];
        }

        throw new NotImplementedException($"No encoding implementation for type: {value.GetType()}");
    }

    /// <summary>
    /// Callback method for when a boolean is set in the player data.
    /// </summary>
    /// <param name="name">Name of the boolean variable.</param>
    /// <param name="orig">The original value of the boolean.</param>
    private bool OnSetPlayerBoolHook(string name, bool orig) {
        CheckSendSaveUpdate(name, () => EncodeValue(orig));

        return orig;
    }
    
    /// <summary>
    /// Callback method for when a float is set in the player data.
    /// </summary>
    /// <param name="name">Name of the float variable.</param>
    /// <param name="orig">The original value of the float.</param>
    private float OnSetPlayerFloatHook(string name, float orig) {
        CheckSendSaveUpdate(name, () => EncodeValue(orig));

        return orig;
    }
    
    /// <summary>
    /// Callback method for when a int is set in the player data.
    /// </summary>
    /// <param name="name">Name of the int variable.</param>
    /// <param name="orig">The original value of the int.</param>
    private int OnSetPlayerIntHook(string name, int orig) {
        CheckSendSaveUpdate(name, () => EncodeValue(orig));

        return orig;
    }

    /// <summary>
    /// Callback method for when a string is set in the player data.
    /// </summary>
    /// <param name="name">Name of the string variable.</param>
    /// <param name="res">The original value of the boolean.</param>
    private string OnSetPlayerStringHook(string name, string res) {
        CheckSendSaveUpdate(name, () => EncodeValue(res));

        return res;
    }

    /// <summary>
    /// Callback method for when an object is set in the player data.
    /// </summary>
    /// <param name="type">The type of the object.</param>
    /// <param name="name">Name of the object variable.</param>
    /// <param name="value">The original value of the object.</param>
    private object OnSetPlayerVariableHook(Type type, string name, object value) {
        CheckSendSaveUpdate(name, () => EncodeValue(value));

        return value;
    }

    /// <summary>
    /// Callback method for when a vector3 is set in the player data.
    /// </summary>
    /// <param name="name">Name of the vector3 variable.</param>
    /// <param name="orig">The original value of the vector3.</param>
    private Vector3 OnSetPlayerVector3Hook(string name, Vector3 orig) {
        CheckSendSaveUpdate(name, () => EncodeValue(orig));
        
        return orig;
    }

    /// <summary>
    /// Callback method for when the scene changes. Used to check for GeoRock, PersistentInt and PersistentBool
    /// instances in the scene.
    /// </summary>
    /// <param name="oldScene">The old scene.</param>
    /// <param name="newScene">The new scene.</param>
    private void OnSceneChanged(Scene oldScene, Scene newScene) {
        _persistentFsmData.Clear();
        
        foreach (var geoRock in Object.FindObjectsOfType<GeoRock>()) {
            var geoRockObject = geoRock.gameObject;
            
            if (geoRockObject.scene != newScene) {
                continue;
            }

            var persistentItemData = new PersistentItemData {
                Id = geoRockObject.name,
                SceneName = global::GameManager.GetBaseSceneName(geoRockObject.scene.name)
            };
            
            Logger.Info($"Found Geo Rock in scene: {persistentItemData}");

            var fsm = geoRock.GetComponent<PlayMakerFSM>();
            if (fsm == null) {
                Logger.Info("  Could not find FSM belonging to Geo Rock object, skipping");
                continue;
            }
            
            var fsmInt = fsm.FsmVariables.GetFsmInt("Hits");

            var persistentFsmData = new PersistentFsmData {
                PersistentItemData = persistentItemData,
                FsmInt = fsmInt,
                LastIntValue = fsmInt.Value
            };

            _persistentFsmData.Add(persistentFsmData);
        }
        
        foreach (var persistentBoolItem in Object.FindObjectsOfType<PersistentBoolItem>()) {
            var itemObject = persistentBoolItem.gameObject;
            
            if (itemObject.scene != newScene) {
                continue;
            }

            var persistentItemData = new PersistentItemData {
                Id = itemObject.name,
                SceneName = global::GameManager.GetBaseSceneName(itemObject.scene.name)
            };
            
            Logger.Info($"Found persistent bool in scene: {persistentItemData}");
            
            var fsm = FSMUtility.FindFSMWithPersistentBool(itemObject.GetComponents<PlayMakerFSM>());
            if (fsm == null) {
                Logger.Info("  Could not find FSM belonging to persistent bool object, skipping");
                continue;
            }
            
            var fsmBool = fsm.FsmVariables.GetFsmBool("Activated");

            var persistentFsmData = new PersistentFsmData {
                PersistentItemData = persistentItemData,
                FsmBool = fsmBool,
                LastBoolValue = fsmBool.Value
            };

            _persistentFsmData.Add(persistentFsmData);
        }
        
        foreach (var persistentIntItem in Object.FindObjectsOfType<PersistentIntItem>()) {
            var itemObject = persistentIntItem.gameObject;
            
            if (itemObject.scene != newScene) {
                continue;
            }

            var persistentItemData = new PersistentItemData {
                Id = itemObject.name,
                SceneName = global::GameManager.GetBaseSceneName(itemObject.scene.name)
            };
            
            Logger.Info($"Found persistent int in scene: {persistentItemData}");

            var fsm = FSMUtility.FindFSMWithPersistentBool(itemObject.GetComponents<PlayMakerFSM>());
            if (fsm == null) {
                Logger.Info("  Could not find FSM belonging to persistent int object, skipping");
                continue;
            }

            var fsmInt = fsm.FsmVariables.GetFsmInt("Value");

            var persistentFsmData = new PersistentFsmData {
                PersistentItemData = persistentItemData,
                FsmInt = fsmInt,
                LastIntValue = fsmInt.Value
            };

            _persistentFsmData.Add(persistentFsmData);
        }
    }

    /// <summary>
    /// Checks if a save update should be sent and send it using the encode function to encode the value of the
    /// changed variable.
    /// </summary>
    /// <param name="name">The name of the variable that was changed.</param>
    /// <param name="encodeFunc">Function to encode the value of the variable to a byte array.</param>
    private void CheckSendSaveUpdate(string name, Func<byte[]> encodeFunc) {
        if (!_entityManager.IsSceneHost) {
            Logger.Info($"Not scene host, not sending save update ({name})");
            return;
        }

        if (!SaveDataMapping.PlayerDataBools.TryGetValue(name, out var value) || !value) {
            Logger.Info($"Not in save data values or false in save data values, not sending save update ({name})");
            return;
        }

        if (!SaveDataMapping.PlayerDataIndices.TryGetValue(name, out var index)) {
            Logger.Info($"Cannot find save data index, not sending save update ({name})");
            return;
        }
        
        Logger.Info($"Sending \"{name}\" as save update");
        
        _netClient.UpdateManager.SetSaveUpdate(
            index,
            encodeFunc.Invoke()
        );
    }

    /// <summary>
    /// Called every unity update. Used to check for changes in the GeoRock/PersistentInt/PersistentBool FSMs.
    /// </summary>
    private void OnUpdatePersistents() {
        using var enumerator = _persistentFsmData.GetEnumerator();

        while (enumerator.MoveNext()) {
            var persistentFsmData = enumerator.Current;
            if (persistentFsmData == null) {
                continue;
            }

            if (persistentFsmData.IsInt) {
                var value = persistentFsmData.FsmInt.Value;
                if (value == persistentFsmData.LastIntValue) {
                    continue;
                }

                persistentFsmData.LastIntValue = value;

                var itemData = persistentFsmData.PersistentItemData;
                
                Logger.Info($"Value for {itemData} changed to: {value}");
                
                if (!_entityManager.IsSceneHost) {
                    Logger.Info($"Not scene host, not sending persistent int/geo rock save update ({itemData.Id}, {itemData.SceneName})");
                    continue;
                }

                if (SaveDataMapping.GeoRockDataBools.TryGetValue(itemData, out var shouldSync) && shouldSync) {
                    if (!SaveDataMapping.GeoRockDataIndices.TryGetValue(itemData, out var index)) {
                        Logger.Info(
                            $"Cannot find geo rock save data index, not sending save update ({itemData.Id}, {itemData.SceneName})");
                        continue;
                    }

                    Logger.Info($"Sending geo rock ({itemData.Id}, {itemData.SceneName}) as save update");

                    _netClient.UpdateManager.SetSaveUpdate(
                        index,
                        new[] { (byte) value }
                    );
                } else if (SaveDataMapping.PersistentIntDataBools.TryGetValue(itemData, out shouldSync) && shouldSync) {
                    if (!SaveDataMapping.PersistentIntDataIndices.TryGetValue(itemData, out var index)) {
                        Logger.Info(
                            $"Cannot find persistent int save data index, not sending save update ({itemData.Id}, {itemData.SceneName})");
                        continue;
                    }

                    Logger.Info($"Sending persistent int ({itemData.Id}, {itemData.SceneName}) as save update");

                    _netClient.UpdateManager.SetSaveUpdate(
                        index,
                        new[] { (byte) value }
                    );
                } else {
                    Logger.Info("Cannot find persistent int/geo rock data bool, not sending save update");
                }
            } else {
                var value = persistentFsmData.FsmBool.Value;
                if (value == persistentFsmData.LastBoolValue) {
                    continue;
                }

                persistentFsmData.LastBoolValue = value;
                
                var itemData = persistentFsmData.PersistentItemData;

                Logger.Info($"Value for {itemData} changed to: {value}");
                
                if (!_entityManager.IsSceneHost) {
                    Logger.Info($"Not scene host, not sending geo rock save update ({itemData.Id}, {itemData.SceneName})");
                    continue;
                }
                
                if (!SaveDataMapping.PersistentBoolDataBools.TryGetValue(itemData, out var shouldSync) || !shouldSync) {
                    Logger.Info($"Not in persistent bool save data values or false in save data values, not sending save update ({itemData.Id}, {itemData.SceneName})");
                    continue;
                }

                if (!SaveDataMapping.PersistentBoolDataIndices.TryGetValue(itemData, out var index)) {
                    Logger.Info($"Cannot find persistent bool save data index, not sending save update ({itemData.Id}, {itemData.SceneName})");
                    continue;
                }
        
                Logger.Info($"Sending persistent bool ({itemData.Id}, {itemData.SceneName}) as save update");

                _netClient.UpdateManager.SetSaveUpdate(
                    index,
                    BitConverter.GetBytes(value)
                );
            }
        }
    }

    /// <summary>
    /// Called every unity update. Used to check for changes in non-primitive variables in the PlayerData.
    /// </summary>
    private void OnUpdateCompounds() {
        void CheckUpdates<TVar, TCheck>(
            List<string> variableNames,
            Dictionary<string, TCheck> checkDict,
            Func<TVar, TCheck> newCheckFunc,
            Func<TCheck, TCheck, bool> changeFunc
        ) {
            foreach (var varName in variableNames) {
                if (!SaveDataMapping.PlayerDataBools.TryGetValue(varName, out var shouldSync) || !shouldSync) {
                    continue;
                }

                if (!SaveDataMapping.PlayerDataIndices.TryGetValue(varName, out var index)) {
                    continue;
                }

                var variable = (TVar) typeof(PlayerData).GetField(varName).GetValue(PlayerData.instance);
                var newCheck = newCheckFunc.Invoke(variable);

                if (!checkDict.TryGetValue(varName, out var check)) {
                    checkDict[varName] = newCheck;
                    continue;
                }

                if (changeFunc(newCheck, check)) {
                    Logger.Info($"Compound variable ({varName}) changed value");
                    
                    checkDict[varName] = newCheck;

                    if (_netClient.IsConnected && _entityManager.IsSceneHost) {
                        _netClient.UpdateManager.SetSaveUpdate(
                            index,
                            EncodeValue(variable)
                        );
                    }
                }
            }
        }
        
        CheckUpdates<List<string>, int>(
            SaveDataMapping.StringListVariables,
            _stringListHashes,
            GetStringListHashCode,
            (hash1, hash2) => hash1 != hash2
        );
        
        CheckUpdates<BossSequenceDoor.Completion, BossSequenceDoor.Completion>(
            SaveDataMapping.BossSequenceDoorCompletionVariables,
            _bsdCompHashes,
            bsdComp => bsdComp,
            (b1, b2) => 
                b1.canUnlock != b2.canUnlock || 
                b1.unlocked != b2.unlocked ||
                b1.completed != b2.completed ||
                b1.allBindings != b2.allBindings ||
                b1.noHits != b2.noHits ||
                b1.boundNail != b2.boundNail ||
                b1.boundShell != b2.boundShell ||
                b1.boundCharms != b2.boundCharms ||
                b1.boundSoul != b2.boundSoul
        );

        CheckUpdates<BossStatue.Completion, BossStatue.Completion>(
            SaveDataMapping.BossStatueCompletionVariables,
            _bsCompHashes,
            bsComp => bsComp,
            (b1, b2) =>
                b1.hasBeenSeen != b2.hasBeenSeen ||
                b1.isUnlocked != b2.isUnlocked ||
                b1.completedTier1 != b2.completedTier1 ||
                b1.completedTier2 != b2.completedTier2 ||
                b1.completedTier3 != b2.completedTier3 ||
                b1.seenTier3Unlock != b2.seenTier3Unlock ||
                b1.usingAltVersion != b2.usingAltVersion
        );
    }

    /// <summary>
    /// Callback method for when a save update is received.
    /// </summary>
    /// <param name="saveUpdate">The save update that was received.</param>
    private void UpdateSaveWithData(SaveUpdate saveUpdate) {
        Logger.Info($"Received save update for index: {saveUpdate.SaveDataIndex}");

        var index = saveUpdate.SaveDataIndex;
        var value = saveUpdate.Value;

        UpdateSaveWithData(index, value);
    }

    public void SetSaveWithData(CurrentSave currentSave) {
        Logger.Info("Received current save, updating...");

        foreach (var keyValuePair in currentSave.SaveData) {
            var index = keyValuePair.Key;
            var value = keyValuePair.Value;

            UpdateSaveWithData(index, value);
        }
    }

    /// <summary>
    /// Update the local save with the given data (index and encoded value).
    /// </summary>
    /// <param name="index">The index of the save data.</param>
    /// <param name="encodedValue">A byte array containing the encoded value of the save data.</param>
    /// <exception cref="NotImplementedException">Thrown when the type belonging to the save data cannot be decoded
    /// due to a missing implementation.</exception>
    private void UpdateSaveWithData(ushort index, byte[] encodedValue) {
        var pd = PlayerData.instance;
        var sceneData = SceneData.instance;

        if (SaveDataMapping.PlayerDataIndices.TryGetValue(index, out var name)) {
            Logger.Info($"Received save update ({index}, {name})");
            
            var fieldInfo = typeof(PlayerData).GetField(name);
            var type = fieldInfo.FieldType;
            var valueLength = encodedValue.Length;

            if (type == typeof(bool)) {
                if (valueLength != 1) {
                    Logger.Warn($"Received save update with incorrect value length for bool: {valueLength}");
                }

                var value = encodedValue[0] == 1;

                pd.SetBoolInternal(name, value);
            } else if (type == typeof(float)) {
                if (valueLength != 4) {
                    Logger.Warn($"Received save update with incorrect value length for float: {valueLength}");
                }

                var value = BitConverter.ToSingle(encodedValue, 0);

                pd.SetFloatInternal(name, value);
            } else if (type == typeof(int)) {
                if (valueLength != 4) {
                    Logger.Warn($"Received save update with incorrect value length for int: {valueLength}");
                }

                var value = BitConverter.ToInt32(encodedValue, 0);

                pd.SetIntInternal(name, value);
            } else if (type == typeof(string)) {
                var sceneIndex = BitConverter.ToUInt16(encodedValue, 0);
                
                if (!EncodeUtil.GetSceneName(sceneIndex, out var value)) {
                    throw new Exception($"Could not decode string from save update: {encodedValue}");
                }

                pd.SetStringInternal(name, value);
            } else if (type == typeof(Vector3)) {
                if (valueLength != 12) {
                    Logger.Warn($"Received save update with incorrect value length for vector3: {valueLength}");
                }

                var value = new Vector3(
                    BitConverter.ToSingle(encodedValue, 0),
                    BitConverter.ToSingle(encodedValue, 4),
                    BitConverter.ToSingle(encodedValue, 8)
                );

                pd.SetVector3Internal(name, value);
            } else if (type == typeof(List<string>)) {
                var length = BitConverter.ToUInt16(encodedValue, 0);
                
                var list = new List<string>();
                for (var i = 0; i < length; i++) {
                    var sceneIndex = BitConverter.ToUInt16(encodedValue, 2 + i * 2);

                    if (!EncodeUtil.GetSceneName(sceneIndex, out var sceneName)) {
                        throw new Exception($"Could not decode string in list from save update: {sceneIndex}");
                    }
                    
                    list.Add(sceneName);
                }

                pd.SetVariableInternal(name, list);
            } else if (type == typeof(BossSequenceDoor.Completion)) {
                var byte1 = encodedValue[0];
                var byte2 = encodedValue[1];

                var bools = EncodeUtil.GetBoolsFromByte(byte1);

                var bsdComp = new BossSequenceDoor.Completion {
                    canUnlock = bools[0],
                    unlocked = bools[1],
                    completed = bools[2],
                    allBindings = bools[3],
                    noHits = bools[4],
                    boundNail = bools[5],
                    boundShell = bools[6],
                    boundCharms = bools[7],
                    boundSoul = byte2 == 1
                };

                pd.SetVariableInternal(name, bsdComp);
            } else if (type == typeof(BossStatue.Completion)) {
                var bools = EncodeUtil.GetBoolsFromByte(encodedValue[0]);

                var bsComp = new BossStatue.Completion {
                    hasBeenSeen = bools[0],
                    isUnlocked = bools[1],
                    completedTier1 = bools[2],
                    completedTier2 = bools[3],
                    completedTier3 = bools[4],
                    seenTier3Unlock = bools[5],
                    usingAltVersion = bools[6]
                };

                pd.SetVariableInternal(name, bsComp);
            } else {
                throw new NotImplementedException($"Could not decode type: {type}");
            }
        }
        
        if (SaveDataMapping.GeoRockDataIndices.TryGetValue(index, out var itemData)) {
            var value = encodedValue[0];
            
            Logger.Info($"Received geo rock save update: {itemData.Id}, {itemData.SceneName}, {value}");

            sceneData.SaveMyState(new GeoRockData {
                id = itemData.Id,
                sceneName = itemData.SceneName,
                hitsLeft = value
            });
        } else if (SaveDataMapping.PersistentBoolDataIndices.TryGetValue(index, out itemData)) {
            var value = encodedValue[0] == 1;
            
            Logger.Info($"Received persistent bool save update: {itemData.Id}, {itemData.SceneName}, {value}");

            sceneData.SaveMyState(new PersistentBoolData {
                id = itemData.Id,
                sceneName = itemData.SceneName,
                activated = value
            });
        } else if (SaveDataMapping.PersistentIntDataIndices.TryGetValue(index, out itemData)) {
            var value = (int) encodedValue[0];
            // Add a special case for the -1 value that some persistent ints might have
            // 255 is never used in the byte space, so we use it for compact networking
            if (value == 255) {
                value = -1;
            }
            
            Logger.Info($"Received persistent int save update: {itemData.Id}, {itemData.SceneName}, {value}");

            sceneData.SaveMyState(new PersistentIntData {
                id = itemData.Id,
                sceneName = itemData.SceneName,
                value = value
            });
        }
    }

    /// <summary>
    /// Get the current save data as a dictionary with mapped indices and encoded values.
    /// </summary>
    /// <returns>A dictionary with mapped indices and byte-encoded values.</returns>
    public static Dictionary<ushort, byte[]> GetCurrentSaveData() {
        var pd = PlayerData.instance;
        var sd = SceneData.instance;

        var saveData = new Dictionary<ushort, byte[]>();

        void AddToSaveData<TCollection, TLookup>(
            IEnumerable<TCollection> enumerable,
            Func<TCollection, TLookup> keyFunc,
            Dictionary<TLookup, bool> boolMapping,
            BiLookup<TLookup, ushort> indexMapping,
            Func<TCollection, object> valueFunc
        ) {
            foreach (var collectionValue in enumerable) {
                var key = keyFunc.Invoke(collectionValue);
                
                if (!boolMapping.TryGetValue(key, out var shouldSync) || !shouldSync) {
                    continue;
                }

                if (!indexMapping.TryGetValue(key, out var index)) {
                    continue;
                }

                var value = valueFunc.Invoke(collectionValue);
            
                saveData.Add(index, EncodeValue(value));
            }
        }
        
        AddToSaveData(
            typeof(PlayerData).GetFields(),
            fieldInfo => fieldInfo.Name,
            SaveDataMapping.PlayerDataBools,
            SaveDataMapping.PlayerDataIndices,
            fieldInfo => fieldInfo.GetValue(pd)
        );
        
        AddToSaveData(
            sd.geoRocks,
            geoRock => new PersistentItemData {
                Id = geoRock.id,
                SceneName = geoRock.sceneName
            },
            SaveDataMapping.GeoRockDataBools,
            SaveDataMapping.GeoRockDataIndices,
            geoRock => geoRock.hitsLeft
        );
        
        AddToSaveData(
            sd.persistentBoolItems,
            boolData => new PersistentItemData {
                Id = boolData.id,
                SceneName = boolData.sceneName
            },
            SaveDataMapping.PersistentBoolDataBools,
            SaveDataMapping.PersistentBoolDataIndices,
            boolData => boolData.activated
        );
        
        AddToSaveData(
            sd.persistentIntItems,
            intData => new PersistentItemData {
                Id = intData.id,
                SceneName = intData.sceneName
            },
            SaveDataMapping.PersistentIntDataBools,
            SaveDataMapping.PersistentIntDataIndices,
            intData => intData.value
        );

        return saveData;
    }
    
    /// <summary>
    /// Get the hash code of the combined values in a string list.
    /// </summary>
    /// <param name="list">The list of strings to calculate the hash code for.</param>
    /// <returns>0 if the list is empty, otherwise a hash code matching the specific order of strings in the list.
    /// </returns>
    private static int GetStringListHashCode(List<string> list) {
        if (list.Count == 0) {
            return 0;
        }
        
        return list
            .Select(item => item.GetHashCode())
            .Aggregate((total, nextCode) => total ^ nextCode);
    }
}
