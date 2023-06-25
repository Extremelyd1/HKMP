using System;
using System.Collections.Generic;
using System.Linq;
using Hkmp.Collection;
using Hkmp.Fsm;
using Hkmp.Game.Client.Entity.Action;
using Hkmp.Game.Client.Entity.Component;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using HutongGames.PlayMaker;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;
using Vector2 = Hkmp.Math.Vector2;

namespace Hkmp.Game.Client.Entity;

/// <summary>
/// A networked entity that is either sending behaviour updates to the server or is entirely controlled by
/// updates from the server.
/// </summary>
internal class Entity {
    /// <summary>
    /// The net client for networking.
    /// </summary>
    private readonly NetClient _netClient;

    /// <summary>
    /// Whether the entity has a parent entity.
    /// </summary>
    private readonly bool _hasParent;

    /// <summary>
    /// The ID of the entity.
    /// </summary>
    public byte Id { get; }

    /// <summary>
    /// The type of the entity.
    /// </summary>
    public EntityType Type { get; }

    /// <summary>
    /// Host-client pair for the game objects.
    /// </summary>
    public HostClientPair<GameObject> Object { get; }

    /// <summary>
    /// Host-client pair for the sprite animators.
    /// </summary>
    private readonly HostClientPair<tk2dSpriteAnimator> _animator;

    /// <summary>
    /// Bi-directional lookup for animation clip names to IDs.
    /// </summary>
    private readonly BiLookup<string, byte> _animationClipNameIds;

    /// <summary>
    /// Host-client pair for the lists of FSMs on the entity.
    /// </summary>
    private readonly HostClientPair<List<PlayMakerFSM>> _fsms;

    /// <summary>
    /// Dictionary mapping data types to entity components.
    /// </summary>
    private readonly Dictionary<EntityComponentType, EntityComponent> _components;

    /// <summary>
    /// Dictionary mapping FSM actions to their entity action data instances.
    /// </summary>
    private readonly Dictionary<FsmStateAction, HookedEntityAction> _hookedActions;
    /// <summary>
    /// Set of FSM action types that have been hooked to prevent duplicate hooks.
    /// </summary>
    private readonly HashSet<Type> _hookedTypes;

    /// <summary>
    /// Whether the unity game object for the host entity was originally active.
    /// </summary>
    private readonly bool _originalIsActive;

    /// <summary>
    /// Whether the entity is controlled, i.e. in control by updates from the server.
    /// </summary>
    private bool _isControlled;

    /// <summary>
    /// The last position of the entity.
    /// </summary>
    private Vector3 _lastPosition;
    /// <summary>
    /// The last scale of the entity.
    /// </summary>
    private Vector3 _lastScale;
    /// <summary>
    /// Whether the game object for the entity was last active.
    /// </summary>
    private bool _lastIsActive;

    /// <summary>
    /// Whether to allow the client entity to animate itself.
    /// </summary>
    private bool _allowClientAnimation;

    /// <summary>
    /// List of snapshots for each FSM of a host entity that contain latest values for state and FSM variables.
    /// Used to check whether state/variables change and to update the server accordingly.
    /// </summary>
    private readonly List<FsmSnapshot> _fsmSnapshots;

