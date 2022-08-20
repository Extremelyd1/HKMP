using System;
using System.Collections.Generic;
using System.Reflection;
using Hkmp.Networking.Packet.Data;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Entity.Action;

internal static class EntityFsmActions {
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

    public static bool GetNetworkDataFromAction(EntityNetworkData data, FsmStateAction action) {
        var actionType = action.GetType();
        if (!TypeGetMethodInfos.TryGetValue(actionType, out var methodInfo)) {
            throw new InvalidOperationException(
                $"Given action type: {action.GetType()} does not have an associated method to get");
        }

        var returnObject = methodInfo.Invoke(
            null,
            StaticNonPublicFlags,
            null,
            new object[] { data, action },
            null!
        );

        // Return whether the return object is a bool and has the value 'true'
        return returnObject is true;
    }

    public static void ApplyNetworkDataFromAction(EntityNetworkData data, FsmStateAction action) {
        var actionType = action.GetType();
        if (!TypeApplyMethodInfos.TryGetValue(actionType, out var methodInfo)) {
            throw new InvalidOperationException(
                $"Given action type: {action.GetType()} does not have an associated method to apply");
        }

        try {
            methodInfo.Invoke(
                null,
                StaticNonPublicFlags,
                null,
                new object[] { data, action },
                null!
            );
        } catch (Exception e) {
            Logger.Warn($"Apply method threw exception: {e.GetType()}, {e.Message}, {e.StackTrace}");

            e = e.InnerException;
            while (e != null) {
                Logger.Warn($"  Inner exception: {e.GetType()}, {e.Message}, {e.StackTrace}");

                e = e.InnerException;
            }
        }
    }

    #region SpawnObjectFromGlobalPool

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SpawnObjectFromGlobalPool action) {
        var position = Vector3.zero;
        var euler = Vector3.up;

        var spawnPoint = action.spawnPoint.Value;
        if (spawnPoint != null) {
            position = spawnPoint.transform.position;
            if (!action.position.IsNone) {
                position += action.position.Value;
            }

            if (!action.rotation.IsNone) {
                euler = action.rotation.Value;
            } else {
                euler = spawnPoint.transform.eulerAngles;
            }
        } else {
            if (!action.position.IsNone) {
                position = action.position.Value;
            }

            if (!action.rotation.IsNone) {
                euler = action.rotation.Value;
            }
        }

        data.Packet.Write(position.x);
        data.Packet.Write(position.y);
        data.Packet.Write(position.z);

        data.Packet.Write(euler.x);
        data.Packet.Write(euler.y);
        data.Packet.Write(euler.z);

        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SpawnObjectFromGlobalPool action) {
        var position = new Vector3(
            data.Packet.ReadFloat(),
            data.Packet.ReadFloat(),
            data.Packet.ReadFloat()
        );
        var euler = new Vector3(
            data.Packet.ReadFloat(),
            data.Packet.ReadFloat(),
            data.Packet.ReadFloat()
        );

        if (action.gameObject != null) {
            action.storeObject.Value = action.gameObject.Value.Spawn(position, Quaternion.Euler(euler));
        }
    }

    #endregion

    #region FireAtTarget

    private static bool GetNetworkDataFromAction(EntityNetworkData data, FireAtTarget action) {
        var target = action.target;

        var position = target.Value.transform.position;
        data.Packet.Write(position.x);
        data.Packet.Write(position.y);

        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, FireAtTarget action) {
        var posX = data.Packet.ReadFloat();
        var posY = data.Packet.ReadFloat();

        var selfGameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);

        var selfPosition = selfGameObject.transform.position;

        var rigidBody = selfGameObject.GetComponent<Rigidbody2D>();
        if (rigidBody == null) {
            return;
        }

        var num = Mathf.Atan2(
            posY + action.position.Value.y - selfPosition.y,
            posX + action.position.Value.x - selfPosition.x
        ) * 57.295776f;

        if (!action.spread.IsNone) {
            num += UnityEngine.Random.Range(-action.spread.Value, action.spread.Value);
        }

        rigidBody.velocity = new Vector2(
            action.speed.Value * Mathf.Cos(num * ((float)System.Math.PI / 180f)),
            action.speed.Value * Mathf.Sin(num * ((float)System.Math.PI / 180f))
        );
    }

    #endregion

    #region SetScale

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetScale action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == action.Fsm.GameObject) {
            return false;
        }

        var scale = action.vector.IsNone ? gameObject.transform.localScale : action.vector.Value;
        if (!action.x.IsNone) {
            scale.x = action.x.Value;
        }

        if (!action.y.IsNone) {
            scale.y = action.y.Value;
        }

        if (!action.z.IsNone) {
            scale.z = action.z.Value;
        }

        data.Packet.Write(scale.x);
        data.Packet.Write(scale.y);
        data.Packet.Write(scale.z);

        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetScale action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == action.Fsm.GameObject) {
            return;
        }

        var scale = new Vector3(
            data.Packet.ReadFloat(),
            data.Packet.ReadFloat(),
            data.Packet.ReadFloat()
        );

        gameObject.transform.localScale = scale;
    }

    #endregion

    #region SetFsmBool

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetFsmBool action) {
        // TODO: if action.setValue can be a reference, make sure to network it
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetFsmBool action) {
        if (action.setValue == null) {
            return;
        }

        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == action.Fsm.GameObject) {
            return;
        }

        if (gameObject == null) {
            return;
        }

        var fsm = ActionHelpers.GetGameObjectFsm(gameObject, action.fsmName.Value);
        if (fsm == null) {
            return;
        }

        var fsmBool = fsm.FsmVariables.FindFsmBool(action.variableName.Value);
        if (fsmBool == null) {
            return;
        }

        fsmBool.Value = action.setValue.Value;
    }

    #endregion

    #region SetFsmFloat

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetFsmFloat action) {
        // TODO: if action.setValue can be a reference, make sure to network it
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetFsmFloat action) {
        if (action.setValue == null) {
            return;
        }

        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == action.Fsm.GameObject) {
            return;
        }

        if (gameObject == null) {
            return;
        }

        var fsm = ActionHelpers.GetGameObjectFsm(gameObject, action.fsmName.Value);
        if (fsm == null) {
            return;
        }

        var fsmFloat = fsm.FsmVariables.GetFsmFloat(action.variableName.Value);
        if (fsmFloat == null) {
            return;
        }

        fsmFloat.Value = action.setValue.Value;
    }

    #endregion
}