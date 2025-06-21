using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GlobalEnums;
using Hkmp.Collection;
using Hkmp.Game.Client.Entity;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Serialization;
using Hkmp.Util;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Hkmp.Logging.Logger;
using MapZone = GlobalEnums.MapZone;
using Object = UnityEngine.Object;

namespace Hkmp.Game.Client.Save;

/// <summary>
/// Class that manages save data synchronisation.
/// </summary>
internal class SaveManager {
    /// <summary>
    /// The save data instance that contains mappings for what to sync and their indices.
    /// </summary>
    private static SaveDataMapping SaveDataMapping => SaveDataMapping.Instance;

    /// <summary>
    /// The net client instance to send save updates.
    /// </summary>
    private readonly NetClient _netClient;

    /// <summary>
    /// The entity manager to check whether we are scene host.
    /// </summary>
    private readonly EntityManager _entityManager;

    /// <summary>
    /// The save changes instance to apply immediate in-world changes from received save data.
    /// </summary>
    private readonly SaveChanges _saveChanges;

    /// <summary>
    /// List of data classes for each FSM that has a persistent int/bool or geo rock attached to it.
    /// </summary>
    private readonly List<PersistentFsmData> _persistentFsmData;

    /// <summary>
    /// Dictionary of BossSequenceDoor.Completion structs in the PlayerData for comparing changes against.
    /// </summary>
    private readonly Dictionary<string, BossSequenceDoor.Completion> _bsdCompHashes;

    /// <summary>
    /// Dictionary of BossStatue.Completion structs in the PlayerData for comparing changes against.
    /// </summary>
    private readonly Dictionary<string, BossStatue.Completion> _bsCompHashes;
    
    /// <summary>
    /// Dictionary of hash codes for list variables in the PlayerData for comparing changes against.
    /// </summary>
    private readonly Dictionary<string, int> _listHashes;

    /// <summary>
    /// List of FieldInfo for fields in PlayerData that are simple values that should be synced. Used for looping
    /// over to check for changes and network those changes.
    /// </summary>
    private readonly List<FieldInfo> _playerDataSimpleSyncFields;

    /// <summary>
    /// Dictionary of variable names mapped to FieldInfo for fields in PlayerData that are compound values that should
    /// be synced. Used for resetting the instance of PlayerData for last values and checking for updates to compound
    /// values.
    /// </summary>
    private readonly List<FieldInfo> _playerDataCompoundSyncFields;

    /// <summary>
    /// PlayerData instance that contains the last values of the currently used PlayerData for comparison checking.
    /// </summary>
    private PlayerData _lastPlayerData;

    /// <summary>
    /// Whether the player is hosting the server, which means that player specific save data is not networked
    /// to the server.
    /// </summary>
    public bool IsHostingServer { get; set; }
    
    public SaveManager(NetClient netClient, EntityManager entityManager) {
        _netClient = netClient;
        _entityManager = entityManager;
        _saveChanges = new SaveChanges();

        _persistentFsmData = [];
        _bsdCompHashes = new Dictionary<string, BossSequenceDoor.Completion>();
        _bsCompHashes = new Dictionary<string, BossStatue.Completion>();
        _listHashes = new Dictionary<string, int>();
        _playerDataSimpleSyncFields = [];
        _playerDataCompoundSyncFields = [];
    }

    /// <summary>
    /// Initializes the save manager by loading the save data json.
    /// </summary>
    public void Initialize() {
        _netClient.ConnectEvent += _ => OnConnect();

        foreach (var field in typeof(PlayerData).GetFields()) {
            var fieldName = field.Name;

            if (!SaveDataMapping.PlayerDataVarProperties.TryGetValue(fieldName, out var varProps) 
                || !varProps.Sync
            ) {
                continue;
            }
            
            var compoundField = SaveDataMapping.StringListVariables.Contains(fieldName) ||
                                SaveDataMapping.BossSequenceDoorCompletionVariables.Contains(fieldName) ||
                                SaveDataMapping.BossStatueCompletionVariables.Contains(fieldName) ||
                                SaveDataMapping.VectorListVariables.Contains(fieldName) ||
                                SaveDataMapping.IntListVariables.Contains(fieldName);

            if (compoundField) {
                _playerDataCompoundSyncFields.Add(field);
            } else {
                _playerDataSimpleSyncFields.Add(field);
            }
        }
    }

    /// <summary>
    /// Register the relevant hooks for save-related operations.
    /// </summary>
    public void RegisterHooks() {
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;

        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdatePlayerData;
        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdatePersistents;
        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdateCompounds;
    }