    public Entity(
        NetClient netClient,
        byte id,
        EntityType type,
        GameObject hostObject,
        GameObject clientObject = null,
        params EntityComponentType[] types
    ) {
        _netClient = netClient;
        Id = id;

        Type = type;

        _isControlled = true;

        if (clientObject == null) {
            Object = new HostClientPair<GameObject> {
                Host = hostObject,
                Client = UnityEngine.Object.Instantiate(
                    hostObject,
                    hostObject.transform.position,
                    hostObject.transform.rotation
                )
            };

            _hasParent = false;
        } else {
            Object = new HostClientPair<GameObject> {
                Host = hostObject,
                Client = clientObject
            };

            _hasParent = true;
        }

        Object.Client.transform.localScale = _hasParent 
            ? Object.Host.transform.localScale 
            : Object.Host.transform.lossyScale;

        // Store whether the host object was active and set it not active until we know if we are scene host
        _originalIsActive = Object.Host.activeSelf;

        _lastIsActive = _hasParent ? Object.Host.activeSelf : Object.Host.activeInHierarchy;

        Logger.Info(
            $"Entity '{Object.Host.name}' was original active: {_originalIsActive}, last active: {_lastIsActive}");

        // Add a position interpolation component to the enemy so we can smooth out position updates
        Object.Client.AddComponent<PositionInterpolation>();

        // Register an update event to send position updates and check for certain value changes
        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdate;

        _animator = new HostClientPair<tk2dSpriteAnimator> {
            Host = Object.Host.GetComponent<tk2dSpriteAnimator>(),
            Client = Object.Client.GetComponent<tk2dSpriteAnimator>()
        };
        if (_animator.Host != null) {
            _animationClipNameIds = new BiLookup<string, byte>();

            var index = 0;
            foreach (var animationClip in _animator.Host.Library.clips) {
                if (_animationClipNameIds.ContainsFirst(animationClip.name)) {
                    continue;
                }

                _animationClipNameIds.Add(animationClip.name, (byte)index++);

                if (index > byte.MaxValue) {
                    Logger.Error($"Too many animation clips to fit in a byte for entity: {Object.Client.name}");
                    break;
                }
            }

            On.tk2dSpriteAnimator.Play_tk2dSpriteAnimationClip_float_float += OnAnimationPlayed;
        }
        
        // Always disallow the client object from being recycled, because it will simply be destroyed
        On.ObjectPool.Recycle_GameObject += (orig, obj) => {
            if (obj == Object.Client) {
                Logger.Debug($"Client object of entity: {Id}, {type} tried to be recycled");
                return;
            }

            orig(obj);
        };

        _fsms = new HostClientPair<List<PlayMakerFSM>> {
            Host = Object.Host.GetComponents<PlayMakerFSM>().ToList(),
            Client = Object.Client.GetComponents<PlayMakerFSM>().ToList()
        };

        _hookedActions = new Dictionary<FsmStateAction, HookedEntityAction>();
        _hookedTypes = new HashSet<Type>();
        _fsmSnapshots = new List<FsmSnapshot>();
        foreach (var fsm in _fsms.Host) {
            ProcessHostFsm(fsm);
        }

        // Remove all components that (re-)activate FSMs
        foreach (var fsmActivator in Object.Client.GetComponents<FSMActivator>()) {
            fsmActivator.StopAllCoroutines();
            UnityEngine.Object.Destroy(fsmActivator);
        }

        foreach (var fsm in _fsms.Client) {
            ProcessClientFsm(fsm);
        }

        _components = new Dictionary<EntityComponentType, EntityComponent>();
        HandleComponents(types);
        
        Object.Host.SetActive(false);
        Object.Client.SetActive(false);

        // // Debug code that logs each action's OnEnter method call
        // foreach (var fsm in _fsms.Host) {
        //     foreach (var state in fsm.FsmStates) {
        //         foreach (var action in state.Actions) {
        //             FsmActionHooks.RegisterFsmStateActionType(action.GetType(), stateAction => {
        //                 if (stateAction != action) {
        //                     return;
        //                 }
        //                 
        //                 Logger.Debug($"Entity ({Id}, {Type}) has host FSM enter action: {state.Name}, {action.GetType()}, {state.Actions.ToList().IndexOf(action)}");
        //             });
        //         }
        //     }
        // }
    }

    /// <summary>
    /// Processes the given FSM for the host entity by hooking supported FSM actions.
    /// </summary>
    /// <param name="fsm">The Playmaker FSM to process.</param>
    private void ProcessHostFsm(PlayMakerFSM fsm) {
        Logger.Info($"Processing host FSM: {fsm.Fsm.Name}");

        for (var i = 0; i < fsm.FsmStates.Length; i++) {
            var state = fsm.FsmStates[i];

            for (var j = 0; j < state.Actions.Length; j++) {
                var action = state.Actions[j];
                if (!action.Enabled) {
                    continue;
                }

                if (!EntityFsmActions.SupportedActionTypes.Contains(action.GetType())) {
                    continue;
                }

                _hookedActions[action] = new HookedEntityAction {
                    Action = action,
                    FsmIndex = _fsms.Host.IndexOf(fsm),
                    StateIndex = i,
                    ActionIndex = j
                };
                // Logger.Info($"Created hooked action: {action.GetType()}, {_fsms.Host.IndexOf(fsm)}, {state.Name}, {j}");

                if (!_hookedTypes.Contains(action.GetType())) {
                    _hookedTypes.Add(action.GetType());

                    FsmActionHooks.RegisterFsmStateActionType(action.GetType(), OnActionEntered);
                }
            }
        }

        var snapshot = new FsmSnapshot {
            CurrentState = fsm.ActiveStateName
        };

        foreach (var f in fsm.FsmVariables.FloatVariables) {
            snapshot.Floats.Add(f.Name, f.Value);
        }
        foreach (var i in fsm.FsmVariables.IntVariables) {
            snapshot.Ints.Add(i.Name, i.Value);
        }
        foreach (var b in fsm.FsmVariables.BoolVariables) {
            snapshot.Bools.Add(b.Name, b.Value);
        }
        foreach (var s in fsm.FsmVariables.StringVariables) {
            snapshot.Strings.Add(s.Name, s.Value);
        }
        foreach (var vec2 in fsm.FsmVariables.Vector2Variables) {
            snapshot.Vector2s.Add(vec2.Name, vec2.Value);
        }
        foreach (var vec3 in fsm.FsmVariables.Vector3Variables) {
            snapshot.Vector3s.Add(vec3.Name, vec3.Value);
        }
            
        _fsmSnapshots.Add(snapshot);
    }

    /// <summary>
    /// Processes the given FSM for the client entity by disabling it.
    /// </summary>
    /// <param name="fsm">The Playmaker FSM to process.</param>
    private void ProcessClientFsm(PlayMakerFSM fsm) {
        Logger.Info($"Processing client FSM: {fsm.Fsm.Name}");
        EntityInitializer.InitializeFsm(fsm);
        fsm.enabled = false;
    }

