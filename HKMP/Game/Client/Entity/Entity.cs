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
    /// The ID of the entity.
    /// </summary>
    private readonly byte _entityId;

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
    private readonly Dictionary<EntityNetworkData.DataType, EntityComponent> _components;

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
    private List<FsmSnapshot> _fsmSnapshots;

    public Entity(
        NetClient netClient,
        byte entityId,
        EntityType type,
        GameObject hostObject
    ) {
        _netClient = netClient;
        _entityId = entityId;

        Type = type;

        _isControlled = true;

        Object = new HostClientPair<GameObject> {
            Host = hostObject,
            Client = UnityEngine.Object.Instantiate(
                hostObject,
                hostObject.transform.position,
                hostObject.transform.rotation
            )
        };
        Object.Client.SetActive(false);

        // Store whether the host object was active and set it not active until we know if we are scene host
        _originalIsActive = Object.Host.activeSelf;
        Object.Host.SetActive(false);

        _lastIsActive = Object.Host.activeInHierarchy;

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
                Logger.Debug($"Client object of entity: {_entityId}, {type} tried to be recycled");
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

        _components = new Dictionary<EntityNetworkData.DataType, EntityComponent>();
        FindComponents();
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

                if (!EntityFsmActions.SupportedActionTypes.Contains(action.GetType())) {
                    continue;
                }

                _hookedActions[action] = new HookedEntityAction {
                    Action = action,
                    FsmIndex = _fsms.Host.IndexOf(fsm),
                    StateIndex = i,
                    ActionIndex = j
                };
                Logger.Info($"Created hooked action: {action.GetType()}, {_fsms.Host.IndexOf(fsm)}, {state.Name}, {j}");

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
        fsm.enabled = false;
    }

    /// <summary>
    /// Check the host and client objects for components that are supported for networking.
    /// </summary>
    private void FindComponents() {
        var hostHealthManager = Object.Host.GetComponent<HealthManager>();
        var clientHealthManager = Object.Client.GetComponent<HealthManager>();
        if (hostHealthManager != null && clientHealthManager != null) {
            var healthManager = new HostClientPair<HealthManager> {
                Host = hostHealthManager,
                Client = clientHealthManager
            };

            _components[EntityNetworkData.DataType.HealthManager] = new HealthManagerComponent(
                _netClient,
                _entityId,
                Object,
                healthManager
            );
        }

        var climber = Object.Client.GetComponent<Climber>();
        if (climber != null) {
            _components[EntityNetworkData.DataType.Rotation] = new RotationComponent(
                _netClient,
                _entityId,
                Object,
                climber
            );
        }

        var hostCollider = Object.Host.GetComponent<BoxCollider2D>();
        var clientCollider = Object.Client.GetComponent<BoxCollider2D>();
        if (hostCollider != null && clientCollider != null) {
            Logger.Info($"Adding collider component to entity: {Object.Host.name}");

            var collider = new HostClientPair<BoxCollider2D> {
                Host = hostCollider,
                Client = clientCollider
            };

            _components[EntityNetworkData.DataType.Collider] = new ColliderComponent(
                _netClient,
                _entityId,
                Object,
                collider
            );
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
            $"Hooked action was entered: {hookedEntityAction.FsmIndex}, {hookedEntityAction.StateIndex}, {hookedEntityAction.ActionIndex}");

        var networkData = new EntityNetworkData {
            Type = EntityNetworkData.DataType.Fsm
        };

        if (_fsms.Host.Count > 1) {
            networkData.Packet.Write((byte)hookedEntityAction.FsmIndex);
        }

        networkData.Packet.Write((byte)hookedEntityAction.StateIndex);
        networkData.Packet.Write((byte)hookedEntityAction.ActionIndex);

        // Only if the GetNetworkDataFromAction method returns true do we add the entity data
        // for sending
        if (EntityFsmActions.GetNetworkDataFromAction(networkData, self)) {
            _netClient.UpdateManager.AddEntityData(_entityId, networkData);
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
                    _entityId,
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

        var newPosition = transform.position;
        if (newPosition != _lastPosition) {
            _lastPosition = newPosition;

            _netClient.UpdateManager.UpdateEntityPosition(
                _entityId,
                new Vector2(newPosition.x, newPosition.y)
            );
        }

        var newScale = transform.localScale;
        if (newScale != _lastScale) {
            _lastScale = newScale;

            _netClient.UpdateManager.UpdateEntityScale(
                _entityId,
                newScale.x > 0
            );
        }

        var newActive = Object.Host.activeInHierarchy;
        if (newActive != _lastIsActive) {
            _lastIsActive = newActive;

            Logger.Info($"Entity '{Object.Host.name}' changed active: {newActive}");

            _netClient.UpdateManager.UpdateEntityIsActive(
                _entityId,
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
                _netClient.UpdateManager.AddEntityHostFsmData(_entityId, fsmIndex, data);
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
            _entityId,
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
        _lastIsActive = Object.Host.activeInHierarchy;

        Logger.Info(
            $"Initializing entity '{Object.Host.name}' with active: {_originalIsActive}, sending active: {_lastIsActive}");

        _netClient.UpdateManager.UpdateEntityIsActive(_entityId, _lastIsActive);

        _isControlled = false;

        foreach (var component in _components.Values) {
            component.IsControlled = false;
        }
    }

    /// <summary>
    /// Makes the entity a host entity if the client user became the scene host.
    /// </summary>
    public void MakeHost() {
        // TODO: disable client object/FSMs, enable host object/FSMs, set current state from snapshots in all FSMs
        // TODO: copy position, scale, animation, etc. from client to host (perhaps before disabling/enabling client/host)
        
        InitializeHost();
    }

    /// <summary>
    /// Updates the position of the client entity.
    /// </summary>
    /// <param name="position">The new position.</param>
    public void UpdatePosition(Vector2 position) {
        var unityPos = new Vector3(position.X, position.Y);

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
    public void UpdateScale(bool scale) {
        var transform = Object.Client.transform;
        var localScale = transform.localScale;
        var currentScaleX = localScale.x;

        if (currentScaleX > 0 != scale) {
            transform.localScale = new Vector3(
                currentScaleX * -1,
                localScale.y,
                localScale.z
            );
        }
    }

    /// <summary>
    /// Updates the animation of the client entity.
    /// </summary>
    /// <param name="animationId">The ID of the animation.</param>
    /// <param name="wrapMode">The wrap mode of the animation clip.</param>
    /// <param name="alreadyInSceneUpdate">Whether this update is when entering a new scene.</param>
    public void UpdateAnimation(byte animationId, tk2dSpriteAnimationClip.WrapMode wrapMode,
        bool alreadyInSceneUpdate) {
        if (_animator.Client == null) {
            Logger.Warn($"Entity '{Object.Client.name}' received animation while client animator does not exist");
            return;
        }

        if (!_animationClipNameIds.TryGetValue(animationId, out var clipName)) {
            Logger.Warn($"Entity '{Object.Client.name}' received unknown animation ID: {animationId}");
            return;
        }

        // Logger.Info($"Entity '{_object.Client.name}' received animation: {animationId}, {clipName}, {wrapMode}");

        // All paths lead to calling the Play method of the sprite animator that is hooked, so we allow the call
        // through the hook
        _allowClientAnimation = true;

        if (alreadyInSceneUpdate) {
            // Since this is an animation update from an entity that was already present in a scene,
            // we need to determine where to start playing this specific animation
            if (wrapMode == tk2dSpriteAnimationClip.WrapMode.Loop) {
                _animator.Client.Play(clipName);
                return;
            }

            var clip = _animator.Client.GetClipByName(clipName);

            if (wrapMode == tk2dSpriteAnimationClip.WrapMode.LoopSection) {
                // The clip loops in a specific section in the frames, so we start playing
                // it from the start of that section
                _animator.Client.PlayFromFrame(clipName, clip.loopStart);
                return;
            }

            if (wrapMode == tk2dSpriteAnimationClip.WrapMode.Once ||
                wrapMode == tk2dSpriteAnimationClip.WrapMode.Single) {
                // Since the clip was played once, it stops on the last frame,
                // so we emulate that by only "playing" the last frame of the clip
                var clipLength = clip.frames.Length;
                _animator.Client.PlayFromFrame(clipName, clipLength - 1);

                // Logger.Info(
                    // $"  Played animation: {clipName}, {clipLength - 1} on {_animator.Client.name}, {_animator.Client.GetHashCode()}");
                return;
            }
        }

        // Otherwise, default to just playing the clip
        _animator.Client.Play(clipName);
    }

    /// <summary>
    /// Updates whether the game object for the client entity is active.
    /// </summary>
    /// <param name="active">The new value for active.</param>
    public void UpdateIsActive(bool active) {
        Logger.Info($"Entity '{Object.Client.name}' received active: {active}");
        Object.Client.SetActive(active);
    }

    /// <summary>
    /// Updates generic data for the client entity.
    /// </summary>
    /// <param name="entityNetworkData">A list of data to update the client entity with.</param>
    public void UpdateData(List<EntityNetworkData> entityNetworkData) {
        foreach (var data in entityNetworkData) {
            if (data.Type == EntityNetworkData.DataType.Fsm) {
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
                
                Logger.Info($"Received entity network data for FSM: {fsm.Fsm.Name}, {state.Name}, {actionIndex} ({action.GetType()})");

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

            void CondUpdateVars<FsmType, BaseType, UnityType>(
                EntityHostFsmData.Type type,
                Dictionary<byte, BaseType> dataDict,
                FsmType[] fsmVarArray,
                Action<FsmType, UnityType> setValueAction
            ) {
                if (data.Types.Contains(type)) {
                    foreach (var pair in dataDict) {
                        if (fsmVarArray.Length <= pair.Key) {
                            Logger.Warn($"Tried to update host FSM var ({typeof(BaseType)}) for unknown index: {pair.Key}");
                        } else {
                            setValueAction.Invoke(fsmVarArray[pair.Key], (UnityType) (object) pair.Value);
                        }
                    }
                }
            }
            
            CondUpdateVars<FsmFloat, float, float>(
                EntityHostFsmData.Type.Floats,
                data.Floats, 
                fsm.FsmVariables.FloatVariables,
                (fsmVar, value) => {
                    fsmVar.Value = value;
                    snapshot.Floats[fsmVar.Name] = value;
                });
            CondUpdateVars<FsmInt, int, int>(
                EntityHostFsmData.Type.Ints,
                data.Ints, 
                fsm.FsmVariables.IntVariables,
                (fsmVar, value) => {
                    fsmVar.Value = value;
                    snapshot.Ints[fsmVar.Name] = value;
                });
            CondUpdateVars<FsmBool, bool, bool>(
                EntityHostFsmData.Type.Bools,
                data.Bools, 
                fsm.FsmVariables.BoolVariables,
                (fsmVar, value) => {
                    fsmVar.Value = value;
                    snapshot.Bools[fsmVar.Name] = value;
                });
            CondUpdateVars<FsmString, string, string>(
                EntityHostFsmData.Type.Strings,
                data.Strings, 
                fsm.FsmVariables.StringVariables,
                (fsmVar, value) => {
                    fsmVar.Value = value;
                    snapshot.Strings[fsmVar.Name] = value;
                });
            CondUpdateVars<FsmVector2, Vector2, UnityEngine.Vector2>(
                EntityHostFsmData.Type.Vector2s,
                data.Vec2s, 
                fsm.FsmVariables.Vector2Variables,
                (fsmVar, value) => {
                    fsmVar.Value = value;
                    snapshot.Vector2s[fsmVar.Name] = value;
                });
            CondUpdateVars<FsmVector3, Math.Vector3, Vector3>(
                EntityHostFsmData.Type.Vector3s,
                data.Vec3s, 
                fsm.FsmVariables.Vector3Variables,
                (fsmVar, value) => {
                    fsmVar.Value = value;
                    snapshot.Vector3s[fsmVar.Name] = value;
                });
        }
    }

    /// <summary>
    /// Destroys the entity.
    /// </summary>
    public void Destroy() {
        MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdate;
        On.tk2dSpriteAnimator.Play_tk2dSpriteAnimationClip_float_float -= OnAnimationPlayed;

        foreach (var component in _components.Values) {
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
