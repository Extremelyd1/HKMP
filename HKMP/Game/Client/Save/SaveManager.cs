using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// The save data instances that contains mappings for what to sync and their indices.
    /// </summary>
    private SaveData _saveData;

    public SaveManager(NetClient netClient, PacketManager packetManager, EntityManager entityManager) {
        _netClient = netClient;
        _packetManager = packetManager;
        _entityManager = entityManager;

        _persistentFsmData = new List<PersistentFsmData>();
    }

    /// <summary>
    /// Initializes the save manager by loading the save data json.
    /// </summary>
    public void Initialize() {
        _saveData = FileUtil.LoadObjectFromEmbeddedJson<SaveData>(SaveDataFilePath);
        _saveData.Initialize();

        ModHooks.SetPlayerBoolHook += OnSetPlayerBoolHook;
        ModHooks.SetPlayerFloatHook += OnSetPlayerFloatHook;
        ModHooks.SetPlayerIntHook += OnSetPlayerIntHook;
        ModHooks.SetPlayerStringHook += OnSetPlayerStringHook;
        ModHooks.SetPlayerVariableHook += OnSetPlayerVariableHook;
        ModHooks.SetPlayerVector3Hook += OnSetPlayerVector3Hook;
        
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdate;
        
        _packetManager.RegisterClientPacketHandler<SaveUpdate>(ClientPacketId.SaveUpdate, UpdateSaveWithData);
    }

    /// <summary>
    /// Callback method for when a boolean is set in the player data.
    /// </summary>
    /// <param name="name">Name of the boolean variable.</param>
    /// <param name="orig">The original value of the boolean.</param>
    private bool OnSetPlayerBoolHook(string name, bool orig) {
        CheckSendSaveUpdate(name, () => {
            return new[] { (byte) (orig ? 0 : 1) };
        });

        return orig;
    }
    
    /// <summary>
    /// Callback method for when a float is set in the player data.
    /// </summary>
    /// <param name="name">Name of the float variable.</param>
    /// <param name="orig">The original value of the float.</param>
    private float OnSetPlayerFloatHook(string name, float orig) {
        CheckSendSaveUpdate(name, () => BitConverter.GetBytes(orig));

        return orig;
    }
    
    /// <summary>
    /// Callback method for when a int is set in the player data.
    /// </summary>
    /// <param name="name">Name of the int variable.</param>
    /// <param name="orig">The original value of the int.</param>
    private int OnSetPlayerIntHook(string name, int orig) {
        CheckSendSaveUpdate(name, () => BitConverter.GetBytes(orig));

        return orig;
    }

    /// <summary>
    /// Callback method for when a string is set in the player data.
    /// </summary>
    /// <param name="name">Name of the string variable.</param>
    /// <param name="res">The original value of the boolean.</param>
    private string OnSetPlayerStringHook(string name, string res) {
        CheckSendSaveUpdate(name, () => {
            var byteEncodedString = Encoding.UTF8.GetBytes(res);

            if (byteEncodedString.Length > ushort.MaxValue) {
                throw new Exception($"Could not encode string of length: {byteEncodedString.Length}");
            }

            var value = BitConverter.GetBytes((ushort) byteEncodedString.Length)
                .Concat(byteEncodedString)
                .ToArray();
            return value;
        });

        return res;
    }

    /// <summary>
    /// Callback method for when an object is set in the player data.
    /// </summary>
    /// <param name="type">The type of the object.</param>
    /// <param name="name">Name of the object variable.</param>
    /// <param name="value">The original value of the object.</param>
    private object OnSetPlayerVariableHook(Type type, string name, object value) {
        throw new NotImplementedException($"Object with type: {value.GetType()} could not be encoded");
    }

    /// <summary>
    /// Callback method for when a vector3 is set in the player data.
    /// </summary>
    /// <param name="name">Name of the vector3 variable.</param>
    /// <param name="orig">The original value of the vector3.</param>
    private Vector3 OnSetPlayerVector3Hook(string name, Vector3 orig) {
        CheckSendSaveUpdate(name, () => 
            BitConverter.GetBytes(orig.x)
                .Concat(BitConverter.GetBytes(orig.y))
                .Concat(BitConverter.GetBytes(orig.z))
                .ToArray()
        );
        
        return orig;
    }

    /// <summary>
    /// Checks if a save update should be sent and send it using the encode function to encode the value of the
    /// changed variable.
    /// </summary>
    /// <param name="name">The name of the variable that was changed.</param>
    /// <param name="encodeFunc">Function that encodes the value of the variable into a byte array.</param>
    private void CheckSendSaveUpdate(string name, Func<byte[]> encodeFunc) {
        if (!_entityManager.IsSceneHost) {
            Logger.Info($"Not scene host, not sending save update ({name})");
            return;
        }

        if (!_saveData.PlayerDataBools.TryGetValue(name, out var value) || !value) {
            Logger.Info($"Not in save data values or false in save data values, not sending save update ({name})");
            return;
        }

        if (!_saveData.PlayerDataIndices.TryGetValue(name, out var index)) {
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
    /// Called every unity update. Used to check for changes in the GeoRock/PersistentInt/PersistentBool FSMs.
    /// </summary>
    private void OnUpdate() {
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

                if (_saveData.GeoRockDataBools.TryGetValue(itemData, out var shouldSync) && shouldSync) {
                    if (!_saveData.GeoRockDataIndices.TryGetValue(itemData, out var index)) {
                        Logger.Info(
                            $"Cannot find geo rock save data index, not sending save update ({itemData.Id}, {itemData.SceneName})");
                        continue;
                    }

                    Logger.Info($"Sending geo rock ({itemData.Id}, {itemData.SceneName}) as save update");

                    _netClient.UpdateManager.SetSaveUpdate(
                        index,
                        new[] { (byte) value }
                    );
                } else if (_saveData.PersistentIntDataBools.TryGetValue(itemData, out shouldSync) && shouldSync) {
                    if (!_saveData.PersistentIntDataIndices.TryGetValue(itemData, out var index)) {
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
                
                if (!_saveData.PersistentBoolDataBools.TryGetValue(itemData, out var shouldSync) || !shouldSync) {
                    Logger.Info($"Not in persistent bool save data values or false in save data values, not sending save update ({itemData.Id}, {itemData.SceneName})");
                    continue;
                }

                if (!_saveData.PersistentBoolDataIndices.TryGetValue(itemData, out var index)) {
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
    /// Callback method for when a save update is received.
    /// </summary>
    /// <param name="saveUpdate">The save update that was received.</param>
    private void UpdateSaveWithData(SaveUpdate saveUpdate) {
        Logger.Info($"Received save update for index: {saveUpdate.SaveDataIndex}");
        
        var pd = PlayerData.instance;
        var sceneData = SceneData.instance;

        if (_saveData.PlayerDataIndices.TryGetValue(saveUpdate.SaveDataIndex, out var name)) {
            var fieldInfo = typeof(PlayerData).GetField(name);
            var type = fieldInfo.FieldType;
            var valueLength = saveUpdate.Value.Length;

            if (type == typeof(bool)) {
                if (valueLength != 1) {
                    Logger.Warn($"Received save update with incorrect value length for bool: {valueLength}");
                }

                var value = saveUpdate.Value[0] == 1;

                pd.SetBoolInternal(name, value);
            } else if (type == typeof(float)) {
                if (valueLength != 4) {
                    Logger.Warn($"Received save update with incorrect value length for float: {valueLength}");
                }

                var value = BitConverter.ToSingle(saveUpdate.Value, 0);

                pd.SetFloatInternal(name, value);
            } else if (type == typeof(int)) {
                if (valueLength != 4) {
                    Logger.Warn($"Received save update with incorrect value length for int: {valueLength}");
                }

                var value = BitConverter.ToInt32(saveUpdate.Value, 0);

                pd.SetIntInternal(name, value);
            } else if (type == typeof(string)) {
                var value = Encoding.UTF8.GetString(saveUpdate.Value);

                pd.SetStringInternal(name, value);
            } else if (type == typeof(Vector3)) {
                if (valueLength != 12) {
                    Logger.Warn($"Received save update with incorrect value length for vector3: {valueLength}");
                }

                var value = new Vector3(
                    BitConverter.ToSingle(saveUpdate.Value, 0),
                    BitConverter.ToSingle(saveUpdate.Value, 4),
                    BitConverter.ToSingle(saveUpdate.Value, 8)
                );

                pd.SetVector3Internal(name, value);
            }
        } else if (_saveData.GeoRockDataIndices.TryGetValue(saveUpdate.SaveDataIndex, out var itemData)) {
            var value = saveUpdate.Value[0];
            
            Logger.Info($"Received geo rock save update: {itemData.Id}, {itemData.SceneName}, {value}");

            sceneData.SaveMyState(new GeoRockData {
                id = itemData.Id,
                sceneName = itemData.SceneName,
                hitsLeft = value
            });
        } else if (_saveData.PersistentBoolDataIndices.TryGetValue(saveUpdate.SaveDataIndex, out itemData)) {
            var value = saveUpdate.Value[0] == 1;
            
            Logger.Info($"Received persistent bool save update: {itemData.Id}, {itemData.SceneName}, {value}");

            sceneData.SaveMyState(new PersistentBoolData {
                id = itemData.Id,
                sceneName = itemData.SceneName,
                activated = value
            });
        } else if (_saveData.PersistentIntDataIndices.TryGetValue(saveUpdate.SaveDataIndex, out itemData)) {
            var value = (int) saveUpdate.Value[0];
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
}
