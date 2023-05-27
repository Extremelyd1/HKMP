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
                continue;
            }

            // Filter out the base methods
            var parameterType = parameterInfos[1].ParameterType;
            if (parameterType.IsAbstract || !parameterType.IsSubclassOf(typeof(FsmStateAction))) {
                continue;
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

    /// <summary>
    /// Checks whether the given game object is in the entity registry and can thus be registered as an entity in
    /// the system.
    /// </summary>
    /// <param name="gameObject">The game object to check for.</param>
    /// <returns>true if the given game object is in the entity registry; otherwise false.</returns>
    private static bool IsObjectInRegistry(GameObject gameObject) {
        foreach (var fsm in gameObject.GetComponents<PlayMakerFSM>()) {
            if (EntityRegistry.TryGetEntry(fsm.gameObject, fsm.Fsm.Name, out _)) {
                return true;
            }
        }

        return false;
    }

    #region SpawnObjectFromGlobalPool

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SpawnObjectFromGlobalPool action) {
        EntitySpawnEvent?.Invoke(action, action.storeObject.Value);
        
        // We first check whether this action results in the spawning of an entity that is managed by the
        // system. Because if so, it would already be handled by an EntitySpawn packet instead, and this will only
        // duplicate the spawning and leave it uncontrolled. So we don't send the data at all
        var toSpawnObject = action.storeObject.Value;
        if (IsObjectInRegistry(toSpawnObject)) {
            Logger.Debug($"Tried getting SpawnObjectFromGlobalPool network data, but spawned object is entity");
            return false;
        }
        
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
            var spawnedObject = action.gameObject.Value.Spawn(position, Quaternion.Euler(euler));
            action.storeObject.Value = spawnedObject;
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
        var setValue = action.setValue.Value;
        data.Packet.Write(setValue);
        
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetFsmBool action) {
        var setValue = data.Packet.ReadBool();

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

        fsmBool.Value = setValue;
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
        if (action?.emission == null) {
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
    
    #region SetVelocity2d

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetVelocity2d action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return false;
        }
        
        if (IsObjectInRegistry(gameObject)) {
            Logger.Debug("Tried getting SetVelocity2d network data, but entity is in registry");
            return false;
        }

        var rigidbody = gameObject.GetComponent<Rigidbody2D>();
        if (rigidbody == null) {
            return false;
        }

        var vector = action.vector.IsNone ? rigidbody.velocity : action.vector.Value;
        if (!action.x.IsNone) {
            vector.x = action.x.Value;
        }

        if (!action.y.IsNone) {
            vector.y = action.y.Value;
        }

        data.Packet.Write(vector.x);
        data.Packet.Write(vector.y);
        
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetVelocity2d action) {
        var vector = new Vector2(
            data.Packet.ReadFloat(), 
            data.Packet.ReadFloat()
        );

        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }
        
        var rigidbody = gameObject.GetComponent<Rigidbody2D>();
        if (rigidbody == null) {
            return;
        }

        rigidbody.velocity = vector;
    }
    
    #endregion
    
    #region SetMeshRenderer

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetMeshRenderer action) {
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetMeshRenderer action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null) {
            return;
        }

        meshRenderer.enabled = action.active.Value;
    }
    
    #endregion
    
    #region SetPosition

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetPosition action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return false;
        }

        if (IsObjectInRegistry(gameObject)) {
            Logger.Debug("Tried getting SetPosition network data, but entity is in registry");
            return false;
        }

        // TODO: this action is used for initialization currently and uses static values
        // If we come across this action and it uses references, we need to not only uncomment the below, but
        // also think about how to use static values for entity initialization and the dynamic values for
        // non-initialization purposes

        // Vector3 vector3;
        // if (!action.vector.IsNone) {
        //     vector3 = action.vector.Value;
        // } else {
        //     if (action.space == Space.World) {
        //         vector3 = gameObject.transform.position;
        //     } else {
        //         vector3 = gameObject.transform.localPosition;
        //     }
        // }
        //
        // if (!action.x.IsNone) {
        //     vector3.x = action.x.Value;
        // }
        //
        // if (!action.y.IsNone) {
        //     vector3.y = action.y.Value;
        // }
        //
        // if (!action.z.IsNone) {
        //     vector3.z = action.z.Value;
        // }
        //
        // data.Packet.Write(vector3.x);
        // data.Packet.Write(vector3.y);
        // data.Packet.Write(vector3.z);
        
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetPosition action) {
        // See comment in "Get" method above

        // var vector = new Vector3(
        //     data.Packet.ReadFloat(),
        //     data.Packet.ReadFloat()
        // );

        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }
        
        Vector3 vector3;
        if (!action.vector.IsNone) {
            vector3 = action.vector.Value;
        } else {
            if (action.space == Space.World) {
                vector3 = gameObject.transform.position;
            } else {
                vector3 = gameObject.transform.localPosition;
            }
        }
        
        if (!action.x.IsNone) {
            vector3.x = action.x.Value;
        }
        
        if (!action.y.IsNone) {
            vector3.y = action.y.Value;
        }
        
        if (!action.z.IsNone) {
            vector3.z = action.z.Value;
        }

        if (action.space == Space.World) {
            gameObject.transform.position = vector3;
        } else {
            gameObject.transform.localPosition = vector3;
        }
    }
    
    #endregion

    #region ActivateGameObject

    private static bool GetNetworkDataFromAction(EntityNetworkData data, ActivateGameObject action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return false;
        }
        
        if (IsObjectInRegistry(gameObject)) {
            Logger.Debug("Tried getting ActivateGameObject network data, but entity is in registry");
            return false;
        }

        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, ActivateGameObject action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        void SetActiveRecursively(GameObject go, bool state) {
            go.SetActive(state);

            foreach (UnityEngine.Component component in go.transform) {
                SetActiveRecursively(component.gameObject, state);
            }
        }

        if (action.recursive.Value) {
            SetActiveRecursively(gameObject, action.activate.Value);
        } else {
            gameObject.SetActive(action.activate.Value);
        }
    }

    #endregion
    
    #region Tk2dPlayAnimation

    private static bool GetNetworkDataFromAction(EntityNetworkData data, Tk2dPlayAnimation action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return false;
        }
        
        if (IsObjectInRegistry(gameObject)) {
            Logger.Debug("Tried getting Tk2dPlayAnimation network data, but entity is in registry");
            return false;
        }

        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, Tk2dPlayAnimation action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var animator = gameObject.GetComponent<tk2dSpriteAnimator>();
        if (animator == null) {
            return;
        }
        
        animator.Play(action.clipName.Value);
    }

    #endregion
    
    #region Tk2dPlayFrame

    private static bool GetNetworkDataFromAction(EntityNetworkData data, Tk2dPlayFrame action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return false;
        }
        
        if (IsObjectInRegistry(gameObject)) {
            Logger.Debug("Tried getting Tk2dPlayFrame network data, but entity is in registry");
            return false;
        }

        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, Tk2dPlayFrame action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var animator = gameObject.GetComponent<tk2dSpriteAnimator>();
        if (animator == null) {
            return;
        }
        
        animator.PlayFromFrame(action.frame.Value);
    }

    #endregion
    
    #region Tk2dPlayAnimationWithEvents

    private static bool GetNetworkDataFromAction(EntityNetworkData data, Tk2dPlayAnimationWithEvents action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return false;
        }
        
        if (IsObjectInRegistry(gameObject)) {
            Logger.Debug("Tried getting Tk2dPlayAnimationWithEvents network data, but entity is in registry");
            return false;
        }

        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, Tk2dPlayAnimationWithEvents action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var animator = gameObject.GetComponent<tk2dSpriteAnimator>();
        if (animator == null) {
            return;
        }
        
        animator.Play(action.clipName.Value);
    }

    #endregion
    
    #region SpawnBlood

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SpawnBlood action) {
        if (GlobalPrefabDefaults.Instance == null) {
            return false;
        }

        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SpawnBlood action) {
        var position = action.position.Value;
        if (action.spawnPoint.Value != null) {
            position += action.spawnPoint.Value.transform.position;
        }
        
        GlobalPrefabDefaults.Instance.SpawnBlood(
            position,
            (short) action.spawnMin.Value,
            (short) action.spawnMax.Value,
            action.speedMin.Value,
            action.speedMax.Value,
            action.angleMin.Value,
            action.angleMax.Value,
            action.colorOverride.IsNone ? new Color?() : action.colorOverride.Value
        );
    }

    #endregion
}