    /// <summary>
    /// Check the host and client objects for components that are supported for networking.
    /// </summary>
    private void HandleComponents(EntityComponentType[] types) {
        var addedComponentsString = $"Adding components to entity ({Object.Host.name}, {Id}):";
        
        var hostHealthManager = Object.Host.GetComponent<HealthManager>();
        var clientHealthManager = Object.Client.GetComponent<HealthManager>();
        if (hostHealthManager != null && clientHealthManager != null) {
            var healthManager = new HostClientPair<HealthManager> {
                Host = hostHealthManager,
                Client = clientHealthManager
            };

            var hmComponent = new HealthManagerComponent(
                _netClient,
                Id,
                Object,
                healthManager
            );
            _components[EntityComponentType.Death] = hmComponent;
            _components[EntityComponentType.Invincibility] = hmComponent;

            addedComponentsString += " Death Invincibility";
        }

        var climber = Object.Client.GetComponent<Climber>();
        if (climber != null) {
            _components[EntityComponentType.Climber] = new ClimberComponent(
                _netClient,
                Id,
                Object,
                climber
            );
            _components[EntityComponentType.Rotation] = new RotationComponent(
                _netClient,
                Id,
                Object
            );
            
            addedComponentsString += " Climber Rotation";
        }

        var hostCollider = Object.Host.GetComponent<Collider2D>();
        var clientCollider = Object.Client.GetComponent<Collider2D>();
        if (hostCollider != null && clientCollider != null) {
            Logger.Info($"Adding collider component to entity: {Object.Host.name}");

            var collider = new HostClientPair<Collider2D> {
                Host = hostCollider,
                Client = clientCollider
            };

            _components[EntityComponentType.Collider] = new ColliderComponent(
                _netClient,
                Id,
                Object,
                collider
            );
            
            addedComponentsString += " Collider";
        }

        var hostDamageHero = Object.Host.GetComponent<DamageHero>();
        var clientDamageHero = Object.Client.GetComponent<DamageHero>();
        if (hostDamageHero != null && clientDamageHero != null) {
            Logger.Info($"Adding DamageHero component to entity: {Object.Host.name}");

            var damageHero = new HostClientPair<DamageHero> {
                Host = hostDamageHero,
                Client = clientDamageHero
            };

            _components[EntityComponentType.DamageHero] = new DamageHeroComponent(
                _netClient,
                Id,
                Object,
                damageHero
            );
            
            addedComponentsString += " DamageHero";
        }
        
        var hostMeshRenderer = Object.Host.GetComponent<MeshRenderer>();
        var clientMeshRenderer = Object.Client.GetComponent<MeshRenderer>();
        if (hostMeshRenderer != null && clientMeshRenderer != null) {
            Logger.Info($"Adding MeshRenderer component to entity: {Object.Host.name}");

            var meshRenderer = new HostClientPair<MeshRenderer> {
                Host = hostMeshRenderer,
                Client = clientMeshRenderer
            };

            _components[EntityComponentType.MeshRenderer] = new MeshRendererComponent(
                _netClient,
                Id,
                Object,
                meshRenderer
            );
            
            addedComponentsString += " MeshRenderer";
        }
        
        // Find Walker MonoBehaviour and remove it from the client object
        var walker = Object.Client.GetComponent<Walker>();
        if (walker != null) {
            UnityEngine.Object.Destroy(walker);
        }
        
        // Find RigidBody2D MonoBehaviour and set it to be kinematic so it doesn't do physics on its own
        var rigidBody = Object.Client.GetComponent<Rigidbody2D>();
        if (rigidBody != null) {
            rigidBody.isKinematic = true;
        }

        // Instantiate all types defined in the entity registry, which are passed to the constructor
        foreach (var type in types) {
            _components[type] = ComponentFactory.InstantiateByType(type, _netClient, Id, Object);
            
            addedComponentsString += $" {type}";
        }

        Logger.Debug(addedComponentsString);
    }

    /// <summary>
    /// Callback method for entering a hooked FSM action.
    /// </summary>
    /// <param name="self">The FSM action instance that was entered.</param>
    private void OnActionEntered(FsmStateAction self) {
        if (_isControlled) {
            return;
        }

        if (!_hookedActions.TryGetValue(self, out var hookedEntityAction)) {
            return;
        }

        Logger.Info(
            $"Entity ({Id}, {Type}) hooked action: {self.Fsm.Name}, {self.State.Name}, {self.GetType()} ({hookedEntityAction.FsmIndex}, {hookedEntityAction.StateIndex}, {hookedEntityAction.ActionIndex})");

        var networkData = new EntityNetworkData {
            Type = EntityComponentType.Fsm
        };

        if (_fsms.Host.Count > 1) {
            networkData.Packet.Write((byte)hookedEntityAction.FsmIndex);
        }

        networkData.Packet.Write((byte)hookedEntityAction.StateIndex);
        networkData.Packet.Write((byte)hookedEntityAction.ActionIndex);

        // Only if the GetNetworkDataFromAction method returns true do we add the entity data
        // for sending
        if (EntityFsmActions.GetNetworkDataFromAction(networkData, self)) {
            _netClient.UpdateManager.AddEntityData(Id, networkData);
        }
    }