    /// <summary>
    /// Deregister the relevant hooks for save-related operations.
    /// </summary>
    public void DeregisterHooks() {
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnSceneChanged;

        MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdatePlayerData;
        MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdatePersistents;
        MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdateCompounds;
    }

    /// <summary>
    /// Callback method for when the player connects to a server, so we can reset the player data.
    /// </summary>
    private void OnConnect() {
        ResetLastPlayerData();
    }

    /// <summary>
    /// Resets the PlayerData instance that stores the last values of all synchronised fields.
    /// </summary>
    private void ResetLastPlayerData() {
        var pd = PlayerData.instance;

        var pdConstructor = typeof(PlayerData).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance,
            null,
            [],
            null
        );
        if (pdConstructor == null) {
            Logger.Error("Could not find protected constructor of PlayerData");
            return;
        }
        
        _lastPlayerData = (PlayerData) pdConstructor.Invoke([]);

        foreach (var field in _playerDataSimpleSyncFields) {
            var value = field.GetValue(pd);
            field.SetValue(_lastPlayerData, value);
        }

        foreach (var field in _playerDataCompoundSyncFields) {
            var value = field.GetValue(pd);
            field.SetValue(_lastPlayerData, GetCompoundCopy(value));
        }
    }
    
    /// <summary>
    /// Update hook to check for changes in the PlayerData instance.
    /// </summary>
    private void OnUpdatePlayerData() {
        var pd = PlayerData.instance;
        if (_lastPlayerData == null) {
            return;
        }

        var gm = global::GameManager.instance;
        if (!gm) {
            return;
        }

        if (gm.gameState == GameState.MAIN_MENU) {
            return;
        }

        foreach (var field in _playerDataSimpleSyncFields) {
            var currentValue = field.GetValue(pd);
            var lastValue = field.GetValue(_lastPlayerData);

            if (currentValue.Equals(lastValue)) {
                continue;
            }

            Logger.Debug($"PlayerData value changed from: {lastValue} to {currentValue}");

            field.SetValue(_lastPlayerData, currentValue);

            if (field.FieldType == typeof(int)) {
                CheckSendSaveUpdate(
                    field.Name, 
                    () => EncodeSaveDataValue(currentValue), 
                    () => {
                        var delta = (int) currentValue - (int) lastValue;
                        return EncodeSaveDataValue(delta);
                    }
                );
            } else {
                CheckSendSaveUpdate(field.Name, () => EncodeSaveDataValue(currentValue));
            }
        }
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

            var persistentItemData = new PersistentItemKey {
                Id = geoRockObject.name,
                SceneName = global::GameManager.GetBaseSceneName(geoRockObject.scene.name)
            };

            Logger.Info($"Found Geo Rock in scene: {persistentItemData}");

            var fsm = geoRock.GetComponent<PlayMakerFSM>();
            if (!fsm) {
                Logger.Info("  Could not find FSM belonging to Geo Rock object, skipping");
                continue;
            }

            var fsmInt = fsm.FsmVariables.GetFsmInt("Hits");

            var persistentFsmData = new PersistentFsmData {
                PersistentItemKey = persistentItemData,
                GetCurrentInt = () => fsmInt.Value,
                SetCurrentInt = value => fsmInt.Value = value,
                LastIntValue = fsmInt.Value
            };

            _persistentFsmData.Add(persistentFsmData);
        }

        foreach (var persistentBoolItem in Object.FindObjectsOfType<PersistentBoolItem>()) {
            var itemObject = persistentBoolItem.gameObject;

            if (itemObject.scene != newScene) {
                continue;
            }

            var persistentItemData = new PersistentItemKey {
                Id = itemObject.name,
                SceneName = global::GameManager.GetBaseSceneName(itemObject.scene.name)
            };

            Logger.Info($"Found persistent bool in scene: {persistentItemData}");

            Func<bool> getCurrentBoolFunc = null;
            Action<bool> setCurrentBoolAction = null;

            var fsm = FSMUtility.FindFSMWithPersistentBool(itemObject.GetComponents<PlayMakerFSM>());
            if (fsm) {
                var fsmBool = fsm.FsmVariables.GetFsmBool("Activated");
                getCurrentBoolFunc = () => fsmBool.Value;
                setCurrentBoolAction = value => fsmBool.Value = value;
            }

            var vinePlatform = itemObject.GetComponent<VinePlatform>();
            if (vinePlatform) {
                getCurrentBoolFunc = () => ReflectionHelper.GetField<VinePlatform, bool>(vinePlatform, "activated");
                setCurrentBoolAction = value => ReflectionHelper.SetField(vinePlatform, "activated", value);
            }

            var breakable = itemObject.GetComponent<Breakable>();
            if (breakable) {
                getCurrentBoolFunc = () => ReflectionHelper.GetField<Breakable, bool>(breakable, "isBroken");
                setCurrentBoolAction = value => ReflectionHelper.SetField(breakable, "isBroken", value);
            }

            if (getCurrentBoolFunc == null) {
                continue;
            }
            
            var persistentFsmData = new PersistentFsmData {
                PersistentItemKey = persistentItemData,
                GetCurrentBool = getCurrentBoolFunc,
                SetCurrentBool = setCurrentBoolAction,
                LastBoolValue = getCurrentBoolFunc.Invoke()
            };

            _persistentFsmData.Add(persistentFsmData);
        }

        foreach (var persistentIntItem in Object.FindObjectsOfType<PersistentIntItem>()) {
            var itemObject = persistentIntItem.gameObject;

            if (itemObject.scene != newScene) {
                continue;
            }

            var persistentItemData = new PersistentItemKey {
                Id = itemObject.name,
                SceneName = global::GameManager.GetBaseSceneName(itemObject.scene.name)
            };

            Logger.Info($"Found persistent int in scene: {persistentItemData}");

            var fsm = FSMUtility.FindFSMWithPersistentBool(itemObject.GetComponents<PlayMakerFSM>());
            if (!fsm) {
                Logger.Info("  Could not find FSM belonging to persistent int object, skipping");
                continue;
            }

            var fsmInt = fsm.FsmVariables.GetFsmInt("Value");

            var persistentFsmData = new PersistentFsmData {
                PersistentItemKey = persistentItemData,
                GetCurrentInt = () => fsmInt.Value,
                SetCurrentInt = value => fsmInt.Value = value,
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
    /// <param name="deltaEncodeFunc">Function to encode the delta value of the variable of the type is applicable.
    /// </param>
    private void CheckSendSaveUpdate(string name, Func<byte[]> encodeFunc, Func<byte[]> deltaEncodeFunc = null) {
        // If we are not connected or the 'permadeathMode' is 2, meaning we have broken/lost Steel Soul
        if (!_netClient.IsConnected || PlayerData.instance.GetInt("permadeathMode") == 2) {
            return;
        }
        
        if (!SaveDataMapping.PlayerDataVarProperties.TryGetValue(name, out var varProps)) {
            Logger.Info($"Not in save data values, not sending save update ({name})");
            return;
        }

        if (!varProps.Sync) {
            Logger.Info($"Value should not sync, not sending save update ({name})");
            return;
        }
        
        // If we should do the scene host check and the player is not scene host, skip sending
        if (!varProps.IgnoreSceneHost && !_entityManager.IsSceneHost) {
            Logger.Info($"Not scene host, but required, not sending save update ({name})");
            return;
        }

        if (!SaveDataMapping.PlayerDataIndices.TryGetValue(name, out var index)) {
            Logger.Info($"Cannot find save data index, not sending save update ({name})");
            return;
        }

        Func<byte[]> toUseEncodeFunc;
        if (varProps.Additive && deltaEncodeFunc != null) {
            toUseEncodeFunc = deltaEncodeFunc;
            
            Logger.Debug($"Sending \"{name}\" as save update (additive)");
        } else {
            toUseEncodeFunc = encodeFunc;
            
            Logger.Debug($"Sending \"{name}\" as save update");
        }

        _netClient.UpdateManager.SetSaveUpdate(
            index,
            toUseEncodeFunc.Invoke()
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
                var value = persistentFsmData.GetCurrentInt.Invoke();
                if (value == persistentFsmData.LastIntValue) {
                    continue;
                }

                persistentFsmData.LastIntValue = value;

                var itemData = persistentFsmData.PersistentItemKey;

                Logger.Info($"Value for {itemData} changed to: {value}");

                if (!_netClient.IsConnected) {
                    continue;
                }

                if (SaveDataMapping.GeoRockBools.TryGetValue(itemData, out var shouldSync) && shouldSync) {
                    if (!_entityManager.IsSceneHost) {
                        Logger.Info(
                            $"Not scene host, not sending geo rock save update ({itemData.Id}, {itemData.SceneName})");
                        continue;
                    }
                    
                    if (!SaveDataMapping.GeoRockIndices.TryGetValue(itemData, out var index)) {
                        Logger.Info(
                            $"Cannot find geo rock save data index, not sending save update ({itemData.Id}, {itemData.SceneName})");
                        continue;
                    }

                    Logger.Info($"Sending geo rock ({itemData.Id}, {itemData.SceneName}) as save update");

                    _netClient.UpdateManager.SetSaveUpdate(
                        index,
                        [(byte) value]
                    );
                } else if (
                    SaveDataMapping.PersistentIntVarProperties.TryGetValue(itemData, out var varProps) && 
                    varProps.Sync
                ) {
                    // If we should do the scene host check and the player is not scene host, skip sending
                    if (!varProps.IgnoreSceneHost && !_entityManager.IsSceneHost) {
                        Logger.Info(
                            $"Not scene host, not sending persistent int save update ({itemData.Id}, {itemData.SceneName})");
                        continue;
                    }

                    if (!SaveDataMapping.PersistentIntIndices.TryGetValue(itemData, out var index)) {
                        Logger.Info(
                            $"Cannot find persistent int save data index, not sending save update ({itemData.Id}, {itemData.SceneName})");
                        continue;
                    }

                    Logger.Info($"Sending persistent int ({itemData.Id}, {itemData.SceneName}) as save update");

                    _netClient.UpdateManager.SetSaveUpdate(
                        index,
                        [(byte) value]
                    );
                } else {
                    Logger.Info("Cannot find persistent int/geo rock data bool, not sending save update");
                }
            } else {
                var value = persistentFsmData.GetCurrentBool.Invoke();
                if (value == persistentFsmData.LastBoolValue) {
                    continue;
                }

                persistentFsmData.LastBoolValue = value;

                var itemData = persistentFsmData.PersistentItemKey;

                Logger.Info($"Value for {itemData} changed to: {value}");
                
                if (!_netClient.IsConnected) {
                    continue;
                }

                if (!SaveDataMapping.PersistentBoolVarProperties.TryGetValue(itemData, out var varProps) ||
                    !varProps.Sync) {
                    Logger.Info(
                        $"Not in persistent bool save data values or false in sync props, not sending save update ({itemData.Id}, {itemData.SceneName})");
                    continue;
                }
                
                // If we should do the scene host check and the player is not scene host, skip sending
                if (!varProps.IgnoreSceneHost && !_entityManager.IsSceneHost) {
                    Logger.Info(
                        $"Not scene host, not sending persistent bool save update ({itemData.Id}, {itemData.SceneName})");
                    continue;
                }

                if (!SaveDataMapping.PersistentBoolIndices.TryGetValue(itemData, out var index)) {
                    Logger.Info(
                        $"Cannot find persistent bool save data index, not sending save update ({itemData.Id}, {itemData.SceneName})");
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
            Func<TCheck, TCheck, bool> changeFunc,
            Func<object, object, byte[]> deltaEncodeFunc = null
        ) {
            foreach (var varName in variableNames) {
                // Get current value from player data based on the variable name
                var currentValue = (TVar) typeof(PlayerData).GetField(varName).GetValue(PlayerData.instance);
                // Get the value with which to check whether this current value is new or not
                // In some cases this is the same value, in others this is a hash of the value
                var currentCheckValue = newCheckFunc.Invoke(currentValue);

                // Check if the dictionary contains the last value to check against
                if (!checkDict.TryGetValue(varName, out var lastCheckValue)) {
                    // If not, we put the value in the dictionary and continue
                    checkDict[varName] = currentCheckValue;
                    continue;
                }

                // Invoke the change function to check whether there is a difference between the current and last value
                if (!changeFunc(currentCheckValue, lastCheckValue)) {
                    continue;
                }

                Logger.Debug($"Compound variable ({varName}) changed value");

                // Since the value changed, we update it in the dictionary
                checkDict[varName] = currentCheckValue;

                if (deltaEncodeFunc == null) {
                    CheckSendSaveUpdate(varName, () => EncodeSaveDataValue(currentValue));
                } else {
                    var lastValue = _lastPlayerData.GetVariableInternal<TVar>(varName);

                    CheckSendSaveUpdate(
                        varName,
                        () => EncodeSaveDataValue(currentValue),
                        () => deltaEncodeFunc.Invoke(currentValue, lastValue)
                    );

                    // Also update the current value in the PlayerData instance for last values
                    // We copy the value, because otherwise it will be updated whenever the list is updated
                    _lastPlayerData.SetVariableInternal(varName, (TVar) GetCompoundCopy(currentValue));
                }
            }
        }

        CheckUpdates<List<string>, int>(
            SaveDataMapping.StringListVariables,
            _listHashes,
            GetListHashCode,
            (hash1, hash2) => hash1 != hash2,
            (currentValue, lastValue) => {
                var currentList = currentValue as List<string>;
                var lastList = lastValue as List<string>;

                // TODO: also allow for negative updates, where something is deleted from the list
                // this also holds for the other two lambdas below
                var deltaList = currentList!.Except(lastList!).ToList();
                
                Logger.Debug($"String list var updated, currentList: {string.Join(", ", currentList)}, lastList: {string.Join(", ", lastList)}, deltaList: {string.Join(", ", deltaList)}");

                return EncodeSaveDataValue(deltaList);
            }
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
        
        CheckUpdates<List<Vector3>, int>(
            SaveDataMapping.VectorListVariables,
            _listHashes,
            GetListHashCode,
            (hash1, hash2) => hash1 != hash2,
            (currentValue, lastValue) => {
                var currentList = currentValue as List<Vector3>;
                var lastList = lastValue as List<Vector3>;

                var deltaList = currentList!.Except(lastList!).ToList();
                
                Logger.Debug($"Vector3 list var updated, currentList: {string.Join(", ", currentList)}, lastList: {string.Join(", ", lastList)}, deltaList: {string.Join(", ", deltaList)}");

                return EncodeSaveDataValue(deltaList);
            }
        );
        
        CheckUpdates<List<int>, int>(
            SaveDataMapping.IntListVariables,
            _listHashes,
            GetListHashCode,
            (hash1, hash2) => hash1 != hash2,
            (currentValue, lastValue) => {
                var currentList = currentValue as List<int>;
                var lastList = lastValue as List<int>;

                var deltaList = currentList!.Except(lastList!).ToList();
                
                Logger.Debug($"Integer list var updated, currentList: {string.Join(", ", currentList)}, lastList: {string.Join(", ", lastList)}, deltaList: {string.Join(", ", deltaList)}");

                return EncodeSaveDataValue(deltaList);
            }
        );
    }

    /// <summary>
    /// Callback method for when a save update is received.
    /// </summary>
    /// <param name="saveUpdate">The save update that was received.</param>
    public void UpdateSaveWithData(SaveUpdate saveUpdate) {
        Logger.Info($"Received save update for index: {saveUpdate.SaveDataIndex}");

        var index = saveUpdate.SaveDataIndex;
        var value = saveUpdate.Value;

        UpdateSaveWithData(index, value);
    }

    /// <summary>
    /// Set the save data from the given CurrentSave by overriding all values.
    /// </summary>
    /// <param name="currentSave">The save data to set.</param>
    public void SetSaveWithData(CurrentSave currentSave) {
        if (IsHostingServer) {
            Logger.Info("Received current save, but player is hosting, not updating");
            return;
        }
        
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
            if (CheckPlayerSpecificHosting(SaveDataMapping.PlayerDataVarProperties, name)) {
                return;
            }

            Logger.Info($"Received save update ({index}, {name})");

            var decodedObject = DecodeSaveDataValue(name, encodedValue);

            if (decodedObject is bool decodedBool) {
                _lastPlayerData?.SetBoolInternal(name, decodedBool);
                pd.SetBoolInternal(name, decodedBool);
            } else if (decodedObject is float decodedFloat) {
                _lastPlayerData?.SetFloatInternal(name, decodedFloat);
                pd.SetFloatInternal(name, decodedFloat);
            } else if (decodedObject is int decodedInt) {
                _lastPlayerData?.SetIntInternal(name, decodedInt);
                pd.SetIntInternal(name, decodedInt);
            } else if (decodedObject is string decodedString) {
                _lastPlayerData?.SetStringInternal(name, decodedString);
                pd.SetStringInternal(name, decodedString);
            } else if (decodedObject is Vector3 decodedVec3) {
                _lastPlayerData?.SetVector3Internal(name, decodedVec3);
                pd.SetVector3Internal(name, decodedVec3);
            } else if (decodedObject is List<string> decodedStringList) {
                // First set the new string list hash, so we don't trigger an update and subsequently a feedback loop
                _listHashes[name] = GetListHashCode(decodedStringList);
                _lastPlayerData?.SetVariableInternal(name, (List<string>) GetCompoundCopy(decodedStringList));
                pd.SetVariableInternal(name, decodedStringList);
            } else if (decodedObject is BossSequenceDoor.Completion decodedBsdComp) {
                // First set the new bsdComp obj in the dict, so we don't trigger an update and subsequently a
                // feedback loop
                _bsdCompHashes[name] = decodedBsdComp;
                _lastPlayerData?.SetVariableInternal(name, (BossSequenceDoor.Completion) GetCompoundCopy(decodedBsdComp));
                pd.SetVariableInternal(name, decodedBsdComp);
            } else if (decodedObject is BossStatue.Completion decodedBsComp) {
                // First set the new bsComp obj in the dict, so we don't trigger an update and subsequently a
                // feedback loop
                _bsCompHashes[name] = decodedBsComp;
                _lastPlayerData?.SetVariableInternal(name, (BossStatue.Completion) GetCompoundCopy(decodedBsComp));
                pd.SetVariableInternal(name, decodedBsComp);
            } else if (decodedObject is List<Vector3> decodedVec3List) {
                // First set the new string list hash, so we don't trigger an update and subsequently a feedback loop
                _listHashes[name] = GetListHashCode(decodedVec3List);
                _lastPlayerData?.SetVariableInternal(name, (List<Vector3>) GetCompoundCopy(decodedVec3List));
                pd.SetVariableInternal(name, decodedVec3List);
            } else if (decodedObject is MapZone decodedMapZone) {
                _lastPlayerData?.SetVariableInternal(name, decodedMapZone);
                pd.SetVariableInternal(name, decodedMapZone);
            } else if (decodedObject is List<int> decodedIntList) {
                // First set the new string list hash, so we don't trigger an update and subsequently a feedback loop
                _listHashes[name] = GetListHashCode(decodedIntList);
                _lastPlayerData?.SetVariableInternal(name, (List<int>) GetCompoundCopy(decodedIntList));
                pd.SetVariableInternal(name, decodedIntList);
            } else {
                throw new ArgumentException($"Could not decode type: {decodedObject.GetType()}");
            }
            
            _saveChanges.ApplyPlayerDataSaveChange(name);
        }

        if (SaveDataMapping.GeoRockIndices.TryGetValue(index, out var itemData)) {
            var value = encodedValue[0];

            Logger.Info($"Received geo rock save update: {itemData.Id}, {itemData.SceneName}, {value}");

            foreach (var persistentFsmData in _persistentFsmData) {
                var existingItemData = persistentFsmData.PersistentItemKey;

                if (existingItemData.Id == itemData.Id && existingItemData.SceneName == itemData.SceneName) {
                    persistentFsmData.SetCurrentInt.Invoke(value);
                    persistentFsmData.LastIntValue = value;
                }
            }

            sceneData.SaveMyState(new GeoRockData {
                id = itemData.Id,
                sceneName = itemData.SceneName,
                hitsLeft = value
            });
        } else if (SaveDataMapping.PersistentBoolIndices.TryGetValue(index, out itemData)) {
            if (CheckPlayerSpecificHosting(SaveDataMapping.PersistentBoolVarProperties, itemData)) {
                return;
            }

            var value = encodedValue[0] == 1;

            Logger.Info($"Received persistent bool save update: {itemData.Id}, {itemData.SceneName}, {value}");

            foreach (var persistentFsmData in _persistentFsmData) {
                var existingItemData = persistentFsmData.PersistentItemKey;

                if (existingItemData.Id == itemData.Id && existingItemData.SceneName == itemData.SceneName) {
                    Logger.Debug($"Setting last bool value for {existingItemData} to {value}");
                    persistentFsmData.SetCurrentBool.Invoke(value);
                    persistentFsmData.LastBoolValue = value;
                }
            }

            sceneData.SaveMyState(new PersistentBoolData {
                id = itemData.Id,
                sceneName = itemData.SceneName,
                activated = value
            });

            _saveChanges.ApplyPersistentValueSaveChange(itemData);
        } else if (SaveDataMapping.PersistentIntIndices.TryGetValue(index, out itemData)) {
            if (CheckPlayerSpecificHosting(SaveDataMapping.PersistentIntVarProperties, itemData)) {
                return;
            }

            var value = (int) encodedValue[0];
            // Add a special case for the -1 value that some persistent ints might have
            // 255 is never used in the byte space, so we use it for compact networking
            if (value == 255) {
                value = -1;
            }

            Logger.Info($"Received persistent int save update: {itemData.Id}, {itemData.SceneName}, {value}");

            foreach (var persistentFsmData in _persistentFsmData) {
                var existingItemData = persistentFsmData.PersistentItemKey;

                if (existingItemData.Id == itemData.Id && existingItemData.SceneName == itemData.SceneName) {
                    persistentFsmData.SetCurrentInt.Invoke(value);
                    persistentFsmData.LastIntValue = value;
                }
            }

            sceneData.SaveMyState(new PersistentIntData {
                id = itemData.Id,
                sceneName = itemData.SceneName,
                value = value
            });
            
            _saveChanges.ApplyPersistentValueSaveChange(itemData);
        }

        // Do the checks for whether the player is hosting and the received save data is player specific and should
        // thus be ignored. Returns true if the data should be ignored, false otherwise.
        bool CheckPlayerSpecificHosting<TKey>(Dictionary<TKey, SaveDataMapping.VarProperties> dict, TKey value) {
            if (!IsHostingServer) {
                return false;
            }

            if (!dict.TryGetValue(value, out var varProps)) {
                return true;
            }

            if (varProps.SyncType != SaveDataMapping.SyncType.Player) {
                return false;
            }

            Logger.Info($"Received player specific save update ({index}, {name}), but player is hosting");
            return true;
        }
    }

    /// <summary>
    /// Encode a save data value by first recasting HK/Unity internal types to HKMP types and then using the EncodeUtil.
    /// </summary>
    /// <param name="value">The object to encode, which should be part of save data.</param>
    /// <returns>A byte array containing the encoded data.</returns>
    private static byte[] EncodeSaveDataValue(object value) {
        // First cast HK or Unity internal types to HKMP types, this is to make sure we can use our internal
        // EncodeUtil to encode all types. This util is also used on the server side, where (in the case of the
        // standalone server) we have no reference of HK or Unity internal types
        if (value is Vector3 vector3) {
            value = (Math.Vector3) vector3;
        } else if (value is MapZone mapZone) {
            value = (Serialization.MapZone) mapZone;
        } else if (value is BossStatue.Completion bsCompletion) {
            value = (BossStatueCompletion) bsCompletion;
        } else if (value is BossSequenceDoor.Completion bsdCompletion) {
            value = (BossSequenceDoorCompletion) bsdCompletion;
        } else if (value is List<Vector3> vector3List) {
            value = vector3List.Select(v => (Math.Vector3) v).ToList();
        }

        return EncodeUtil.EncodeSaveDataValue(value);
    }

    /// <summary>
    /// Decode a save data value by first using the EncodeUtil and then recasting HKMP types to HK/Unity internal types. 
    /// </summary>
    /// <param name="name">The name of the save data variable.</param>
    /// <param name="encodedValue">A byte array containing the encoded data.</param>
    /// <returns>The decoded object.</returns>
    private static object DecodeSaveDataValue(string name, byte[] encodedValue) {
        var decodedValue = EncodeUtil.DecodeSaveDataValue(name, encodedValue);
        
        // Now we cast HKMP types to HK or Unity internal types, this is to make sure we can use our internal
        // EncodeUtil to decode all types. This util is also used on the server side, where (in the case of the
        // standalone server) we have no reference of HK or Unity internal types
        if (decodedValue is Math.Vector3 vector3) {
            decodedValue = (Vector3) vector3;
        } else if (decodedValue is Serialization.MapZone mapZone) {
            decodedValue = (MapZone) mapZone;
        } else if (decodedValue is BossStatueCompletion bsCompletion) {
            decodedValue = (BossStatue.Completion) bsCompletion;
        } else if (decodedValue is BossSequenceDoorCompletion bsdCompletion) {
            decodedValue = (BossSequenceDoor.Completion) bsdCompletion;
        } else if (decodedValue is List<Math.Vector3> vector3List) {
            decodedValue = vector3List.Select(v => (Vector3) v).ToList();
        }

        return decodedValue;
    }

    /// <summary>
    /// Get the current save data as a dictionary with mapped indices and encoded values. This returns the global save
    /// data if the <paramref name="server"/> boolean is set. Otherwise, it returns the player save data.
    /// </summary>
    /// <returns>A dictionary with mapped indices and byte-encoded values.</returns>
    public static Dictionary<ushort, byte[]> GetCurrentSaveData(bool server) {
        var pd = PlayerData.instance;
        var sd = SceneData.instance;

        var saveData = new Dictionary<ushort, byte[]>();

        void AddToSaveData<TCollection, TLookup>(
            IEnumerable<TCollection> enumerable,
            Func<TCollection, TLookup> keyFunc,
            object syncMapping,
            BiLookup<TLookup, ushort> indexMapping,
            Func<TCollection, object> valueFunc
        ) {
            foreach (var collectionValue in enumerable) {
                var key = keyFunc.Invoke(collectionValue);

                if (syncMapping is Dictionary<TLookup, bool> boolMapping) {
                    if (!boolMapping.TryGetValue(key, out var shouldSync) || !shouldSync) {
                        continue;
                    }

                    // Since all geo rocks are server data, we need to check whether we are actually trying to get
                    // server data or not and continue appropriately
                    if (!server) {
                        continue;
                    }
                } else if (syncMapping is Dictionary<TLookup, SaveDataMapping.VarProperties> syncPropMapping) {
                    if (!syncPropMapping.TryGetValue(key, out var varProps)) {
                        continue;
                    }

                    // Skip values that are not supposed to be synced, or ones that have the property that it is
                    // server data. Since we will not require the hosting player's save data on the server.
                    if (!varProps.Sync) {
                        continue;
                    }

                    // Check whether the sync type corresponds with the server parameter. If it is server data, but
                    // we are trying to get player data, we continue
                    if ((varProps.SyncType == SaveDataMapping.SyncType.Server) != server) {
                        continue;
                    }
                }

                if (!indexMapping.TryGetValue(key, out var index)) {
                    continue;
                }

                var value = valueFunc.Invoke(collectionValue);

                saveData.Add(index, EncodeSaveDataValue(value));
            }
        }

        AddToSaveData(
            typeof(PlayerData).GetFields(),
            fieldInfo => fieldInfo.Name,
            SaveDataMapping.PlayerDataVarProperties,
            SaveDataMapping.PlayerDataIndices,
            fieldInfo => fieldInfo.GetValue(pd)
        );

        AddToSaveData(
            sd.geoRocks,
            geoRock => new PersistentItemKey {
                Id = geoRock.id,
                SceneName = geoRock.sceneName
            },
            SaveDataMapping.GeoRockBools,
            SaveDataMapping.GeoRockIndices,
            geoRock => geoRock.hitsLeft
        );

        AddToSaveData(
            sd.persistentBoolItems,
            boolData => new PersistentItemKey {
                Id = boolData.id,
                SceneName = boolData.sceneName
            },
            SaveDataMapping.PersistentBoolVarProperties,
            SaveDataMapping.PersistentBoolIndices,
            boolData => boolData.activated
        );

        AddToSaveData(
            sd.persistentIntItems,
            intData => new PersistentItemKey {
                Id = intData.id,
                SceneName = intData.sceneName
            },
            SaveDataMapping.PersistentIntVarProperties,
            SaveDataMapping.PersistentIntIndices,
            intData => intData.value
        );
        
        return saveData;
    }

    /// <summary>
    /// Get the hash code of the combined values in a list.
    /// </summary>
    /// <param name="list">The list to calculate the hash code for.</param>
    /// <returns>0 if the list is empty, otherwise a hash code matching the specific order of values in the list.
    /// </returns>
    private static int GetListHashCode<T>(List<T> list) {
        if (list.Count == 0) {
            return 0;
        }

        return list
            .Select(item => item.GetHashCode())
            .Aggregate((total, nextCode) => total ^ nextCode);
    }

    /// <summary>
    /// Get a copy of the given object for compound objects in the PlayerData, such as string lists, integer lists,
    /// completion for boss sequences or boss doors, etc.
    /// </summary>
    /// <param name="value">The object value to get a copy from.</param>
    /// <returns>The copy of the given value.</returns>
    /// <exception cref="ArgumentException">Thrown when a copy cannot be made, due to the given value being null or
    /// of a non-compound or non-PlayerData type.</exception>
    private static object GetCompoundCopy(object value) {
        if (value == null) {
            throw new ArgumentException("Cannot get copy of null");
        }
        
        if (value is List<string> stringListValue) {
            return new List<string>(stringListValue);
        }

        if (value is List<int> intListValue) {
            return new List<int>(intListValue);
        }
        
        if (value is List<Vector3> vecListValue) {
            return new List<Vector3>(vecListValue);
        }

        if (value is BossSequenceDoor.Completion bsdComp) {
            return new BossSequenceDoor.Completion {
                canUnlock = bsdComp.canUnlock,
                unlocked = bsdComp.unlocked,
                completed = bsdComp.completed,
                allBindings = bsdComp.allBindings,
                noHits = bsdComp.noHits,
                boundNail = bsdComp.boundNail,
                boundShell = bsdComp.boundShell,
                boundCharms = bsdComp.boundCharms,
                boundSoul = bsdComp.boundSoul,
                viewedBossSceneCompletions = bsdComp.viewedBossSceneCompletions == null 
                    ? [] 
                    : [..bsdComp.viewedBossSceneCompletions]
            };
        }

        if (value is BossStatue.Completion bsComp) {
            return new BossStatue.Completion {
                hasBeenSeen = bsComp.hasBeenSeen,
                isUnlocked = bsComp.isUnlocked,
                completedTier1 = bsComp.completedTier1,
                completedTier2 = bsComp.completedTier2,
                completedTier3 = bsComp.completedTier3,
                seenTier3Unlock = bsComp.seenTier3Unlock,
                usingAltVersion = bsComp.usingAltVersion
            };
        }

        throw new ArgumentException($"Cannot get copy of value with type: {value.GetType()}");
    }
}
