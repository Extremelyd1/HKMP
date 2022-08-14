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
using Object = UnityEngine.Object;
using Vector2 = Hkmp.Math.Vector2;

namespace Hkmp.Game.Client.Entity {
    internal class Entity {
        private readonly NetClient _netClient;
        private readonly byte _entityId;

        private readonly GameObject _hostObject;
        private readonly GameObject _clientObject;

        private readonly tk2dSpriteAnimator _hostAnimator;
        private readonly tk2dSpriteAnimator _clientAnimator;
        
        private readonly BiLookup<string, byte> _animationClipNameIds;

        private readonly List<PlayMakerFSM> _hostFsms;
        private readonly List<PlayMakerFSM> _clientFsms;

        private readonly Dictionary<EntityNetworkData.DataType, EntityComponent> _components;

        private readonly Dictionary<FsmStateAction, HookedEntityAction> _hookedActions;

        private readonly bool _originalIsActive;

        private bool _isControlled;
        
        private Vector3 _lastPosition;
        private Vector3 _lastScale;
        private bool _lastIsActive;

        public Entity(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) {
            _netClient = netClient;
            _entityId = entityId;
            _hostObject = gameObject;

            _isControlled = true;

            _clientObject = Object.Instantiate(
                _hostObject,
                _hostObject.transform.position,
                _hostObject.transform.rotation
            );
            _clientObject.SetActive(false);

            // Store whether the host object was active and set it not active until we know if we are scene host
            _originalIsActive = _hostObject.activeSelf;
            _hostObject.SetActive(false);

            _lastIsActive = _hostObject.activeInHierarchy;
            
            Logger.Get().Info(this, $"Entity '{_hostObject.name}' was original active: {_originalIsActive}, last active: {_lastIsActive}");

            // Add a position interpolation component to the enemy so we can smooth out position updates
            _clientObject.AddComponent<PositionInterpolation>();

            // Register an update event to send position updates
            MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdate;

            _hostAnimator = _hostObject.GetComponent<tk2dSpriteAnimator>();
            _clientAnimator = _clientObject.GetComponent<tk2dSpriteAnimator>();
            if (_hostAnimator != null) {
                _animationClipNameIds = new BiLookup<string, byte>();

                var index = 0;
                foreach (var animationClip in _hostAnimator.Library.clips) {
                    _animationClipNameIds.Add(animationClip.name, (byte)index++);

                    if (index > byte.MaxValue) {
                        Logger.Get().Error(this,
                            $"Too many animation clips to fit in a byte for entity: {_clientObject.name}");
                        break;
                    }
                }

                On.tk2dSpriteAnimator.Play_tk2dSpriteAnimationClip_float_float += OnAnimationPlayed;
            }

            _hostFsms = _hostObject.GetComponents<PlayMakerFSM>().ToList();
            
            _hookedActions = new Dictionary<FsmStateAction, HookedEntityAction>();
            foreach (var fsm in _hostFsms) {
                ProcessHostFsm(fsm);
            }

            // Remove all components that (re-)activate FSMs
            foreach (var fsmActivator in _clientObject.GetComponents<FSMActivator>()) {
                fsmActivator.StopAllCoroutines();
                Object.Destroy(fsmActivator);
            }

            _clientFsms = _clientObject.GetComponents<PlayMakerFSM>().ToList();
            foreach (var fsm in _clientFsms) {
                ProcessClientFsm(fsm);
            }
            
            _components = new Dictionary<EntityNetworkData.DataType, EntityComponent>();
            FindComponents();
        }

        private void ProcessHostFsm(PlayMakerFSM fsm) {
            Logger.Get().Info(this, $"Processing host FSM: {fsm.Fsm.Name}");

            for (var i = 0; i < fsm.FsmStates.Length; i++) {
                var state = fsm.FsmStates[i];

                for (var j = 0; j < state.Actions.Length; j++) {
                    var action = state.Actions[j];

                    if (!EntityFsmActions.SupportedActionTypes.Contains(action.GetType())) {
                        continue;
                    }

                    _hookedActions[action] = new HookedEntityAction {
                        Action = action,
                        FsmIndex = _hostFsms.IndexOf(fsm),
                        StateIndex = i,
                        ActionIndex = j
                    };
                    Logger.Get().Info(this, $"Created hooked action: {action.GetType()}, {_hostFsms.IndexOf(fsm)}, {i}, {j}");

                    FsmActionHooks.RegisterFsmStateActionType(action.GetType(), OnActionEntered);
                }
            }
        }

