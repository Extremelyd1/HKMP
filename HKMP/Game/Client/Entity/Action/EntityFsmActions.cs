using System;
using System.Collections.Generic;
using System.Reflection;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Game.Client.Entity.Action;

internal static class EntityFsmActions {
    private const string LogObjectName = "Hkmp.Game.Client.Entity.Action.EntityFsmActions";

    private const string GetMethodNamePrefix = "Get";
    private const string ApplyMethodNamePrefix = "Apply";

    private const BindingFlags StaticNonPublicFlags = BindingFlags.Static | BindingFlags.NonPublic;

    public static readonly HashSet<Type> SupportedActionTypes = new();

    private static readonly Dictionary<Type, MethodInfo> TypeGetMethodInfos = new();
    private static readonly Dictionary<Type, MethodInfo> TypeApplyMethodInfos = new();

    static EntityFsmActions() {
        var methodInfos = typeof(EntityFsmActions).GetMethods(StaticNonPublicFlags);

        foreach (var methodInfo in methodInfos) {
            var parameterInfos = methodInfo.GetParameters();
            if (parameterInfos.Length != 2) {
                // Can't be a method that gets or applies entity network data
                return;
            }

            // Filter out the base methods
            var parameterType = parameterInfos[1].ParameterType;
            if (parameterType.IsAbstract || !parameterType.IsSubclassOf(typeof(FsmStateAction))) {
                return;
            }

            SupportedActionTypes.Add(parameterType);

            if (methodInfo.Name.StartsWith(GetMethodNamePrefix)) {
                TypeGetMethodInfos.Add(parameterType, methodInfo);
            } else if (methodInfo.Name.StartsWith(ApplyMethodNamePrefix)) {
                TypeApplyMethodInfos.Add(parameterType, methodInfo);
            } else {
                throw new Exception("Method was defined that does not adhere to the method naming");
            }
        }
    }

    public static void GetNetworkDataFromAction(EntityNetworkData data, FsmStateAction action) {
        var actionType = action.GetType();
        if (!TypeGetMethodInfos.TryGetValue(actionType, out var methodInfo)) {
            throw new InvalidOperationException(
                $"Given action type: {action.GetType()} does not have an associated method to get");
        }

        methodInfo.Invoke(
            null, 
            StaticNonPublicFlags, 
            null, 
            new object[] { data, action }, 
            null!
        );
    }

    public static void ApplyNetworkDataFromAction(EntityNetworkData data, FsmStateAction action) {
        var actionType = action.GetType();
        if (!TypeApplyMethodInfos.TryGetValue(actionType, out var methodInfo)) {
            throw new InvalidOperationException(
                $"Given action type: {action.GetType()} does not have an associated method to apply");
        }

        methodInfo.Invoke(
            null, 
            StaticNonPublicFlags,
            null, 
            new object[] { data, action }, 
            null!
        );
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