    /// <summary>
    /// Callback method for handling updates.
    /// </summary>
    private void OnUpdate() {
        if (Object.Host == null) {
            if (_lastIsActive) {
                // If the host object was active, but now it null (or destroyed in Unity), we can send
                // to the server that the entity can be regarded as inactive
                Logger.Info($"Entity '{Object.Client.name}' host object is null (or destroyed) and was active");

                _lastIsActive = false;

                _netClient.UpdateManager.UpdateEntityIsActive(
                    Id,
                    false
                );
            }

            return;
        }

        var hostObjectActive = Object.Host.activeSelf;

        if (_isControlled) {
            if (hostObjectActive) {
                Logger.Info($"Entity '{Object.Host.name}' host object became active, re-disabling");
                Object.Host.SetActive(false);
            }

            return;
        }

        var transform = Object.Host.transform;

        var newPosition = _hasParent ? transform.localPosition : transform.position;
        if (newPosition != _lastPosition) {
            _lastPosition = newPosition;

            _netClient.UpdateManager.UpdateEntityPosition(
                Id,
                new Vector2(newPosition.x, newPosition.y)
            );
        }

        var newScale = _hasParent ? transform.localScale : transform.lossyScale;
        var newScaleX = newScale.x > 0;
        var newScaleY = newScale.y > 0;
        var lastScaleX = _lastScale.x > 0;
        var lastScaleY = _lastScale.y > 0;
        if (newScaleX != lastScaleX || newScaleY != lastScaleY) {
            _lastScale = newScale;

            byte scaleToSend = 0;
            if (newScaleX) {
                scaleToSend |= 1;
            }

            if (newScaleY) {
                scaleToSend |= 2;
            }

            _netClient.UpdateManager.UpdateEntityScale(
                Id,
                scaleToSend
            );
        }

        var newActive = _hasParent ? Object.Host.activeSelf : Object.Host.activeInHierarchy;
        if (newActive != _lastIsActive) {
            _lastIsActive = newActive;

            Logger.Info($"Entity '{Object.Host.name}' changed active: {newActive}");

            _netClient.UpdateManager.UpdateEntityIsActive(
                Id,
                newActive
            );
        }

        for (byte fsmIndex = 0; fsmIndex < _fsms.Host.Count; fsmIndex++) {
            var fsm = _fsms.Host[fsmIndex];
            var snapshot = _fsmSnapshots[fsmIndex];

            var data = new EntityHostFsmData();

            var lastStateName = snapshot.CurrentState;
            if (fsm.ActiveStateName != lastStateName) {
                snapshot.CurrentState = fsm.ActiveStateName;

                data.Types.Add(EntityHostFsmData.Type.State);
                data.CurrentState = (byte) Array.IndexOf(fsm.FsmStates, fsm.Fsm.ActiveState);
                
                Logger.Debug($"Entity ({Id}, {Type}) host changed states: {lastStateName}, {fsm.ActiveStateName}");
            }

            // Define a method that allows generalization of checking for changes in all FSM variables
            void CondAddData<VarType, BaseType, DataType>(
                VarType[] fsmVars,
                Dictionary<string, BaseType> snapshotDict,
                Func<VarType, string> fsmVarName,
                Func<VarType, BaseType> fsmVarValue,
                EntityHostFsmData.Type type,
                Dictionary<byte, DataType> dataDict
            ) {
                for (byte i = 0; i < fsmVars.Length; i++) {
                    var fsmVar = fsmVars[i];

                    var name = fsmVarName.Invoke(fsmVar);
                    if (!snapshotDict.TryGetValue(name, out var lastValue)) {
                        Logger.Warn($"No last value found for FSM var: {name}");
                        continue;
                    }

                    var value = fsmVarValue.Invoke(fsmVar);
                    if (!value.Equals(lastValue)) {
                        // Update the value in the snapshot since it changed
                        snapshotDict[name] = value;

                        data.Types.Add(type);
                        // Some funky casting here to make sure we can use this method with Vector2 and Vector3
                        // Since there is a mismatch between our Hkmp.Math.Vector2 and Unity's Vector2
                        // But our types have explicit converters, so casting is possible
                        if (value is UnityEngine.Vector2 vec2) {
                            dataDict[i] = (DataType) (object) (Vector2) vec2;
                        } else if (value is Vector3 vec3) {
                            dataDict[i] = (DataType) (object) (Hkmp.Math.Vector3) vec3;
                        } else {
                            dataDict[i] = (DataType) (object) value;
                        }
                    }
                }
            }

            CondAddData(
                fsm.FsmVariables.FloatVariables, 
                snapshot.Floats,
                fsmFloat => fsmFloat.Name,
                fsmFloat => fsmFloat.Value,
                EntityHostFsmData.Type.Floats,
                data.Floats
            );
            CondAddData(
                fsm.FsmVariables.IntVariables, 
                snapshot.Ints,
                fsmInt => fsmInt.Name,
                fsmInt => fsmInt.Value,
                EntityHostFsmData.Type.Ints,
                data.Ints
            );
            CondAddData(
                fsm.FsmVariables.BoolVariables, 
                snapshot.Bools,
                fsmBool => fsmBool.Name,
                fsmBool => fsmBool.Value,
                EntityHostFsmData.Type.Bools,
                data.Bools
            );
            CondAddData(
                fsm.FsmVariables.StringVariables, 
                snapshot.Strings,
                fsmString => fsmString.Name,
                fsmString => fsmString.Value,
                EntityHostFsmData.Type.Strings,
                data.Strings
            );
            CondAddData(
                fsm.FsmVariables.Vector2Variables, 
                snapshot.Vector2s,
                fsmVec2 => fsmVec2.Name,
                fsmVec2 => fsmVec2.Value,
                EntityHostFsmData.Type.Vector2s,
                data.Vec2s
            );
            CondAddData(
                fsm.FsmVariables.Vector3Variables, 
                snapshot.Vector3s,
                fsmVec3 => fsmVec3.Name,
                fsmVec3 => fsmVec3.Value,
                EntityHostFsmData.Type.Vector3s,
                data.Vec3s
            );

            if (data.Types.Count > 0) {
                _netClient.UpdateManager.AddEntityHostFsmData(Id, fsmIndex, data);
            }
        }
    }