        private void ProcessClientFsm(PlayMakerFSM fsm) {
            Logger.Get().Info(this, $"Processing client FSM: {fsm.Fsm.Name}");
            fsm.enabled = false;
        }
        
        private void FindComponents() {
            var climber = _clientObject.GetComponent<Climber>();
            if (climber != null) {
                _components[EntityNetworkData.DataType.Rotation] = new RotationComponent(
                    _netClient,
                    _entityId,
                    _hostObject,
                _clientObject,
                    climber
                );
            }
        }
        
        private void OnActionEntered(FsmStateAction self) {
            Logger.Get().Info(this, $"ActionEntered: {self.GetType()}");
            
            if (_isControlled) {
                return;
            }
            
            if (!_hookedActions.TryGetValue(self, out var hookedEntityAction)) {
                return;
            }
            
            Logger.Get().Info(this, $"Hooked action was entered: {hookedEntityAction.FsmIndex}, {hookedEntityAction.StateIndex}, {hookedEntityAction.ActionIndex}");
            
            var networkData = new EntityNetworkData {
                Type = EntityNetworkData.DataType.Fsm
            };
            
            if (_hostFsms.Count > 1) {
                networkData.Data.Add((byte)hookedEntityAction.FsmIndex);
            }
            
            networkData.Data.Add((byte) hookedEntityAction.StateIndex);
            networkData.Data.Add((byte) hookedEntityAction.ActionIndex);
            
            EntityFsmActions.GetNetworkDataFromAction(networkData, self);
            
            _netClient.UpdateManager.AddEntityData(_entityId, networkData);
        }

        private void OnUpdate() {
            if (_hostObject == null) {
                return;
            }
            
            var hostObjectActive = _hostObject.activeSelf;

            if (_isControlled) {
                if (hostObjectActive) {
                    Logger.Get().Info(this, $"Entity '{_hostObject.name}' host object became active, re-disabling");
                    _hostObject.SetActive(false);
                }
                
                return;
            }

            var transform = _hostObject.transform;

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

            var newActive = _hostObject.activeInHierarchy;
            if (newActive != _lastIsActive) {
                _lastIsActive = newActive;
                
                Logger.Get().Info(this, $"Entity '{_hostObject.name}' changed active: {newActive}");
                
                _netClient.UpdateManager.UpdateEntityIsActive(
                    _entityId,
                    newActive
                );
            }
        }

        private void OnAnimationPlayed(
            On.tk2dSpriteAnimator.orig_Play_tk2dSpriteAnimationClip_float_float orig, 
            tk2dSpriteAnimator self, 
            tk2dSpriteAnimationClip clip, 
            float clipStartTime, 
            float overrideFps
        ) {
            orig(self, clip, clipStartTime, overrideFps);

            if (self != _hostAnimator) {
                return;
            }
            
            if (_isControlled) {
                return;
            }

            if (!_animationClipNameIds.TryGetValue(clip.name, out var animationId)) {
                Logger.Get().Warn(this, $"Entity '{_clientObject.name}' played unknown animation: {clip.name}");
                return;
            }

            // Logger.Get().Info(this, $"Entity '{_gameObject.name}' sends animation: {clip.name}, {animationId}, {clip.wrapMode}");
            _netClient.UpdateManager.UpdateEntityAnimation(
                _entityId,
                animationId,
                (byte) clip.wrapMode
            );
        }

        public void InitializeHost() {
            _hostObject.SetActive(_originalIsActive);
            
            // Also update the last active variable to account for this potential change
            // Otherwise we might trigger the update sending of activity twice
            _lastIsActive = _hostObject.activeInHierarchy;

            Logger.Get().Info(this, $"Initializing entity '{_hostObject.name}' with active: {_originalIsActive}, sending active: {_lastIsActive}");

            var currentObject = _hostObject;
            while (currentObject != null) {
                Logger.Get().Info(this, $"  Object: {currentObject}, active: {currentObject.activeSelf}");

                var transform = currentObject.transform;
                if (transform == null) {
                    break;
                }

                var parent = transform.parent;
                if (parent == null) {
                    break;
                }

                currentObject = parent.gameObject;
            }
            
            _netClient.UpdateManager.UpdateEntityIsActive(_entityId, _lastIsActive);
            
            _isControlled = false;

            foreach (var component in _components.Values) {
                component.IsControlled = false;
            }
        }

