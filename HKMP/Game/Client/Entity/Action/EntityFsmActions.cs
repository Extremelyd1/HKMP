using System;
using System.Collections.Generic;
using System.Reflection;
using Hkmp.Networking.Packet.Data;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;
using Random = UnityEngine.Random;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

namespace Hkmp.Game.Client.Entity.Action;

/// <summary>
/// Static class containing method that transform FSM actions into network-able data and applying networked data
/// into the FSM actions implementations. 
/// </summary>
internal static class EntityFsmActions {
    /// <summary>
    /// The prefix of a method name that transforms an FSM action into network-able data.
    /// </summary>
    private const string GetMethodNamePrefix = "Get";
    /// <summary>
    /// The prefix of a method name that applies network data into an FSM action.
    /// </summary>
    private const string ApplyMethodNamePrefix = "Apply";

    /// <summary>
    /// Binding flags for accessing the private static methods in this class.
    /// </summary>
    private const BindingFlags StaticNonPublicFlags = BindingFlags.Static | BindingFlags.NonPublic;

    /// <summary>
    /// Set containing types of actions that are supported for transformation by a method in this class.
    /// </summary>
    public static readonly HashSet<Type> SupportedActionTypes = new();

    /// <summary>
    /// Event that is called when an entity is spawned from an object.
    /// </summary>
    public static event Action<FsmStateAction, GameObject> EntitySpawnEvent;

    /// <summary>
    /// Dictionary mapping a type of an FSM action to the corresponding method info of the "get" method in this class.
    /// </summary>
    private static readonly Dictionary<Type, MethodInfo> TypeGetMethodInfos = new();
    /// <summary>
    /// Dictionary mapping a type of an FSM action to the corresponding method info of the "apply" method in this class.
    /// </summary>
    private static readonly Dictionary<Type, MethodInfo> TypeApplyMethodInfos = new();

    /// <summary>
    /// Static constructor that initializes the set and dictionaries by checking all methods in the class.
    /// </summary>
    /// <exception cref="Exception"></exception>
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

    /// <summary>
    /// Gets network-able data from the given action and puts it in the given <see cref="EntityNetworkData"/> instance.
    /// </summary>
    /// <param name="data">The instance to put the data into.</param>
    /// <param name="action">The action to transform.</param>
    /// <returns>Whether from this action network-able data was made.</returns>
    /// <exception cref="InvalidOperationException">Thrown if there is no suitable method for the action and thus
    /// no network data is written.</exception>
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

    /// <summary>
    /// Reads networked data from the given instance and mimics the execution of the given FSM action.
    /// </summary>
    /// <param name="data">The instance from which to get the data.</param>
    /// <param name="action">The FSM action to mimic execution for.</param>
    /// <exception cref="InvalidOperationException">Thrown if there is no suitable method for the action and thus
    /// no FSM action will be mimicked.</exception>
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

        EntitySpawnEvent?.Invoke(action, action.storeObject.Value);

        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SpawnObjectFromGlobalPool action) {
        // We first check whether applying this action results in the spawning of an entity that is managed by the
        // system. Because if so, it would already be handled by an EntitySpawn packet instead, and this will only
        // duplicate the spawning and leave it uncontrolled
        var toSpawnObject = action.gameObject.Value;
        foreach (var fsm in toSpawnObject.GetComponents<PlayMakerFSM>()) {
            if (EntityRegistry.TryGetEntry(fsm.gameObject.name, fsm.Fsm.Name, out var entry)) {
                Logger.Debug($"Tried applying SpawnObjectFromGlobalPool network data, but to spawn object is entity: {entry.Type}");
                return;
            }
        }

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
            var spawnedObject = action.gameObject.Value.Spawn(position, Quaternion.Euler(euler));
            action.storeObject.Value = spawnedObject;

            // TODO: this might give an issue if the packets for two of these actions get out of order and the IDs
            // of the spawned entities get switched. This only holds in the case where two different entities are
            // spawned
            EntitySpawnEvent?.Invoke(action, spawnedObject);
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
            num += Random.Range(-action.spread.Value, action.spread.Value);
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
    
    #region SetParticleEmission

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetParticleEmission action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetParticleEmission action) {
        if (action.emission == null) {
            return;
        }

        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var particleSystem = gameObject.GetComponent<ParticleSystem>();
        if (particleSystem == null) {
            return;
        }

#pragma warning disable CS0618
        particleSystem.enableEmission = action.emission.Value;
#pragma warning restore CS0618
    }
    
    #endregion
    
    #region PlayParticleEmitter

    private static bool GetNetworkDataFromAction(EntityNetworkData data, PlayParticleEmitter action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, PlayParticleEmitter action) {
        if (action.emit == null) {
            return;
        }

        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var particleSystem = gameObject.GetComponent<ParticleSystem>();
        if (particleSystem == null) {
            return;
        }

        if (particleSystem.isPlaying && action.emit.Value <= 0) {
            particleSystem.Play();
        } else if (action.emit.Value > 0) {
            particleSystem.Emit(action.emit.Value);
        }
    }
    
    #endregion
    
    #region SetGameObject

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetGameObject action) {
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetGameObject action) {
        action.variable.Value = action.gameObject.Value;
    }
    
    #endregion
    
    #region GetOwner

    private static bool GetNetworkDataFromAction(EntityNetworkData data, GetOwner action) {
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, GetOwner action) {
        action.storeGameObject.Value = action.Owner;
    }
    
    #endregion
    
    #region GetHero

    private static bool GetNetworkDataFromAction(EntityNetworkData data, GetHero action) {
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, GetHero action) {
        var heroController = HeroController.instance;
        action.storeResult.Value = heroController == null ? null : heroController.gameObject;
    }
    
    #endregion
    
    #region FindChild

    private static bool GetNetworkDataFromAction(EntityNetworkData data, FindChild action) {
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, FindChild action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var transform = gameObject.transform.Find(action.childName.Value);
        action.storeResult.Value = transform == null ? null : transform.gameObject;
    }
    
    #endregion
    
    #region FindGameObject

    private static bool GetNetworkDataFromAction(EntityNetworkData data, FindGameObject action) {
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, FindGameObject action) {
        if (action.withTag.Value == "Untagged") {
            action.store.Value = GameObject.Find(action.objectName.Value);
            return;
        }

        if (string.IsNullOrEmpty(action.objectName.Value)) {
            action.store.Value = GameObject.FindGameObjectWithTag(action.withTag.Value);
            return;
        }

        foreach (var gameObject in GameObject.FindGameObjectsWithTag(action.withTag.Value)) {
            if (gameObject.name == action.objectName.Value) {
                action.store.Value = gameObject;
                return;
            }
        }

        action.store.Value = null;
    }
    
    #endregion
    
    #region FindAlertRange

    private static bool GetNetworkDataFromAction(EntityNetworkData data, FindAlertRange action) {
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, FindAlertRange action) {
        action.storeResult.Value = AlertRange.Find(action.target.GetSafe(action), action.childName);
    }
    
    #endregion
    
    #region GetParent

    private static bool GetNetworkDataFromAction(EntityNetworkData data, GetParent action) {
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, GetParent action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            action.storeResult.Value = null;
            return;
        }

        var parent = gameObject.transform.parent;
        if (parent == null) {
            action.storeResult.Value = null;
            return;
        }

        action.storeResult.Value = parent.gameObject;
    }
    
    #endregion
}