    /// <summary>
    /// Callback method for when the sprite animator plays an animation.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The sprite animator instance.</param>
    /// <param name="clip">The animation clip that was played.</param>
    /// <param name="clipStartTime">The start time of the animation clip.</param>
    /// <param name="overrideFps">The FPS override for the clip.</param>
    private void OnAnimationPlayed(
        On.tk2dSpriteAnimator.orig_Play_tk2dSpriteAnimationClip_float_float orig,
        tk2dSpriteAnimator self,
        tk2dSpriteAnimationClip clip,
        float clipStartTime,
        float overrideFps
    ) {
        if (self == _animator.Client) {
            if (!_allowClientAnimation) {
                Logger.Info($"Entity '{Object.Client.name}' client animator tried playing animation");
            } else {
                // Logger.Info($"Entity '{_object.Client.name}' client animator was allowed to play animation");

                orig(self, clip, clipStartTime, overrideFps);

                _allowClientAnimation = false;
            }

            return;
        }

        orig(self, clip, clipStartTime, overrideFps);

        if (self != _animator.Host) {
            return;
        }

        if (_isControlled) {
            return;
        }

        if (!_animationClipNameIds.TryGetValue(clip.name, out var animationId)) {
            Logger.Warn($"Entity '{Object.Client.name}' played unknown animation: {clip.name}");
            return;
        }

        Logger.Info($"Entity '{Object.Host.name}' sends animation: {clip.name}, {animationId}, {clip.wrapMode}");
        _netClient.UpdateManager.UpdateEntityAnimation(
            Id,
            animationId,
            (byte)clip.wrapMode
        );
    }

    /// <summary>
    /// Initializes the entity when the client user is the scene host.
    /// </summary>
    public void InitializeHost() {
        Object.Host.SetActive(_originalIsActive);

        // Also update the last active variable to account for this potential change
        // Otherwise we might trigger the update sending of activity twice
        _lastIsActive = _hasParent ? Object.Host.activeSelf : Object.Host.activeInHierarchy;

        Logger.Info(
            $"Initializing entity '{Object.Host.name}' with active: {_originalIsActive}, sending active: {_lastIsActive}");

        _netClient.UpdateManager.UpdateEntityIsActive(Id, _lastIsActive);

        _isControlled = false;

        foreach (var component in _components.Values) {
            component.IsControlled = false;
            component.InitializeHost();
        }
    }