        // TODO: parameters should be all FSM details to kickstart all FSMs of the game object
        public void MakeHost() {
            // TODO: read all variables from the parameters and set the FSM variables of all FSMs
            
            InitializeHost();
        }

        public void UpdatePosition(Vector2 position) {
            var unityPos = new Vector3(position.X, position.Y);

            if (_clientObject == null) {
                return;
            }

            var positionInterpolation = _clientObject.GetComponent<PositionInterpolation>();
            if (positionInterpolation == null) {
                return;
            }

            positionInterpolation.SetNewPosition(unityPos);
        }

        public void UpdateScale(bool scale) {
            var transform = _clientObject.transform;
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

        public void UpdateAnimation(byte animationId, tk2dSpriteAnimationClip.WrapMode wrapMode, bool alreadyInSceneUpdate) {
            if (_clientAnimator == null) {
                Logger.Get().Warn(this,
                    $"Entity '{_clientObject.name}' received animation while client animator does not exist");
                return;
            }

            if (!_animationClipNameIds.TryGetValue(animationId, out var clipName)) {
                Logger.Get().Warn(this, $"Entity '{_clientObject.name}' received unknown animation ID: {animationId}");
                return;
            }
            
            // Logger.Get().Info(this, $"Entity '{_gameObject.name}' received animation: {animationId}, {clipName}, {wrapMode}");

            if (alreadyInSceneUpdate) {
                // Since this is an animation update from an entity that was already present in a scene,
                // we need to determine where to start playing this specific animation
                if (wrapMode == tk2dSpriteAnimationClip.WrapMode.Loop) {
                    _clientAnimator.Play(clipName);
                    return;
                }
                
                var clip = _clientAnimator.GetClipByName(clipName);

                if (wrapMode == tk2dSpriteAnimationClip.WrapMode.LoopSection) {
                    // The clip loops in a specific section in the frames, so we start playing
                    // it from the start of that section
                    _clientAnimator.PlayFromFrame(clipName, clip.loopStart);
                    return;
                }

                if (wrapMode == tk2dSpriteAnimationClip.WrapMode.Once ||
                    wrapMode == tk2dSpriteAnimationClip.WrapMode.Single) {
                    // Since the clip was played once, it stops on the last frame,
                    // so we emulate that by only "playing" the last frame of the clip
                    var clipLength = clip.frames.Length;
                    _clientAnimator.PlayFromFrame(clipName, clipLength - 1);
                    return;
                }
            }
            
            // Otherwise, default to just playing the clip
            _clientAnimator.Play(clipName);
        }

        public void UpdateIsActive(bool active) {
            Logger.Get().Info(this, $"Entity '{_clientObject.name}' received active: {active}");
            _clientObject.SetActive(active);
        }

        public void UpdateData(List<EntityNetworkData> entityNetworkData) {
            Logger.Get().Info(this, $"UpdateData called for entity: {_entityId}");
            foreach (var data in entityNetworkData) {
                if (data.Type == EntityNetworkData.DataType.Fsm) {
                    PlayMakerFSM fsm;
                    byte stateIndex;
                    byte actionIndex;

                    if (_clientFsms.Count > 1) {
                        // Do a check on the length of the data
                        if (data.Data.Count < 3) {
                            continue;
                        }

                        var fsmIndex = data.Data[0];
                        fsm = _clientFsms[fsmIndex];
                        
                        stateIndex = data.Data[1];
                        actionIndex = data.Data[2];

                        data.Data.RemoveRange(0, 3);
                    } else {
                        // Do a check on the length of the data
                        if (data.Data.Count < 2) {
                            continue;
                        }
                        
                        fsm = _clientFsms[0];

                        stateIndex = data.Data[0];
                        actionIndex = data.Data[1];
                        
                        data.Data.RemoveRange(0, 2);
                    }

                    Logger.Get().Info(this, $"Received entity network data for FSM: {fsm.Fsm.Name}, {stateIndex}, {actionIndex}");

                    var state = fsm.FsmStates[stateIndex];
                    var action = state.Actions[actionIndex];

                    EntityFsmActions.ApplyNetworkDataFromAction(data, action);
                    
                    continue;
                }
                
                if (_components.TryGetValue(data.Type, out var component)) {
                    component.Update(data);
                }
            }
        }

        public void Destroy() {
            MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdate;
            On.tk2dSpriteAnimator.Play_tk2dSpriteAnimationClip_float_float -= OnAnimationPlayed;

            foreach (var component in _components.Values) {
                component.Destroy();
            }
        }
    }
}