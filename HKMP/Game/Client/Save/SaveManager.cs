using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hkmp.Collection;
using Hkmp.Game.Client.Entity;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using Modding;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

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
    /// Dictionary mapping save data values to booleans indicating whether they should be synchronised.
    /// </summary>
    private Dictionary<string, bool> _saveDataValues;

    /// <summary>
    /// Bi-directional lookup that maps save data names and their indices.
    /// </summary>
    private BiLookup<string, ushort> _saveDataIndices;

    public SaveManager(NetClient netClient, PacketManager packetManager, EntityManager entityManager) {
        _netClient = netClient;
        _packetManager = packetManager;
        _entityManager = entityManager;
    }

    /// <summary>
    /// Initializes the save manager by loading the save data json.
    /// </summary>
    public void Initialize() {
        _saveDataValues = FileUtil.LoadObjectFromEmbeddedJson<Dictionary<string, bool>>(SaveDataFilePath);
        if (_saveDataValues == null) {
            Logger.Warn("Could not load save data json");
            return;
        }

        _saveDataIndices = new BiLookup<string, ushort>();
        ushort index = 0;
        foreach (var saveDataName in _saveDataValues.Keys) {
            Logger.Info($"Saving ({saveDataName}, {index}) in bi-lookup");
            _saveDataIndices.Add(saveDataName, index++);
        }
        
        ModHooks.SetPlayerBoolHook += OnSetPlayerBoolHook;
        ModHooks.SetPlayerFloatHook += OnSetPlayerFloatHook;
        ModHooks.SetPlayerIntHook += OnSetPlayerIntHook;
        ModHooks.SetPlayerStringHook += OnSetPlayerStringHook;
        ModHooks.SetPlayerVariableHook += OnSetPlayerVariableHook;
        ModHooks.SetPlayerVector3Hook += OnSetPlayerVector3Hook;
        
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

        if (!_saveDataValues.TryGetValue(name, out var value) || !value) {
            Logger.Info($"Not in save data values or false in save data values, not sending save update ({name})");
            return;
        }

        if (!_saveDataIndices.TryGetValue(name, out var index)) {
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
    /// Callback method for when a save update is received.
    /// </summary>
    /// <param name="saveUpdate">The save update that was received.</param>
    private void UpdateSaveWithData(SaveUpdate saveUpdate) {
        if (!_saveDataIndices.TryGetValue(saveUpdate.SaveDataIndex, out var name)) {
            Logger.Warn($"Received save update with unknown index: {saveUpdate.SaveDataIndex}");
            return;
        }
        
        Logger.Info($"Received save update for index: {saveUpdate.SaveDataIndex}");

        var pd = PlayerData.instance;

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
    }
}