    /// <summary>
    /// Makes the entity a host entity if the client user became the scene host.
    /// </summary>
    public void MakeHost() {
        Logger.Info($"Making entity ({Id}, {Type}) a host entity");

        // If the client object is null, we don't have to care about doing anything for the host object anymore
        if (Object.Client == null) {
            if (Object.Host != null) {
                Object.Host.SetActive(false);
            }
            
            _isControlled = false;

            foreach (var component in _components.Values) {
                component.IsControlled = false;
            }

            return;
        }

        if (_hasParent) {
            Object.Host.transform.localPosition = _lastPosition = Object.Client.transform.localPosition;
            Object.Host.transform.localScale = _lastScale = Object.Client.transform.localScale;
        } else {
            var clientPos = Object.Client.transform.localPosition;
            var parentPos = Vector3.zero;
            if (Object.Host.transform.parent != null) {
                parentPos = Object.Host.transform.parent.position;
            }
        
            var newPosX = clientPos.x - parentPos.x;
            var newPosY = clientPos.y - parentPos.y;
            var newPosZ = clientPos.z - parentPos.z;
        
            Object.Host.transform.localPosition = _lastPosition = new Vector3(newPosX, newPosY, newPosZ);
            
            // Since the scale of the client object is the entire scale we have and the host object scale can be in a
            // hierarchy, we need to calculate what the new local scale of the host will be to match the client scale
            var clientScale = Object.Client.transform.localScale;
            var hostLocalScale = Object.Host.transform.localScale;
            var hostLossyScale = Object.Host.transform.lossyScale;
            
            var newScaleX = hostLocalScale.x == 0 || hostLossyScale.x == 0
                ? 0f
                : clientScale.x / (hostLossyScale.x / hostLocalScale.x);
            var newScaleY = hostLocalScale.y == 0 || hostLossyScale.y == 0
                ? 0f
                : clientScale.y / (hostLossyScale.y / hostLocalScale.y);
            var newScaleZ = hostLocalScale.z == 0 || hostLossyScale.z == 0
                ? 0f
                : clientScale.z / (hostLossyScale.z / hostLocalScale.z);
        
            Object.Host.transform.localScale = _lastScale = new Vector3(newScaleX, newScaleY, newScaleZ);
        }

        // Make sure that the sprite animator doesn't play the default clip after enabling the object
        if (_animator.Host != null) {
            _animator.Host.playAutomatically = false;
        }

        var clientActive = Object.Client.activeSelf;
        Object.Client.SetActive(false);
        Object.Host.SetActive(clientActive);

        if (clientActive) {
            var rigidBody = Object.Host.GetComponent<Rigidbody2D>();
            if (rigidBody != null) {
                rigidBody.isKinematic = false;
            }
        }

        _lastIsActive = _hasParent ? Object.Host.activeSelf : Object.Host.activeInHierarchy;
        
        _isControlled = false;

        foreach (var component in _components.Values) {
            component.IsControlled = false;
        }

        if (_animator.Client != null) {
            var currentClip = _animator.Client.CurrentClip;
            if (currentClip != null) {
                var clientAnimation = currentClip.name;
                var wrapMode = currentClip.wrapMode;
            
                Logger.Debug($"MakeHost ({Id}, {Type}) animation: {clientAnimation}, {wrapMode}");
            
                LateUpdateAnimation(_animator.Host, clientAnimation, wrapMode);   
            }
        }

        for (var fsmIndex = 0; fsmIndex < _fsms.Host.Count; fsmIndex++) {
            var fsm = _fsms.Host[fsmIndex];

            // Force initialize the host FSM, since it might have been disabled before initializing
            EntityInitializer.InitializeFsm(fsm);
            
            var snapshot = _fsmSnapshots[fsmIndex];

            foreach (var pair in snapshot.Floats) {
                fsm.FsmVariables.GetFsmFloat(pair.Key).Value = pair.Value;
            }
            foreach (var pair in snapshot.Ints) {
                fsm.FsmVariables.GetFsmInt(pair.Key).Value = pair.Value;
            }
            foreach (var pair in snapshot.Bools) {
                fsm.FsmVariables.GetFsmBool(pair.Key).Value = pair.Value;
            }
            foreach (var pair in snapshot.Strings) {
                fsm.FsmVariables.GetFsmString(pair.Key).Value = pair.Value;
            }
            foreach (var pair in snapshot.Vector2s) {
                fsm.FsmVariables.GetFsmVector2(pair.Key).Value = pair.Value;
            }
            foreach (var pair in snapshot.Vector3s) {
                fsm.FsmVariables.GetFsmVector3(pair.Key).Value = pair.Value;
            }

            // Before setting the state, we replace the actions of the to-be state to only include the ones that
            // should be executed again (including actions with "everyFrame" on true or that continuously check
            // collisions for example).
            var state = fsm.GetState(snapshot.CurrentState);
            if (state == null) {
                Logger.Debug("  Not setting FSM state, because current state is empty");
                continue;
            }
            
            Logger.Debug($"  Setting FSM state: {snapshot.CurrentState}");
            
            var oldActions = state.Actions;
            var newActions = oldActions.Where(ActionRegistry.IsActionContinuous).ToArray();

            // Replace the actions, set the state and reset the actions again
            state.Actions = newActions;
            fsm.SetState(snapshot.CurrentState);
            state.Actions = oldActions;
        }
    }

    /// <summary>
    /// Updates the position of the client entity.
    /// </summary>
    /// <param name="position">The new position.</param>
    public void UpdatePosition(Vector2 position) {
        var unityPos = new Vector3(
            position.X, 
            position.Y,
            _hasParent ? Object.Host.transform.localPosition.z : Object.Host.transform.position.z
        );

        if (Object.Client == null) {
            return;
        }

        var positionInterpolation = Object.Client.GetComponent<PositionInterpolation>();
        if (positionInterpolation == null) {
            return;
        }

        positionInterpolation.SetNewPosition(unityPos);
    }

