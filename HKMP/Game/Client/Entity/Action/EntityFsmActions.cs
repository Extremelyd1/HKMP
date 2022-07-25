using System;
using System.Collections.Generic;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Game.Client.Entity.Action; 

internal static class EntityFsmActions {
    private const string LogObjectName = "Hkmp.Game.Client.Entity.Action.EntityFsmActions"; 
    
    public static readonly HashSet<Type> SupportedActionTypes = new() {
        typeof(SpawnObjectFromGlobalPool)
    };
    
    public static void GetNetworkDataFromAction(EntityNetworkData data, FsmStateAction action) {
        if (action is SpawnObjectFromGlobalPool spawnObjectFromGlobalPool) {
            GetNetworkDataFromAction(data, spawnObjectFromGlobalPool);
        }

        throw new InvalidOperationException($"Given action type: {action.GetType()} does not have an associated method to get");
    }
    
    public static void ApplyNetworkDataFromAction(EntityNetworkData data, FsmStateAction action) {
        if (action is SpawnObjectFromGlobalPool spawnObjectFromGlobalPool) {
            ApplyNetworkDataFromAction(data, spawnObjectFromGlobalPool);
        }

        throw new InvalidOperationException($"Given action type: {action.GetType()} does not have an associated method to apply");
    }

    #region SpawnObjectFromGlobalPool

    private static void GetNetworkDataFromAction(EntityNetworkData data, SpawnObjectFromGlobalPool action) {
        var spawnPoint = action.spawnPoint;
        if (spawnPoint == null) {
            data.Data.Add(0);
            return;
        }

        data.Data.Add(1);

        var position = spawnPoint.Value.transform.position;
        data.Data.AddRange(BitConverter.GetBytes(position.x));
        data.Data.AddRange(BitConverter.GetBytes(position.y));
        data.Data.AddRange(BitConverter.GetBytes(position.z));
        
        Logger.Get().Info(LogObjectName, $"Added entity network data: {position.x}, {position.y}, {position.z}");
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SpawnObjectFromGlobalPool action) {
        var packet = new Packet(data.Data.ToArray());

        var position = Vector3.zero;
        var euler = Vector3.up;

        var hasSpawnPoint = packet.ReadBool();
        if (hasSpawnPoint) {
            var posX = packet.ReadFloat();
            var posY = packet.ReadFloat();
            var posZ = packet.ReadFloat();
            
            Logger.Get().Info(LogObjectName, $"Applying entity network data: {posX}, {posY}, {posZ}");

            position = new Vector3(posX, posY, posZ);

            if (!action.position.IsNone) {
                position += action.position.Value;
            }

            euler = !action.rotation.IsNone ? action.rotation.Value : action.spawnPoint.Value.transform.eulerAngles;
        } else {
            Logger.Get().Info(LogObjectName, $"Applying entity network data without spawnpoint");
            if (!action.position.IsNone) {
                position = action.position.Value;
            }

            if (!action.rotation.IsNone) {
                euler = action.rotation.Value;
            }
        }

        if (action.gameObject != null) {
            action.storeObject.Value = action.gameObject.Value.Spawn(position, Quaternion.Euler(euler));
        }
    }

    #endregion
}