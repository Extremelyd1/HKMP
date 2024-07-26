using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;
using Object = UnityEngine.Object;
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
    public static event Func<EntitySpawnDetails, bool> EntitySpawnEvent;

    /// <summary>
    /// Dictionary mapping a type of an FSM action to the corresponding method info of the "get" method in this class.
    /// </summary>
    private static readonly Dictionary<Type, MethodInfo> TypeGetMethodInfos = new();
    /// <summary>
    /// Dictionary mapping a type of an FSM action to the corresponding method info of the "apply" method in this class.
    /// </summary>
    private static readonly Dictionary<Type, MethodInfo> TypeApplyMethodInfos = new();

    /// <summary>
    /// Dictionary containing queues of objects for a FSM action that has been executed on a host entity.
    /// Used to log the results of random calls to network to clients.
    /// </summary>
    private static readonly Dictionary<FsmStateAction, Queue<object>> RandomActionValues = new();

    /// <summary>
    /// List of actions that are executing while in a state and need to be stopped again when the state is exited.
    /// </summary>
    private static readonly List<ActionInState> ActionsInState = new();
    
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

        // Register the IL hooks for modifying FSM action methods
        IL.HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPool.OnEnter += FlingObjectsFromGlobalPoolOnEnter;
        IL.HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPoolVel.OnEnter += FlingObjectsFromGlobalPoolVelOnEnter;
        IL.HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPoolTime.OnUpdate += FlingObjectsFromGlobalPoolTimeOnUpdate;
        IL.HutongGames.PlayMaker.Actions.GetRandomChild.DoGetRandomChild += GetRandomChildOnDoGetRandomChild;
        
        // Register IL hooks for the OnEnter method of certain classes. These OnEnter methods do not
        // have a method body and thus no IL instructions (apart from ret). Hooking this in the FsmActionHooks class
        // will not work, so we emit a NOP instruction to the body to make it hookable
        void EmitNop(ILContext il) => new ILCursor(il).Emit(OpCodes.Nop);

        IL.HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPoolTime.OnEnter += EmitNop;
        IL.SpawnBloodTime.OnEnter += EmitNop;
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
        return EntityRegistry.TryGetEntry(gameObject, out _);
    }
    
    /// <summary>
    /// Method to call the spawn event externally. TODO: refactor this into something more appropriate
    /// </summary>
    /// <param name="details">The spawn details for the event.</param>
    /// <returns>Whether an entity was registered from this spawn.</returns>
    public static bool CallEntitySpawnEvent(EntitySpawnDetails details) {
        return EntitySpawnEvent != null && EntitySpawnEvent.Invoke(details);
    }
    
    /// <summary>
    /// Emit intercept instruction on the next Unity Random Range() call for the given IL cursor.
    /// </summary>
    /// <param name="c">The cursor for the IL context of the method.</param>
    /// <typeparam name="TValue">The return type of the random call.</typeparam>
    /// <typeparam name="TObject">The type of the FSM state action in which the random call occurs.</typeparam>
    private static void EmitRandomInterceptInstructions<TValue, TObject>(ILCursor c) where TObject : FsmStateAction {
        // Goto the next call instruction for Random.Range()
        c.GotoNext(i => i.MatchCall(typeof(Random), "Range"));

        // Move the cursor after the call instruction
        c.Index++;

        // Push the current instance of the class onto the stack
        c.Emit(OpCodes.Ldarg_0);

        // Emit a delegate that pops the current random value off the stack and puts it back after some processing 
        c.EmitDelegate<Func<TValue, TObject, TValue>>((value, instance) => {
            // We need to check whether the game object that is being spawned with this action is not an object
            // managed by the system. Because if so, we do not store the random values because the action for it
            // is not being networked. Only the game object spawn is networked with an EntitySpawn packet directly.
            var fsmGameObject = ReflectionHelper.GetField<TObject, FsmGameObject>(instance, "gameObject");
            if (fsmGameObject != null && fsmGameObject.Value != null && IsObjectInRegistry(fsmGameObject.Value)) {
                return value;
            }
            
            if (!RandomActionValues.TryGetValue(instance, out var queue)) {
                queue = new Queue<object>();
                RandomActionValues[instance] = queue;
            }

            queue.Enqueue(value);

            return value;
        });
    }
    
    /// <summary>
    /// IL edit method for modifying the <see cref="FlingObjectsFromGlobalPool"/>
    /// <see cref="FlingObjectsFromGlobalPool.OnEnter"/> method to store the results of the random calls.
    /// </summary>
    private static void FlingObjectsFromGlobalPoolOnEnter(ILContext il) {
        try {
            // Create a cursor for this context
            var c = new ILCursor(il);
            
            // Emit instructions for Random.Range calls for 1 int and 4 floats 
            EmitRandomInterceptInstructions<int, FlingObjectsFromGlobalPool>(c);
            EmitRandomInterceptInstructions<float, FlingObjectsFromGlobalPool>(c);
            EmitRandomInterceptInstructions<float, FlingObjectsFromGlobalPool>(c);
            EmitRandomInterceptInstructions<float, FlingObjectsFromGlobalPool>(c);
            EmitRandomInterceptInstructions<float, FlingObjectsFromGlobalPool>(c);
            
            // Reset cursor
            c = new ILCursor(il);

            // Goto the next call instruction for ObjectPoolExtensions.Spawn
            c.GotoNext(i => i.MatchCall(typeof(ObjectPoolExtensions), "Spawn"));
            
            // Move the cursor after the call instruction
            c.Index++;
            
            // Push the current instance of the class onto the stack
            c.Emit(OpCodes.Ldarg_0);
            
            // Emit a delegate that pops the spawned game object off the stack and uses it, then puts it back again
            c.EmitDelegate<Func<GameObject, FlingObjectsFromGlobalPool, GameObject>>((go, action) => {
                Logger.Debug($"Delegate of FlingObjectsFromGlobalPool: {go.name}");
                if (EntitySpawnEvent != null && EntitySpawnEvent.Invoke(new EntitySpawnDetails {
                        Type = EntitySpawnType.FsmAction,
                        Action = action,
                        GameObject = go
                })) {
                    Logger.Debug("FlingObjectsFromGlobalPool IL spawned object is entity");
                }
                
                return go;
            });
        } catch (Exception e) {
            Logger.Error($"Could not change FlingObjectsFromGlobalPool#OnEnter IL:\n{e}");
        }
    }

    /// <summary>
    /// IL edit method for modifying the <see cref="FlingObjectsFromGlobalPoolVel"/>
    /// <see cref="FlingObjectsFromGlobalPoolVel.OnEnter"/> method to store the results of the random calls.
    /// </summary>
    private static void FlingObjectsFromGlobalPoolVelOnEnter(ILContext il) {
        try {
            // Create a cursor for this context
            var c = new ILCursor(il);
            
            // Emit instructions for Random.Range calls for 1 int and 4 floats 
            EmitRandomInterceptInstructions<int, FlingObjectsFromGlobalPoolVel>(c);
            EmitRandomInterceptInstructions<float, FlingObjectsFromGlobalPoolVel>(c);
            EmitRandomInterceptInstructions<float, FlingObjectsFromGlobalPoolVel>(c);
            EmitRandomInterceptInstructions<float, FlingObjectsFromGlobalPoolVel>(c);
            EmitRandomInterceptInstructions<float, FlingObjectsFromGlobalPoolVel>(c);
            
            // Reset cursor
            c = new ILCursor(il);

            // Goto the next call instruction for ObjectPoolExtensions.Spawn
            c.GotoNext(i => i.MatchCall(typeof(ObjectPoolExtensions), "Spawn"));
            
            // Move the cursor after the call instruction
            c.Index++;
            
            // Push the current instance of the class onto the stack
            c.Emit(OpCodes.Ldarg_0);
            
            // Emit a delegate that pops the spawned game object off the stack and uses it, then puts it back again
            c.EmitDelegate<Func<GameObject, FlingObjectsFromGlobalPoolVel, GameObject>>((go, action) => {
                Logger.Debug($"Delegate of FlingObjectsFromGlobalPoolVel: {go.name}");
                if (EntitySpawnEvent != null && EntitySpawnEvent.Invoke(new EntitySpawnDetails {
                        Type = EntitySpawnType.FsmAction,
                        Action = action,
                        GameObject = go
                })) {
                    Logger.Debug("FlingObjectsFromGlobalPoolVel IL spawned object is entity");
                }
                
                return go;
            });
        } catch (Exception e) {
            Logger.Error($"Could not change FlingObjectsFromGlobalPoolVel#OnEnter IL:\n{e}");
        }
    }
    
    /// <summary>
    /// IL edit method for modifying the <see cref="FlingObjectsFromGlobalPoolTime"/>
    /// <see cref="FlingObjectsFromGlobalPoolTime.OnUpdate"/> method to network the repeated spawning of objects.
    /// </summary>
    private static void FlingObjectsFromGlobalPoolTimeOnUpdate(ILContext il) {
        try {
            // Create a cursor for this context
            var c = new ILCursor(il);
            
            // Goto the next call instruction for Random.Range()
            c.GotoNext(i => i.MatchCall(typeof(ObjectPoolExtensions), "Spawn"));

            // Move the cursor after the call instruction
            c.Index++;
            
            // Push the current instance of the class onto the stack
            c.Emit(OpCodes.Ldarg_0);

            // Emit a delegate that pops the spawned object off the stack and pushes it onto it again
            c.EmitDelegate<Func<GameObject, FlingObjectsFromGlobalPoolTime, GameObject>>((gameObject, action) => {
                EntitySpawnEvent?.Invoke(new EntitySpawnDetails {
                    Type = EntitySpawnType.FsmAction,
                    Action = action,
                    GameObject = gameObject
                });
                
                return gameObject;
            });
        } catch (Exception e) {
            Logger.Error($"Could not change FlingObjectsFromGlobalPoolTime#OnUpdate IL:\n{e}");
        }
    }

    /// <summary>
    /// IL edit method for modifying the <see cref="GetRandomChild"/> DoGetRandomChild
    /// method to store the results of the random calls.
    /// </summary>
    private static void GetRandomChildOnDoGetRandomChild(ILContext il) {
        try {
            // Create a cursor for this context
            var c = new ILCursor(il);
            
            // Emit instructions for Random.Range calls for 1 int and 4 floats 
            EmitRandomInterceptInstructions<int, GetRandomChild>(c);
        } catch (Exception e) {
            Logger.Error($"Could not change GetRandomChild#DoGetRandomChild IL:\n{e}");
        }
    }

    /// <summary>
    /// Register a state change for the given FSM. Will propagate this change to all actions that are running in
    /// that state.
    /// </summary>
    /// <param name="fsm">The FSM that changed states.</param>
    /// <param name="stateName">The name of the state that was changed to.</param>
    public static void RegisterStateChange(HutongGames.PlayMaker.Fsm fsm, string stateName) {
        Logger.Debug($"RegisterStateChange: {fsm.Name}, {stateName}");
    
        for (var i = ActionsInState.Count - 1; i >= 0; i--) {
            var actionInState = ActionsInState[i];
            
            Logger.Debug($"  Action in state: {actionInState.Fsm.Name}, {actionInState.StateName}");

            if (actionInState.Fsm == fsm && actionInState.StateName != stateName) {
                Logger.Debug("EntityFsmActions: state changed, cancelling action in state");
                actionInState.ExitState();
                ActionsInState.RemoveAt(i);
            }
        }
    }

    #region SpawnObjectFromGlobalPool

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SpawnObjectFromGlobalPool action) {
        // We first check whether this action results in the spawning of an entity that is managed by the
        // system. Because if so, it would already be handled by an EntitySpawn packet instead, and this will only
        // duplicate the spawning and leave it uncontrolled. So we don't send the data at all
        if (EntitySpawnEvent != null && EntitySpawnEvent.Invoke(new EntitySpawnDetails {
            Type = EntitySpawnType.FsmAction,
            Action = action,
            GameObject = action.storeObject.Value
        })) {
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
    
    #region FlingObjectsFromGlobalPool
    
    private static bool GetNetworkDataFromAction(EntityNetworkData data, FlingObjectsFromGlobalPool action) {
        // We first check whether the game object belonging to the Rigidbody2D in the action is an object that is
        // managed by the system. Because if so, it means that we have already caught its spawning in the IL hook
        // for the action and sent an EntitySpawn packet instead. So we need not also network this action separately.
        var rigidbody = ReflectionHelper.GetField<FlingObjectsFromGlobalPool, Rigidbody2D>(action, "rb2d");
        if (rigidbody != null && rigidbody.gameObject != null && IsObjectInRegistry(rigidbody.gameObject)) {
            Logger.Debug("Skipping getting network data for FlingObjectsFromGlobalPool, because spawned objects are managed by system");
            return false;
        }
        
        var position = Vector3.zero;
    
        var spawnPoint = action.spawnPoint.Value;
        if (spawnPoint != null) {
            position = spawnPoint.transform.position;
            if (!action.position.IsNone) {
                position += action.position.Value;
            }
        } else if (!action.position.IsNone) {
            position = action.position.Value;
        }

        if (!RandomActionValues.TryGetValue(action, out var queue)) {
            return false;
        }

        if (queue.Count == 0) {
            Logger.Debug("Getting data for FlingObjectsFromGlobalPool has not enough items in queue 1");
            return false;
        }
        
        data.Packet.Write(position.x);
        data.Packet.Write(position.y);
        data.Packet.Write(position.z);

        var numSpawns = (int) queue.Dequeue();
        data.Packet.Write((byte) numSpawns);

        for (var i = 0; i < numSpawns; i++) {
            if (action.originVariationX != null) {
                if (queue.Count == 0) {
                    Logger.Debug("Getting data for FlingObjectsFromGlobalPool has not enough items in queue 2");
                    return false;
                }
                
                var originVariationX = (float) queue.Dequeue();
                data.Packet.Write(originVariationX);
            } else {
                data.Packet.Write(0f);
            }
            
            if (action.originVariationY != null) {
                if (queue.Count == 0) {
                    Logger.Debug("Getting data for FlingObjectsFromGlobalPool has not enough items in queue 3");
                    return false;
                }
                
                var originVariationY = (float) queue.Dequeue();
                data.Packet.Write(originVariationY);
            } else {
                data.Packet.Write(0f);
            }

            if (queue.Count < 2) {
                Logger.Debug("Getting data for FlingObjectsFromGlobalPool has not enough items in queue 4");
                queue.Clear();
                return false;
            }

            var speed = (float) queue.Dequeue();
            var angle = (float) queue.Dequeue();
            
            data.Packet.Write(speed);
            data.Packet.Write(angle);
        }
        
        queue.Clear();
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, FlingObjectsFromGlobalPool action) {
        var position = new Vector3(
            data.Packet.ReadFloat(),
            data.Packet.ReadFloat(),
            data.Packet.ReadFloat()
        );

        var numSpawns = data.Packet.ReadByte();
        for (var i = 0; i < numSpawns; i++) {
            var go = action.gameObject.Value.Spawn(position, Quaternion.Euler(Vector3.zero));

            var originVariationX = data.Packet.ReadFloat();
            position.x += originVariationX;

            var originVariationY = data.Packet.ReadFloat();
            position.y += originVariationY;

            go.transform.position = position;

            var speed = data.Packet.ReadFloat();
            var angle = data.Packet.ReadFloat();

            var x = speed * Mathf.Cos(angle * ((float) System.Math.PI / 180f));
            var y = speed * Mathf.Sin(angle * ((float) System.Math.PI / 180f));

            var rigidBody = go.GetComponent<Rigidbody2D>();
            if (rigidBody == null) {
                return;
            }

            rigidBody.velocity = new Vector2(x, y);

            if (!action.FSM.IsNone) {
                FSMUtility.LocateFSM(go, action.FSM.Value).SendEvent(action.FSMEvent.Value);
            }
        }
    }
    
    #endregion
    
    #region FlingObjectsFromGlobalPoolVel
    
    private static bool GetNetworkDataFromAction(EntityNetworkData data, FlingObjectsFromGlobalPoolVel action) {
        // We first check whether the game object belonging to the Rigidbody2D in the action is an object that is
        // managed by the system. Because if so, it means that we have already caught its spawning in the IL hook
        // for the action and sent an EntitySpawn packet instead. So we need not also network this action separately.
        var rigidbody = ReflectionHelper.GetField<FlingObjectsFromGlobalPoolVel, Rigidbody2D>(action, "rb2d");
        if (rigidbody != null && rigidbody.gameObject != null && IsObjectInRegistry(rigidbody.gameObject)) {
            Logger.Debug("Skipping getting network data for FlingObjectsFromGlobalPool, because spawned objects are managed by system");
            return false;
        }
        
        var position = Vector3.zero;
    
        var spawnPoint = action.spawnPoint.Value;
        if (spawnPoint != null) {
            position = spawnPoint.transform.position;
            if (!action.position.IsNone) {
                position += action.position.Value;
            }
        } else if (!action.position.IsNone) {
            position = action.position.Value;
        }

        if (!RandomActionValues.TryGetValue(action, out var queue)) {
            return false;
        }

        if (queue.Count == 0) {
            Logger.Debug("Getting data for FlingObjectsFromGlobalPoolVel has not enough items in queue 1");
            return false;
        }
        
        data.Packet.Write(position.x);
        data.Packet.Write(position.y);
        data.Packet.Write(position.z);

        var numSpawns = (int) queue.Dequeue();
        data.Packet.Write((byte) numSpawns);

        for (var i = 0; i < numSpawns; i++) {
            if (action.originVariationX != null) {
                if (queue.Count == 0) {
                    Logger.Debug("Getting data for FlingObjectsFromGlobalPoolVel has not enough items in queue 2");
                    return false;
                }
                
                var originVariationX = (float) queue.Dequeue();
                data.Packet.Write(originVariationX);
            } else {
                data.Packet.Write(0f);
            }
            
            if (action.originVariationY != null) {
                if (queue.Count == 0) {
                    Logger.Debug("Getting data for FlingObjectsFromGlobalPoolVel has not enough items in queue 3");
                    return false;
                }
                
                var originVariationY = (float) queue.Dequeue();
                data.Packet.Write(originVariationY);
            } else {
                data.Packet.Write(0f);
            }

            if (queue.Count < 2) {
                Logger.Debug("Getting data for FlingObjectsFromGlobalPoolVel has not enough items in queue 4");
                queue.Clear();
                return false;
            }

            var speedX = (float) queue.Dequeue();
            var speedY = (float) queue.Dequeue();
            
            data.Packet.Write(speedX);
            data.Packet.Write(speedY);
        }
        
        queue.Clear();
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, FlingObjectsFromGlobalPoolVel action) {
        var position = new Vector3(
            data.Packet.ReadFloat(),
            data.Packet.ReadFloat(),
            data.Packet.ReadFloat()
        );

        var numSpawns = data.Packet.ReadByte();
        for (var i = 0; i < numSpawns; i++) {
            var go = action.gameObject.Value.Spawn(position, Quaternion.Euler(Vector3.zero));

            var originVariationX = data.Packet.ReadFloat();
            position.x += originVariationX;

            var originVariationY = data.Packet.ReadFloat();
            position.y += originVariationY;

            go.transform.position = position;

            var speedX = data.Packet.ReadFloat();
            var speedY = data.Packet.ReadFloat();

            var rigidBody = go.GetComponent<Rigidbody2D>();
            if (rigidBody == null) {
                return;
            }

            rigidBody.velocity = new Vector2(speedX, speedY);
        }
    }
    
    #endregion
    
    #region CreateObject

    private static bool GetNetworkDataFromAction(EntityNetworkData data, CreateObject action) {
        // We first check whether this action results in the spawning of an entity that is managed by the
        // system. Because if so, it would already be handled by an EntitySpawn packet instead, and this will only
        // duplicate the spawning and leave it uncontrolled. So we don't send the data at all
        if (EntitySpawnEvent != null && EntitySpawnEvent.Invoke(new EntitySpawnDetails {
            Type = EntitySpawnType.FsmAction,
            Action = action,
            GameObject = action.storeObject.Value
        })) {
            Logger.Debug($"Tried getting CreateObject network data, but spawned object is entity");
            return false;
        }

        var original = action.gameObject.Value;
        if (original == null) {
            return false;
        }

        var position = Vector3.zero;
        var euler = Vector3.zero;

        if (action.spawnPoint.Value != null) {
            position = action.spawnPoint.Value.transform.position;
            if (!action.position.IsNone) {
                position += action.position.Value;
            }

            euler = !action.rotation.IsNone ? action.rotation.Value : action.spawnPoint.Value.transform.eulerAngles;
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

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, CreateObject action) {
        Logger.Debug("ApplyNetworkDataFromAction CreateObject");

        Vector3 position;
        Vector3 euler;

        if (data == null) {
            position = Vector3.zero;
            euler = Vector3.zero;
            if (action.spawnPoint.Value != null) {
                position = action.spawnPoint.Value.transform.position;

                if (!action.position.IsNone) {
                    position += action.position.Value;
                }

                euler = !action.rotation.IsNone ? 
                    action.rotation.Value : 
                    action.spawnPoint.Value.transform.eulerAngles;
            } else {
                if (!action.position.IsNone) {
                    position = action.position.Value;
                }

                if (!action.rotation.IsNone) {
                    euler = action.rotation.Value;
                }
            }
        } else {
            position = new Vector3(
                data.Packet.ReadFloat(),
                data.Packet.ReadFloat(),
                data.Packet.ReadFloat()
            );
            euler = new Vector3(
                data.Packet.ReadFloat(),
                data.Packet.ReadFloat(),
                data.Packet.ReadFloat()
            );
        }

        var original = action.gameObject.Value;
        if (original == null) {
            return;
        }

        var spawnedObject = Object.Instantiate(original, position, Quaternion.Euler(euler));
        action.storeObject.Value = spawnedObject;
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
        if (selfGameObject == null) {
            return;
        }

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

        if (IsObjectInRegistry(gameObject)) {
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
        Vector3 scale;
        
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);

        if (data == null) {
            scale = action.vector.IsNone ? gameObject.transform.localScale : action.vector.Value;

            if (!action.x.IsNone) {
                scale.x = action.x.Value;
            }
            
            if (!action.y.IsNone) {
                scale.y = action.y.Value;
            }
            
            if (!action.z.IsNone) {
                scale.z = action.z.Value;
            }
        } else {
            scale = new Vector3(
                data.Packet.ReadFloat(),
                data.Packet.ReadFloat(),
                data.Packet.ReadFloat()
            );
        }

        gameObject.transform.localScale = scale;
    }

    #endregion

    #region SetFsmBool

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetFsmBool action) {
        if (action.setValue == null) {
            return false;
        }

        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == action.Fsm.GameObject) {
            return false;
        }
        
        var setValue = action.setValue.Value;
        data.Packet.Write(setValue);
        
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetFsmBool action) {
        var setValue = data.Packet.ReadBool();

        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
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
    
    #region SetFsmInt

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetFsmInt action) {
        if (action.setValue == null) {
            return false;
        }

        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == action.Fsm.GameObject) {
            return false;
        }
        
        var setValue = action.setValue.Value;
        data.Packet.Write(setValue);
        
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetFsmInt action) {
        var setValue = data.Packet.ReadInt();

        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var fsm = ActionHelpers.GetGameObjectFsm(gameObject, action.fsmName.Value);
        if (fsm == null) {
            return;
        }

        var fsmInt = fsm.FsmVariables.GetFsmInt(action.variableName.Value);
        if (fsmInt == null) {
            return;
        }

        fsmInt.Value = setValue;
    }

    #endregion

    #region SetFsmFloat

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetFsmFloat action) {
        // TODO: if action.setValue can be a reference, make sure to network it
        if (action.setValue == null) {
            return false;
        }

        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == action.Fsm.GameObject) {
            return false;
        }
        
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetFsmFloat action) {
        if (action.setValue == null) {
            return;
        }

        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
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
    
    #region SetFsmString

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetFsmString action) {
        // TODO: if action.setValue can be a reference, make sure to network it
        if (action.setValue == null) {
            return false;
        }

        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == action.Fsm.GameObject) {
            return false;
        }
        
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetFsmString action) {
        if (action.setValue == null) {
            return;
        }

        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var fsm = ActionHelpers.GetGameObjectFsm(gameObject, action.fsmName.Value);
        if (fsm == null) {
            return;
        }

        var fsmString = fsm.FsmVariables.GetFsmString(action.variableName.Value);
        if (fsmString == null) {
            return;
        }

        fsmString.Value = action.setValue.Value;
    }

    #endregion
    
    #region SetParticleEmission

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetParticleEmission action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetParticleEmission action) {
        if (action.Fsm == null) {
            return;
        }

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
    
    #region SetParticleEmissionRate

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetParticleEmissionRate action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetParticleEmissionRate action) {
        if (action.gameObject == null) {
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
        particleSystem.emissionRate = action.emissionRate.Value;
#pragma warning restore CS0618
    }

    #endregion
    
    #region SetParticleEmissionSpeed

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetParticleEmissionSpeed action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetParticleEmissionSpeed action) {
        if (action.gameObject == null) {
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
        particleSystem.startSpeed = action.emissionSpeed.Value;
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

        if (!particleSystem.isPlaying && action.emit.Value <= 0) {
            particleSystem.Play();
        } else if (action.emit.Value > 0) {
            particleSystem.Emit(action.emit.Value);
        }
    }
    
    #endregion
    
    #region StopParticleEmitter

    private static bool GetNetworkDataFromAction(EntityNetworkData data, StopParticleEmitter action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, StopParticleEmitter action) {
        if (action.gameObject == null) {
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

        if (particleSystem.isPlaying) {
            particleSystem.Stop();
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
    
    #region GetChild

    private static bool GetNetworkDataFromAction(EntityNetworkData data, GetChild action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, GetChild action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);

        var result = ReflectionHelper.CallMethod<GameObject>(
            typeof(GetChild),
            "DoGetChildByName",
            gameObject,
            action.childName.Value,
            action.withTag.Value
        );

        action.storeResult.Value = result;
    }

    #endregion
    
    #region FindChild

    private static bool GetNetworkDataFromAction(EntityNetworkData data, FindChild action) {
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, FindChild action) {
        if (action.Fsm == null) {
            return;
        }
    
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
    
    #region SetProperty

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetProperty action) {
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetProperty action) {
        action.targetProperty.SetValue();
    }
    
    #endregion
    
    #region SetParent

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetParent action) {
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetParent action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var parent = action.parent.Value;
        gameObject.transform.parent = parent != null ? parent.transform : null;

        if (action.resetLocalPosition.Value) {
            gameObject.transform.localPosition = Vector3.zero;
        }

        if (action.resetLocalRotation.Value) {
            gameObject.transform.localRotation = Quaternion.identity;
        }
        
        if (parent == null) {
            var fsms = gameObject.GetComponents<PlayMakerFSM>();
            foreach (var fsm in fsms) {
                if (fsm.Fsm.Name.Equals("destroy_if_gameobject_null")) {
                    Object.Destroy(fsm);

                    Logger.Debug($"De-parented object contained \"{fsm.Fsm.Name}\" FSM, removing it");
                }
            }
        }
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
        
        data.Packet.Write(vector3.x);
        data.Packet.Write(vector3.y);
        data.Packet.Write(vector3.z);
        
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetPosition action) {
        Vector3 vector3;
        
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);

        if (data == null) {
            if (gameObject == null) {
                return;
            }
            
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
        } else {
            vector3 = new Vector3(
                data.Packet.ReadFloat(),
                data.Packet.ReadFloat(),
                data.Packet.ReadFloat()
            );
            
            if (gameObject == null) {
                return;
            }
        }

        if (action.space == Space.World) {
            gameObject.transform.position = vector3;
        } else {
            gameObject.transform.localPosition = vector3;
        }
    }
    
    #endregion
    
    #region SetRotation

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetRotation action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return false;
        }

        if (IsObjectInRegistry(gameObject)) {
            Logger.Debug("Tried getting SetPosition network data, but entity is in registry");
            return false;
        }

        Vector3 vector3;
        if (action.quaternion.IsNone) {
            if (action.vector.IsNone) {
                if (action.space == Space.Self) {
                    vector3 = gameObject.transform.localEulerAngles;
                } else {
                    vector3 = gameObject.transform.eulerAngles;
                }
            } else {
                vector3 = action.vector.Value;
            }
        } else {
            vector3 = action.quaternion.Value.eulerAngles;
        }

        if (!action.xAngle.IsNone) {
            vector3.x = action.xAngle.Value;
        }

        if (!action.yAngle.IsNone) {
            vector3.y = action.yAngle.Value;
        }

        if (!action.zAngle.IsNone) {
            vector3.z = action.zAngle.Value;
        }
        
        data.Packet.Write(vector3.x);
        data.Packet.Write(vector3.y);
        data.Packet.Write(vector3.z);
        
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetRotation action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);

        if (data == null) {
            Logger.Error("No data passed for applying SetRotation action");
            return;
        }
        
        var vector3 = new Vector3(
            data.Packet.ReadFloat(),
            data.Packet.ReadFloat(),
            data.Packet.ReadFloat()
        );
            
        if (gameObject == null) {
            return;
        }

        if (action.space == Space.Self) {
            gameObject.transform.localEulerAngles = vector3;
        } else {
            gameObject.transform.eulerAngles = vector3;
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
        
        data.Packet.Write((short) action.spawnMin.Value);
        data.Packet.Write((short) action.spawnMax.Value);
        
        data.Packet.Write(action.speedMin.Value);
        data.Packet.Write(action.speedMax.Value);
        data.Packet.Write(action.angleMin.Value);
        data.Packet.Write(action.angleMax.Value);

        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SpawnBlood action) {
        var spawnMin = data.Packet.ReadShort();
        var spawnMax = data.Packet.ReadShort();

        var speedMin = data.Packet.ReadFloat();
        var speedMax = data.Packet.ReadFloat();

        var angleMin = data.Packet.ReadFloat();
        var angleMax = data.Packet.ReadFloat();
        
        var position = action.position.Value;
        if (action.spawnPoint.Value != null) {
            position += action.spawnPoint.Value.transform.position;
        }
        
        GlobalPrefabDefaults.Instance.SpawnBlood(
            position,
            spawnMin,
            spawnMax,
            speedMin,
            speedMax,
            angleMin,
            angleMax,
            action.colorOverride.IsNone ? new Color?() : action.colorOverride.Value
        );
    }

    #endregion
    
    #region SendEventByName

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SendEventByName action) {
        if (action.eventTarget.gameObject.GameObject.Value == action.Fsm.GameObject.gameObject) {
            return false;
        }

        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SendEventByName action) {
        if (action.delay.Value < 1.0 / 1000.0) {
            action.Fsm.Event(action.eventTarget, action.sendEvent.Value);
        } else {
            // We need to delay the event sending ourselves, because the FSM that we are executing in is not enabled
            // The usual implementation of SendEventByName will thus not work
            MonoBehaviourUtil.Instance.StartCoroutine(DelayEvent());

            IEnumerator DelayEvent() {
                yield return new WaitForSeconds(action.delay.Value);
                
                action.Fsm.Event(action.eventTarget, action.sendEvent.Value);
            }
        }
    }

    #endregion
    
    #region SendEventByNameV2

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SendEventByNameV2 action) {
        if (action.eventTarget.gameObject.GameObject.Value == action.Fsm.GameObject.gameObject) {
            return false;
        }

        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SendEventByNameV2 action) {
        if (action.delay.Value < 1.0 / 1000.0) {
            action.Fsm.Event(action.eventTarget, action.sendEvent.Value);
        } else {
            // We need to delay the event sending ourselves, because the FSM that we are executing in is not enabled
            // The usual implementation of SendEventByNameV2 will thus not work
            MonoBehaviourUtil.Instance.StartCoroutine(DelayEvent());

            IEnumerator DelayEvent() {
                yield return new WaitForSeconds(action.delay.Value);
                
                action.Fsm.Event(action.eventTarget, action.sendEvent.Value);
            }
        }
    }

    #endregion
    
    #region SendHealthManagerDeathEvent

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SendHealthManagerDeathEvent action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SendHealthManagerDeathEvent action) {
        var gameObject = action.target.OwnerOption == OwnerDefaultOption.UseOwner
            ? action.Owner
            : action.target.GameObject.Value;

        if (gameObject == null) {
            return;
        }

        var healthManager = gameObject.GetComponent<HealthManager>();
        if (healthManager == null) {
            return;
        }
        
        healthManager.SendDeathEvent();
    }

    #endregion
    
    #region ActivateAllChildren

    private static bool GetNetworkDataFromAction(EntityNetworkData data, ActivateAllChildren action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, ActivateAllChildren action) {
        var gameObject = action.gameObject.Value;
        if (gameObject == null) {
            return;
        }

        foreach (UnityEngine.Component component in gameObject.transform) {
            component.gameObject.SetActive(action.activate);
        }
    }

    #endregion
    
    #region SetBoxCollider2DSizeVector

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetBoxCollider2DSizeVector action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetBoxCollider2DSizeVector action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject1);
        if (gameObject == null) {
            return;
        }

        var collider = gameObject.GetComponent<BoxCollider2D>();
        if (collider == null) {
            return;
        }

        if (!action.size.IsNone) {
            collider.size = action.size.Value;
        }

        if (!action.offset.IsNone) {
            collider.offset = action.offset.Value;
        }
    }

    #endregion
    
    #region SetVelocityAsAngle

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetVelocityAsAngle action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return false;
        }
        
        if (IsObjectInRegistry(gameObject)) {
            Logger.Debug("Tried getting SetVelocityAsAngle network data, but entity is in registry");
            return false;
        }

        data.Packet.Write(action.speed.Value);
        data.Packet.Write(action.angle.Value);
        
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetVelocityAsAngle action) {
        var speed = data.Packet.ReadFloat();
        var angle = data.Packet.ReadFloat();
        
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }
        
        var rigidbody = gameObject.GetComponent<Rigidbody2D>();
        if (rigidbody == null) {
            return;
        }

        var x = speed * Mathf.Cos(angle * ((float) System.Math.PI / 180f));
        var y = speed * Mathf.Sin(angle * ((float) System.Math.PI / 180f));

        rigidbody.velocity = new Vector2(x, y);
    }

    #endregion
    
    #region iTweenMoveBy

    private static bool GetNetworkDataFromAction(EntityNetworkData data, iTweenMoveBy action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return false;
        }

        if (IsObjectInRegistry(gameObject)) {
            return false;
        }
    
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, iTweenMoveBy action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }
        
        var id = ReflectionHelper.GetField<iTweenMoveBy, int>(action, "itweenID");

        var args = new Hashtable {
            { "amount", action.vector.IsNone ? Vector3.zero : action.vector.Value }, 
            {
                action.speed.IsNone ? "time" : "speed",
                (float) (action.speed.IsNone
                    ? (action.time.IsNone ? 1.0 : action.time.Value)
                    : (double) action.speed.Value)
            },
            { "delay", (float) (action.delay.IsNone ? 0.0 : (double) action.delay.Value) },
            { "easetype", action.easeType },
            { "looptype", action.loopType },
            { "oncomplete", "iTweenOnComplete" },
            { "oncompleteparams", id },
            { "onstart", "iTweenOnStart" },
            { "onstartparams", id },
            { "ignoretimescale", !action.realTime.IsNone && action.realTime.Value },
            { "space", action.space },
            { "name", action.id.IsNone ? "" : (object) action.id.Value },
            { "axis", action.axis == iTweenFsmAction.AxisRestriction.none ? "" : (object) Enum.GetName(typeof (iTweenFsmAction.AxisRestriction), action.axis) }
        };

        if (!action.orientToPath.IsNone) {
            args.Add("orienttopath", action.orientToPath.Value);
        }

        if (!action.lookAtObject.IsNone) {
            args.Add("looktarget",
                action.lookAtVector.IsNone
                    ? action.lookAtObject.Value.transform.position
                    : action.lookAtObject.Value.transform.position + action.lookAtVector.Value
            );
        } else if (!action.lookAtVector.IsNone) {
            args.Add("looktarget", action.lookAtVector.Value);
        }

        if (!action.lookAtObject.IsNone || !action.lookAtVector.IsNone) {
            args.Add("looktime", (float) (action.lookTime.IsNone ? 0.0 : (double) action.lookTime.Value));
        }

        ReflectionHelper.SetField(action, "itweenType", "move");

        iTween.MoveBy(gameObject, args);
    }

    #endregion
    
    #region iTweenScaleTo

    private static bool GetNetworkDataFromAction(EntityNetworkData data, iTweenScaleTo action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return false;
        }

        if (IsObjectInRegistry(gameObject)) {
            return false;
        }
        
        return action.loopType == iTween.LoopType.none;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, iTweenScaleTo action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var vector = action.vectorScale.IsNone ? Vector3.zero : action.vectorScale.Value;

        if (!action.transformScale.IsNone && action.transformScale.Value) {
            vector = action.transformScale.Value.transform.localScale + vector;
        }

        var id = ReflectionHelper.GetField<iTweenScaleTo, int>(action, "itweenID");
        
        iTween.ScaleTo(gameObject, iTween.Hash(
            "scale", 
            vector,
            "name", 
            action.id.IsNone ? "" : action.id.Value, 
            action.speed.IsNone ? "time" : "speed", 
            (float) (action.speed.IsNone ? action.time.IsNone ? 1.0 : action.time.Value : (double) action.speed.Value), 
            "delay", 
            (float) (action.delay.IsNone ? 0.0 : (double) action.delay.Value), 
            "easetype", 
            action.easeType, 
            "looptype",
            action.loopType, 
            "oncomplete", 
            "iTweenOnComplete", 
            "oncompleteparams", 
            id, 
            "onstart", 
            "iTweenOnStart", 
            "onstartparams", 
            id, 
            "ignoretimescale", 
            (action.realTime.IsNone ? 0 : action.realTime.Value ? 1 : 0) > 0
        ));
    }

    #endregion
    
    #region SetTag

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetTag action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetTag action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        gameObject.tag = action.tag.Value;
    }

    #endregion
    
    #region DestroyObject

    private static bool GetNetworkDataFromAction(EntityNetworkData data, DestroyObject action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, DestroyObject action) {
        var gameObject = action.gameObject.Value;
        if (gameObject == null) {
            return;
        }

        var delay = action.delay.Value;
        if (delay <= 0) {
            Object.Destroy(gameObject);
        } else {
            Object.Destroy(gameObject, delay);
        }

        if (action.detachChildren.Value) {
            gameObject.transform.DetachChildren();
        }
    }

    #endregion
    
    #region SetGravity2dScale

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetGravity2dScale action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return false;
        }
        
        if (IsObjectInRegistry(gameObject)) {
            Logger.Debug("Tried getting SetGravity2dScale network data, but entity is in registry");
            return false;
        }
        
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetGravity2dScale action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var rigidBody = gameObject.GetComponent<Rigidbody2D>();
        if (rigidBody == null) {
            return;
        }

        rigidBody.gravityScale = action.gravityScale.Value;
    }

    #endregion
    
    #region SetCollider

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetCollider action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return false;
        }
        
        if (IsObjectInRegistry(gameObject)) {
            Logger.Debug("Tried getting SetCollider network data, but entity is in registry");
            return false;
        }
        
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetCollider action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var collider = gameObject.GetComponent<Collider2D>();
        if (collider == null) {
            return;
        }

        collider.enabled = action.active.Value;
    }

    #endregion
    
    #region SetStringValue

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetStringValue action) {
        if (action.stringVariable == null || action.stringValue == null) {
            return false;
        }
        
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetStringValue action) {
        action.stringVariable.Value = action.stringValue.Value;
    }

    #endregion
    
    #region GetRandomChild

    private static bool GetNetworkDataFromAction(EntityNetworkData data, GetRandomChild action) {
        if (!RandomActionValues.TryGetValue(action, out var queue)) {
            return false;
        }

        if (queue.Count == 0) {
            Logger.Debug("Getting data for GetRandomChild has not enough items in queue");
            return false;
        }

        var randomIndex = (int) queue.Dequeue();
        data.Packet.Write((byte) randomIndex);
        
        queue.Clear();

        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, GetRandomChild action) {
        var randomIndex = data.Packet.ReadByte();

        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var childCount = gameObject.transform.childCount;
        if (childCount == 0) {
            return;
        }

        action.storeResult.Value = gameObject.transform.GetChild(randomIndex).gameObject;
    }
    
    #endregion
    
    #region DestroyComponent

    private static bool GetNetworkDataFromAction(EntityNetworkData data, DestroyComponent action) {
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, DestroyComponent action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var component = gameObject.GetComponent(ReflectionUtils.GetGlobalType(action.component.Value));
        if (component == null) {
            return;
        }

        Object.Destroy(component);
    }
    
    #endregion
    
    #region AddComponent

    private static bool GetNetworkDataFromAction(EntityNetworkData data, AddComponent action) {
        if (action.removeOnExit.Value) {
            Logger.Debug("Tried getting data for AddComponent action, but removeOnExit is true");
            return false;
        }
        
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, AddComponent action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var component = gameObject.AddComponent(ReflectionUtils.GetGlobalType(action.component.Value));
        action.storeComponent.Value = component;
    }
    
    #endregion
    
    #region PreBuildTK2DSprites

    private static bool GetNetworkDataFromAction(EntityNetworkData data, PreBuildTK2DSprites action) {
        return true;
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, PreBuildTK2DSprites action) {
        var gameObject = action.gameObject.Value;
        if (gameObject == null) {
            return;
        }

        tk2dSprite[] sprites;

        if (action.useChildren) {
            sprites = gameObject.GetComponentsInChildren<tk2dSprite>(true);
        } else {
            sprites = gameObject.GetComponents<tk2dSprite>();
        }

        foreach (var sprite in sprites) {
            sprite.ForceBuild();
        }
    }
    
    #endregion
    
    #region GetPosition

    private static bool GetNetworkDataFromAction(EntityNetworkData data, GetPosition action) {
        Logger.Debug($"Getting network data for GetPosition: {action.Fsm.GameObject.name}, {action.Fsm.Name}");
        
        return action.Fsm.GameObject.name.StartsWith("Colosseum Manager") && action.Fsm.Name.Equals("Battle Control");
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, GetPosition action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var vector3 = action.space == Space.World ? gameObject.transform.position : gameObject.transform.localPosition;
        action.vector.Value = vector3;
        action.x.Value = vector3.x;
        action.y.Value = vector3.y;
        action.z.Value = vector3.z;
    }
    
    #endregion
    
    #region CallMethodProper

    private static bool GetNetworkDataFromAction(EntityNetworkData data, CallMethodProper action) {
        Logger.Debug($"Getting network data for CallMethodProper: {action.Fsm.GameObject.name}, {action.Fsm.Name}");
        
        return action.Fsm.GameObject.name.StartsWith("Colosseum Manager") && 
               action.Fsm.Name.Equals("Battle Control") || 
               action.Fsm.GameObject.name.StartsWith("Mantis Lord Throne") && 
               action.Fsm.Name.Equals("Mantis Throne Main");
    }
    
    private static void ApplyNetworkDataFromAction(EntityNetworkData data, CallMethodProper action) {
        if (action.behaviour.Value == null) {
            return;
        }

        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var component = gameObject.GetComponent(action.behaviour.Value) as MonoBehaviour;
        if (component == null) {
            return;
        }

        var type = component.GetType();
        var methodInfo = type.GetMethod(action.methodName.Value);
        if (methodInfo == null) {
            return;
        }

        var parameterInfo = methodInfo.GetParameters();

        object obj;
        if (parameterInfo.Length == 0) {
            obj = methodInfo.Invoke(component, null);
        } else {
            var paramArray = new object[action.parameters.Length];

            for (var i = 0; i < action.parameters.Length; i++) {
                var fsmVar = action.parameters[i];
                fsmVar.UpdateValue();
                paramArray[i] = fsmVar.GetValue();
            }

            try {
                obj = methodInfo.Invoke(component, paramArray);
            } catch (Exception e) {
                Logger.Error($"Error applying CallMethodProper:\n{e}");
                return;
            }
        }

        if (action.storeResult.Type == VariableType.Unknown) {
            return;
        }

        action.storeResult.SetValue(obj);
    }
    
    #endregion
    
    #region AudioPlay

    private static bool GetNetworkDataFromAction(EntityNetworkData data, AudioPlay action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, AudioPlay action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null || !audioSource.enabled) {
            return;
        }
        
        var audioClip = action.oneShotClip.Value as AudioClip;
        if (audioClip == null) {
            audioSource.Play();

            if (action.volume.IsNone) {
                return;
            }

            audioSource.volume = action.volume.Value;
            return;
        }

        if (!action.volume.IsNone) {
            audioSource.PlayOneShot(audioClip, action.volume.Value);
            return;
        }

        audioSource.PlayOneShot(audioClip);
    }

    #endregion

    #region AudioPlaySimple

    private static bool GetNetworkDataFromAction(EntityNetworkData data, AudioPlaySimple action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, AudioPlaySimple action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null) {
            return;
        }

        var audioClip = action.oneShotClip.Value as AudioClip;
        if (audioClip == null) {
            if (!audioSource.isPlaying) {
                audioSource.Play();
            }

            if (!action.volume.IsNone) {
                audioSource.volume = action.volume.Value;
            }
        } else {
            if (!action.volume.IsNone) {
                audioSource.PlayOneShot(audioClip, action.volume.Value);
            } else {
                audioSource.PlayOneShot(audioClip);
            }
        }
    }

    #endregion
    
    #region AudioPlayerOneShot

    private static bool GetNetworkDataFromAction(EntityNetworkData data, AudioPlayerOneShot action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, AudioPlayerOneShot action) {
        // TODO: delay?

        if (action.audioClips.Length == 0) {
            return;
        }

        var audioPlayerPrefab = action.audioPlayer.Value;
        var audioPlayer = audioPlayerPrefab.Spawn(
            action.spawnPoint.Value.transform.position, Quaternion.Euler(Vector3.up)
        );

        var audioSource = audioPlayer.GetComponent<AudioSource>();

        action.storePlayer.Value = audioPlayer;

        var randomWeightedIndex = ActionHelpers.GetRandomWeightedIndex(action.weights);
        if (randomWeightedIndex != -1) {
            var audioClip = action.audioClips[randomWeightedIndex];
            if (audioClip != null) {
                audioSource.pitch = Random.Range(action.pitchMin.Value, action.pitchMax.Value);
                audioSource.PlayOneShot(audioClip);
            }
        }

        audioSource.volume = action.volume.Value;
    }

    #endregion
    
    #region AudioPlayerOneShotSingle

    private static bool GetNetworkDataFromAction(EntityNetworkData data, AudioPlayerOneShotSingle action) {
        if (action.audioPlayer.IsNone || action.spawnPoint.IsNone || action.spawnPoint.Value == null) {
            return false;
        }
        
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, AudioPlayerOneShotSingle action) {
        // TODO: delay?

        if (action.audioPlayer.IsNone || action.spawnPoint.IsNone || action.spawnPoint.Value == null) {
            return;
        }

        var audioPlayer = action.audioPlayer.Value;
        var position = action.spawnPoint.Value.transform.position;
        var up = Vector3.up;

        if (audioPlayer == null) {
            return;
        }

        audioPlayer = audioPlayer.Spawn(position, Quaternion.Euler(up));
        var audioSource = audioPlayer.GetComponent<AudioSource>();
        action.storePlayer.Value = audioPlayer;

        var audioClip = action.audioClip.Value as AudioClip;
        audioSource.pitch = Random.Range(action.pitchMin.Value, action.pitchMax.Value);
        audioSource.volume = action.volume.Value;

        if (audioClip == null) {
            return;
        }
        
        audioSource.PlayOneShot(audioClip);
    }

    #endregion
    
    #region SetAudioClip

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetAudioClip action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetAudioClip action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null) {
            return;
        }

        audioSource.clip = action.audioClip.Value as AudioClip;
    }

    #endregion
    
    #region SetAudioPitch

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetAudioPitch action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetAudioPitch action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null) {
            return;
        }

        audioSource.pitch = action.pitch.Value;
    }

    #endregion
    
    #region AudioStop
    
    private static bool GetNetworkDataFromAction(EntityNetworkData data, AudioStop action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, AudioStop action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null) {
            return;
        }
        
        audioSource.Stop();
    }

    #endregion
    
    #region SetAudioVolume
    
    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetAudioVolume action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetAudioVolume action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null) {
            return;
        }

        audioSource.volume = action.volume.Value;
    }

    #endregion
    
    #region AudioPlayRandom
    
    private static bool GetNetworkDataFromAction(EntityNetworkData data, AudioPlayRandom action) {
        if (action.audioClips.Length == 0) {
            return false;
        }
        
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, AudioPlayRandom action) {
        if (action.audioClips.Length == 0) {
            return;
        }

        var audioSource = action.gameObject.Value.GetComponent<AudioSource>();

        var randomWeightedIndex = ActionHelpers.GetRandomWeightedIndex(action.weights);
        if (randomWeightedIndex == -1) {
            return;
        }

        var audioClip = action.audioClips[randomWeightedIndex];
        if (audioClip == null) {
            return;
        }

        audioSource.pitch = Random.Range(action.pitchMin.Value, action.pitchMax.Value);
        audioSource.PlayOneShot(audioClip);
    }

    #endregion
    
    #region AudioPlayInState
    
    private static bool GetNetworkDataFromAction(EntityNetworkData data, AudioPlayInState action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, AudioPlayInState action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null) {
            return;
        }

        if (!audioSource.isPlaying) {
            audioSource.Play();
        }

        if (!action.volume.IsNone) {
            audioSource.volume = action.volume.Value;
        }
        
        var exitAction = () => {
            audioSource.Stop();
        };

        new ActionInState {
            Fsm = action.Fsm,
            StateName = action.State.Name,
            ExitAction = exitAction
        }.Register();
    }

    #endregion

    #region SpawnBloodTime
    
    private static bool GetNetworkDataFromAction(EntityNetworkData data, SpawnBloodTime action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SpawnBloodTime action) {
        var position = action.position.Value;
        if (action.spawnPoint.Value != null) {
            position += action.spawnPoint.Value.transform.position;
        }

        var spawnMin = (short) action.spawnMin.Value;
        var spawnMax = (short) action.spawnMax.Value;
        
        var speedMin = action.speedMin.Value;
        var speedMax = action.speedMax.Value;
        
        var angleMin = action.angleMin.Value;
        var angleMax = action.angleMax.Value;

        var color = action.colorOverride.IsNone ? new Color?() : action.colorOverride.Value;

        var coroutine = MonoBehaviourUtil.Instance.StartCoroutine(Behaviour());

        new ActionInState {
            Fsm = action.Fsm,
            StateName = action.State.Name,
            Coroutine = coroutine
        }.Register();
        
        IEnumerator Behaviour() {
            while (true) {
                yield return new WaitForSeconds(action.delay.Value);

                if (GlobalPrefabDefaults.Instance == null) {
                    break;
                }
                
                GlobalPrefabDefaults.Instance.SpawnBlood(
                    position,
                    spawnMin,
                    spawnMax,
                    speedMin,
                    speedMax,
                    angleMin,
                    angleMax,
                    color
                );
            }
        }
    }

    #endregion
    
    #region FlingObjectsFromGlobalPoolTime
    
    private static bool GetNetworkDataFromAction(EntityNetworkData data, FlingObjectsFromGlobalPoolTime action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, FlingObjectsFromGlobalPoolTime action) {
        var coroutine = MonoBehaviourUtil.Instance.StartCoroutine(Behaviour());
        
        new ActionInState {
            Fsm = action.Fsm,
            StateName = action.State.Name,
            Coroutine = coroutine
        }.Register();

        IEnumerator Behaviour() {
            while (true) {
                yield return new WaitForSeconds(action.frequency.Value);

                if (action.gameObject.Value == null) {
                    break;
                }

                var position = Vector3.zero;

                var spawnPoint = action.spawnPoint.Value;
                if (spawnPoint != null) {
                    position = spawnPoint.transform.position;
                    if (!action.position.IsNone) {
                        position += action.position.Value;
                    }
                } else if (!action.position.IsNone) {
                    position = action.position.Value;
                }

                var numSpawns = Random.Range(action.spawnMin.Value, action.spawnMax.Value + 1);
                for (var i = 0; i < numSpawns; i++) {
                    var gameObject = action.gameObject.Value.Spawn(position, Quaternion.Euler(Vector3.zero));

                    if (action.originVariationX != null) {
                        position.x += Random.Range(-action.originVariationX.Value, action.originVariationX.Value);
                    }

                    if (action.originVariationY != null) {
                        position.y += Random.Range(-action.originVariationY.Value, action.originVariationY.Value);
                    }

                    gameObject.transform.position = position;

                    var rigidBody = gameObject.GetComponent<Rigidbody2D>();
                    if (rigidBody == null) {
                        continue;
                    }

                    var speed = Random.Range(action.speedMin.Value, action.speedMax.Value);
                    var angle = Random.Range(action.angleMin.Value, action.angleMax.Value);

                    var x = speed * Mathf.Cos(angle * ((float) System.Math.PI / 180f));
                    var y = speed * Mathf.Sin(angle * ((float) System.Math.PI / 180f));

                    rigidBody.velocity = new Vector2(x, y);
                }
            }
        }
    }

    #endregion
    
    #region SendMessage

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SendMessage action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SendMessage action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        object parameter = null;
        switch (action.functionCall.ParameterType) {
            case "Array":
                parameter = action.functionCall.ArrayParameter.Values;
                break;
            case "Color":
                parameter = action.functionCall.ColorParameter.Value;
                break;
            case "Enum":
                parameter = action.functionCall.EnumParameter.Value;
                break;
            case "GameObject":
                parameter = action.functionCall.GameObjectParameter.Value;
                break;
            case "Material":
                parameter = action.functionCall.MaterialParameter.Value;
                break;
            case "Object":
                parameter = action.functionCall.ObjectParameter.Value;
                break;
            case "Quaternion":
                parameter = action.functionCall.QuaternionParameter.Value;
                break;
            case "Rect":
                parameter = action.functionCall.RectParamater.Value;
                break;
            case "Texture":
                parameter = action.functionCall.TextureParameter.Value;
                break;
            case "Vector2":
                parameter = action.functionCall.Vector2Parameter.Value;
                break;
            case "Vector3":
                parameter = action.functionCall.Vector3Parameter.Value;
                break;
            case "bool":
                parameter = action.functionCall.BoolParameter.Value;
                break;
            case "float":
                parameter = action.functionCall.FloatParameter.Value;
                break;
            case "int":
                parameter = action.functionCall.IntParameter.Value;
                break;
            case "string":
                parameter = action.functionCall.StringParameter.Value;
                break;
        }

        switch (action.delivery) {
            case SendMessage.MessageType.SendMessage:
                gameObject.SendMessage(action.functionCall.FunctionName, parameter, action.options);
                break;
            case SendMessage.MessageType.SendMessageUpwards:
                gameObject.SendMessageUpwards(action.functionCall.FunctionName, parameter, action.options);
                break;
            case SendMessage.MessageType.BroadcastMessage:
                gameObject.BroadcastMessage(action.functionCall.FunctionName, parameter, action.options);
                break;
        }
    }

    #endregion
    
    #region SetCircleCollider

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetCircleCollider action) {
        return action.gameObject != null;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetCircleCollider action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var collider = gameObject.GetComponent<CircleCollider2D>();
        if (collider != null) {
            collider.enabled = action.active.Value;
        }
    }

    #endregion
    
    #region SetPolygonCollider

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetPolygonCollider action) {
        return action.gameObject != null;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetPolygonCollider action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var collider = gameObject.GetComponent<PolygonCollider2D>();
        if (collider != null) {
            collider.enabled = action.active.Value;
        }
    }

    #endregion
    
    #region PreSpawnGameObjects

    private static bool GetNetworkDataFromAction(EntityNetworkData data, PreSpawnGameObjects action) {
        if (EntitySpawnEvent != null) {
            var spawnedEntity = false;
            
            var arr = action.storeArray.Values;
            for (var i = 0; i < arr.Length; i++) {
                var spawnedGo = (GameObject) arr[i];

                if (EntitySpawnEvent.Invoke(new EntitySpawnDetails {
                    Type = EntitySpawnType.FsmAction,
                    Action = action,
                    GameObject = spawnedGo
                })) {
                    Logger.Debug("Tried getting PreSpawnGameObjects network data, but spawned objects contains entity");
                    spawnedEntity = true;
                }
            }

            if (spawnedEntity) {
                return false;
            }
        }
        
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, PreSpawnGameObjects action) {
        if (action.prefab.Value == null) {
            return;
        }

        if (action.storeArray.IsNone) {
            return;
        }

        if (action.spawnAmount.Value <= 0 || action.spawnAmountMultiplier.Value <= 0) {
            return;
        }

        var length = action.spawnAmount.Value * action.spawnAmountMultiplier.Value;
        action.storeArray.Resize(length);

        for (var i = 0; i < length; i++) {
            var go = Object.Instantiate(action.prefab.Value);
            go.SetActive(false);
            action.storeArray.Values[i] = go;
        }
    }

    #endregion
    
    #region SetSpriteRenderer

    private static bool GetNetworkDataFromAction(EntityNetworkData data, SetSpriteRenderer action) {
        return action.gameObject != null;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, SetSpriteRenderer action) {
        var gameObject = action.Fsm.GetOwnerDefaultTarget(action.gameObject);
        if (gameObject == null) {
            return;
        }

        var spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) {
            spriteRenderer.enabled = action.active.Value;
        }
    }

    #endregion
    
    #region MoveLiftChain

    private static bool GetNetworkDataFromAction(EntityNetworkData data, MoveLiftChain action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, MoveLiftChain action) {
        var go = action.target.GetSafe(action);
        if (go == null) {
            return;
        }

        var liftChain = go.GetComponent<LiftChain>();
        if (liftChain == null) {
            return;
        }

        ReflectionHelper.CallMethod(action, "Apply", [liftChain]);
    }

    #endregion
    
    #region StopLiftChain

    private static bool GetNetworkDataFromAction(EntityNetworkData data, StopLiftChain action) {
        return true;
    }

    private static void ApplyNetworkDataFromAction(EntityNetworkData data, StopLiftChain action) {
        var go = action.target.GetSafe(action);
        if (go == null) {
            return;
        }

        var liftChain = go.GetComponent<LiftChain>();
        if (liftChain == null) {
            return;
        }

        ReflectionHelper.CallMethod(action, "Apply", [liftChain]);
    }

    #endregion

    /// <summary>
    /// Class that keeps track of an action that executes while in a certain state of the FSM.
    /// </summary>
    private class ActionInState {
        /// <summary>
        /// The FSM of the action that is executing.
        /// </summary>
        public HutongGames.PlayMaker.Fsm Fsm { get; init; }

        /// <summary>
        /// The name of the state in which this action executes.
        /// </summary>
        public string StateName { get; init; }

        /// <summary>
        /// The coroutine that should be stopped when the state is exited.
        /// </summary>
        public Coroutine Coroutine { private get; init; }

        /// <summary>
        /// The action that should be executed when the state is exited.
        /// </summary>
        public System.Action ExitAction { private get; init; }

        /// <summary>
        /// Register this action by adding it to the list.
        /// </summary>
        public void Register() {
            ActionsInState.Add(this);
        }

        /// <summary>
        /// Call when the state is exited, will stop the coroutine and execute the exit action.
        /// </summary>
        public void ExitState() {
            if (Coroutine != null) {
                MonoBehaviourUtil.Instance.StopCoroutine(Coroutine);
            }
            
            ExitAction?.Invoke();
        }
    }
}