    /// <summary>
    /// Updates the scale of the client entity.
    /// </summary>
    /// <param name="scale">The new scale.</param>
    public void UpdateScale(byte scale) {
        var transform = Object.Client.transform;
        var localScale = transform.localScale;
        var currentScaleX = localScale.x;
        var currentScaleY = localScale.y;

        var scaleXPos = (scale & 1) != 0;
        var scaleYPos = (scale & 2) != 0;
        
        // We use the host scale as reference, either lossy or local depending on if we have a parent
        var hostScale = _hasParent ? Object.Host.transform.localScale : Object.Host.transform.lossyScale;
        var hostScaleX = hostScale.x;
        var hostScaleY = hostScale.y;

        // Check whether the sign of the scale is equal to that of the received update
        // Otherwise, construct the new scale by using the host scale and correct setting the sign
        if (currentScaleX > 0 != scaleXPos) {
            currentScaleX = System.Math.Abs(hostScaleX);
            if (!scaleXPos) {
                currentScaleX *= -1;
            }
        }

        if (currentScaleY > 0 != scaleYPos) {
            currentScaleY = System.Math.Abs(hostScaleY);
            if (!scaleYPos) {
                currentScaleY *= -1;
            }
        }
        
        transform.localScale = new Vector3(
            currentScaleX,
            currentScaleY,
            hostScale.z
        );
    }

    /// <summary>
    /// Updates the animation of the client entity.
    /// </summary>
    /// <param name="animationId">The ID of the animation.</param>
    /// <param name="wrapMode">The wrap mode of the animation clip.</param>
    /// <param name="alreadyInSceneUpdate">Whether this update is when entering a new scene.</param>
    public void UpdateAnimation(
        byte animationId, 
        tk2dSpriteAnimationClip.WrapMode wrapMode, 
        bool alreadyInSceneUpdate
    ) {
        if (_animator.Client == null) {
            Logger.Warn($"Entity '{Object.Client.name}' received animation while client animator does not exist");
            return;
        }

        if (!_animationClipNameIds.TryGetValue(animationId, out var clipName)) {
            Logger.Warn($"Entity '{Object.Client.name}' received unknown animation ID: {animationId}");
            return;
        }

        Logger.Info($"Entity '{Object.Client.name}' received animation: {animationId}, {clipName}, {wrapMode}");

        // All paths lead to calling the Play method of the sprite animator that is hooked, so we allow the call
        // through the hook
        _allowClientAnimation = true;

        if (alreadyInSceneUpdate) {
            // Since this is an animation update from an entity that was already present in a scene,
            // we need to determine where to start playing this specific animation
            LateUpdateAnimation(_animator.Client, clipName, wrapMode);
        }

        // Otherwise, default to just playing the clip
        _animator.Client.Play(clipName);
    }

    /// <summary>
    /// Update the animation for the given animator with the given clip name and wrap mode. This assumes that we need
    /// to replicate animation behaviour for a late update.
    /// </summary>
    /// <param name="animator">The sprite animator to update.</param>
    /// <param name="clipName">The name of the animation clip.</param>
    /// <param name="wrapMode">The wrap mode for the animation.</param>
    private void LateUpdateAnimation(
        tk2dSpriteAnimator animator, 
        string clipName, 
        tk2dSpriteAnimationClip.WrapMode wrapMode
    ) {
        if (wrapMode == tk2dSpriteAnimationClip.WrapMode.Loop) {
            animator.Play(clipName);
            return;
        }

        var clip = animator.GetClipByName(clipName);
        
        Logger.Debug($"Entity ({Id}, {Type}) LateUpdateAnimation: {clip.name}, {wrapMode}");

        if (wrapMode == tk2dSpriteAnimationClip.WrapMode.LoopSection) {
            // The clip loops in a specific section in the frames, so we start playing
            // it from the start of that section
            animator.PlayFromFrame(clipName, clip.loopStart);
            return;
        }

        if (wrapMode == tk2dSpriteAnimationClip.WrapMode.Once ||
            wrapMode == tk2dSpriteAnimationClip.WrapMode.Single) {
            // Since the clip was played once, it stops on the last frame,
            // so we emulate that by only "playing" the last frame of the clip
            var clipLength = clip.frames.Length;
            animator.PlayFromFrame(clipName, clipLength - 1);

            // Logger.Info(
            // $"  Played animation: {clipName}, {clipLength - 1} on {_animator.Client.name}, {_animator.Client.GetHashCode()}");
        }
    }

    /// <summary>
    /// Updates whether the game object for the client entity is active.
    /// </summary>
    /// <param name="active">The new value for active.</param>
    public void UpdateIsActive(bool active) {
        // Logger.Info($"Entity '{Object.Client.name}' received active: {active}");
        if (Object.Client != null) {
            Object.Client.SetActive(active);
        } else {
            Logger.Warn($"Entity ({Id}, {Type}) could not update active, because client object is null");
        }
    }

