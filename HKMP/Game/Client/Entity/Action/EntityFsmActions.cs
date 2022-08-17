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

            Logger.Get().Info(LogObjectName, $"Parameter type that is subclass: {parameterType}");
            SupportedActionTypes.Add(parameterType);

            if (methodInfo.Name.StartsWith(GetMethodNamePrefix)) {
                TypeGetMethodInfos.Add(parameterType, methodInfo);
            } else if (methodInfo.Name.StartsWith(ApplyMethodNamePrefix)) {
                Logger.Get().Info(LogObjectName, $"Method info found: {methodInfo}");
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
        
        Logger.Get().Info(LogObjectName, $"Apply method: {actionType}, {methodInfo}");

        methodInfo.Invoke(
            null, 
            StaticNonPublicFlags,
            null, 
            new object[] { data, action }, 
            null!
        );
        
        Logger.Get().Info(LogObjectName, $"After methodinfo invoke: {actionType}, {methodInfo}");
    }

    #region SpawnObjectFromGlobalPool

    private static void GetNetworkDataFromAction(EntityNetworkData data, SpawnObjectFromGlobalPool action) {
        var spawnPoint = action.spawnPoint;
        if (spawnPoint == null) {
            data.Packet.Write(false);
            return;
        }

        data.Packet.Write(true);

        var position = spawnPoint.Value.transform.position;
        data.Packet.Write(position.x);
        data.Packet.Write(position.y);

        if (action.rotation.IsNone) {
            var rotation = spawnPoint.Value.transform.eulerAngles;
            data.Packet.Write(rotation.x);
            data.Packet.Write(rotation.y);
            data.Packet.Write(rotation.z);
        }

        Logger.Get().Info(LogObjectName, $"Added SOFGP entity network data: {position.x}, {position.y}, {position.z}");
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SpawnObjectFromGlobalPool action) {
        try {
            var position = Vector3.zero;
            var euler = Vector3.up;

            var hasSpawnPoint = data.Packet.ReadBool();
            if (hasSpawnPoint) {
                var posX = data.Packet.ReadFloat();
                var posY = data.Packet.ReadFloat();

                Logger.Get().Info(LogObjectName, $"Applying SOFGP entity network data: {posX}, {posY}");

                position = new Vector3(posX, posY);

                if (!action.position.IsNone) {
                    position += action.position.Value;
                }

                if (!action.rotation.IsNone) {
                    euler = action.rotation.Value;
                } else {
                    euler = new Vector3(
                        data.Packet.ReadFloat(),
                        data.Packet.ReadFloat(),
                        data.Packet.ReadFloat()
                    );
                }
            } else {
                Logger.Get().Info(LogObjectName, "Applying SOFGP entity network data without spawnpoint");
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
        } catch (Exception e) {
            Logger.Get().Info(LogObjectName, $"Apply SOFGP exception: {e.GetType()}, {e.Message}, {e.StackTrace}");
        }
    }

    #endregion
    
    #region FireAtTarget

    private static void GetNetworkDataFromAction(EntityNetworkData data, FireAtTarget action) {
        var target = action.target;

        var position = target.Value.transform.position;
        data.Packet.Write(position.x);
        data.Packet.Write(position.y);

        Logger.Get().Info(LogObjectName, $"Added FAT entity network data: {position.x}, {position.y}");
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, FireAtTarget action) {
        try {
            var posX = data.Packet.ReadFloat();
            var posY = data.Packet.ReadFloat();

            Logger.Get().Info(LogObjectName, $"Applying FAT entity network data: {posX}, {posY}");

            // var selfGameObject = ReflectionHelper.GetField<FireAtTarget, FsmGameObject>(action, "self");
            
            Logger.Get().Info(LogObjectName, $"Action: {action}");
            
            var gameObject = action.gameObject;
            Logger.Get().Info(LogObjectName, $"GameObject null: {gameObject == null}");
            Logger.Get().Info(LogObjectName, $"GameObject: {gameObject}");
            
            var fsm = action.Fsm;
            Logger.Get().Info(LogObjectName, $"Action FSM: {fsm}");
            
            var ownerDefaultObject = gameObject.GameObject;
            Logger.Get().Info(LogObjectName, $"ownerDefaultObject null: {ownerDefaultObject == null}");
            Logger.Get().Info(LogObjectName, $"ownerDefaultObject: {ownerDefaultObject}");
            
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
        } catch (Exception e) {
            Logger.Get().Info(LogObjectName, $"Apply FAT exception: {e.GetType()}, {e.Message}, {e.StackTrace}");
        }
    }
    
    #endregion
}