    /// <summary>
    /// Updates generic data for the client entity.
    /// </summary>
    /// <param name="entityNetworkData">A list of data to update the client entity with.</param>
    public void UpdateData(List<EntityNetworkData> entityNetworkData) {
        foreach (var data in entityNetworkData) {
            if (data.Type == EntityComponentType.Fsm) {
                PlayMakerFSM fsm;
                byte stateIndex;
                byte actionIndex;

                if (_fsms.Client.Count > 1) {
                    // Do a check on the length of the data
                    if (data.Packet.Length < 3) {
                        continue;
                    }

                    var fsmIndex = data.Packet.ReadByte();
                    fsm = _fsms.Client[fsmIndex];

                    stateIndex = data.Packet.ReadByte();
                    actionIndex = data.Packet.ReadByte();
                } else {
                    // Do a check on the length of the data
                    if (data.Packet.Length < 2) {
                        continue;
                    }

                    fsm = _fsms.Client[0];

                    stateIndex = data.Packet.ReadByte();
                    actionIndex = data.Packet.ReadByte();
                }

                var state = fsm.FsmStates[stateIndex];
                var action = state.Actions[actionIndex];
                
                // Logger.Info($"Received entity network data for FSM: {fsm.Fsm.Name}, {state.Name}, {actionIndex} ({action.GetType()})");

                EntityFsmActions.ApplyNetworkDataFromAction(data, action);

                continue;
            }

            if (_components.TryGetValue(data.Type, out var component)) {
                component.Update(data);
            }
        }
    }

    /// <summary>
    /// Update the FSMs of the host entity to prepare for host transfer or disconnects.
    /// </summary>
    /// <param name="hostFsmData">Dictionary mapping FSM index to data.</param>
    public void UpdateHostFsmData(Dictionary<byte, EntityHostFsmData> hostFsmData) {
        foreach (var fsmPair in hostFsmData) {
            var fsmIndex = fsmPair.Key;
            var data = fsmPair.Value;

            if (_fsms.Host.Count <= fsmIndex) {
                Logger.Warn($"Tried to update host FSM data for unknown FSM index: {fsmIndex}");
                continue;
            }

            var fsm = _fsms.Host[fsmIndex];
            var snapshot = _fsmSnapshots[fsmIndex];

            if (data.Types.Contains(EntityHostFsmData.Type.State)) {
                var states = fsm.FsmStates;
                if (states.Length <= data.CurrentState) {
                    Logger.Warn($"Tried to update host FSM state for unknown state index: {data.CurrentState}");
                } else {
                    snapshot.CurrentState = states[data.CurrentState].Name;
                }
            }

            void CondUpdateVars<FsmType, BaseType>(
                EntityHostFsmData.Type type,
                Dictionary<byte, BaseType> dataDict,
                FsmType[] fsmVarArray,
                Action<FsmType, object> setValueAction
            ) {
                if (data.Types.Contains(type)) {
                    foreach (var pair in dataDict) {
                        if (fsmVarArray.Length <= pair.Key) {
                            Logger.Warn($"Tried to update host FSM var ({typeof(BaseType)}) for unknown index: {pair.Key}");
                        } else {
                            setValueAction.Invoke(fsmVarArray[pair.Key], pair.Value);
                        }
                    }
                }
            }
            
            CondUpdateVars(
                EntityHostFsmData.Type.Floats,
                data.Floats, 
                fsm.FsmVariables.FloatVariables,
                (fsmVar, value) => {
                    fsmVar.Value = (float) value;
                    snapshot.Floats[fsmVar.Name] = (float) value;
                });
            CondUpdateVars(
                EntityHostFsmData.Type.Ints,
                data.Ints, 
                fsm.FsmVariables.IntVariables,
                (fsmVar, value) => {
                    fsmVar.Value = (int) value;
                    snapshot.Ints[fsmVar.Name] = (int) value;
                });
            CondUpdateVars(
                EntityHostFsmData.Type.Bools,
                data.Bools, 
                fsm.FsmVariables.BoolVariables,
                (fsmVar, value) => {
                    fsmVar.Value = (bool) value;
                    snapshot.Bools[fsmVar.Name] = (bool) value;
                });
            CondUpdateVars(
                EntityHostFsmData.Type.Strings,
                data.Strings, 
                fsm.FsmVariables.StringVariables,
                (fsmVar, value) => {
                    fsmVar.Value = (string) value;
                    snapshot.Strings[fsmVar.Name] = (string) value;
                });
            CondUpdateVars(
                EntityHostFsmData.Type.Vector2s,
                data.Vec2s, 
                fsm.FsmVariables.Vector2Variables,
                (fsmVar, value) => {
                    fsmVar.Value = (UnityEngine.Vector2) (Vector2) value;
                    snapshot.Vector2s[fsmVar.Name] = (UnityEngine.Vector2) (Vector2) value;
                });
            CondUpdateVars(
                EntityHostFsmData.Type.Vector3s,
                data.Vec3s, 
                fsm.FsmVariables.Vector3Variables,
                (fsmVar, value) => {
                    fsmVar.Value = (Vector3) (Hkmp.Math.Vector3) value;
                    snapshot.Vector3s[fsmVar.Name] = (Vector3) (Hkmp.Math.Vector3) value;
                });
        }
    }

    /// <summary>
    /// Destroys the entity.
    /// </summary>
    public void Destroy() {
        MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdate;
        On.tk2dSpriteAnimator.Play_tk2dSpriteAnimationClip_float_float -= OnAnimationPlayed;

        foreach (var component in _components.Values.Distinct()) {
            component.Destroy();
        }
    }

    /// <summary>
    /// Get the list of client FSMs.
    /// </summary>
    /// <returns>A list containing the client FSM instances.</returns>
    public List<PlayMakerFSM> GetClientFsms() {
        return _fsms.Client;
    }
}
