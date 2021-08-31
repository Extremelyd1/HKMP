using System;
using System.Collections.Generic;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using HutongGames.PlayMaker;
using UnityEngine;
using Vector2 = Hkmp.Math.Vector2;

namespace Hkmp.Game.Client.Entity {
    /**
     * Abstract class that implements the entity interface. This class provides some base functionality to
     * classes extending it that is commonly used for controlling and updating its behaviour.
     */
    public abstract class Entity : IEntity {
        private readonly NetClient _netClient;
        private readonly EntityType _entityType;
        private readonly byte _entityId;

        // Whether the local player is the scene host
        protected bool IsHostEntity;

        // Dictionary storing transitions by FSM
        private readonly Dictionary<PlayMakerFSM, TransitionStore> _fsmTransitionStores;

        // Whether this entity has been initialized to prevent double initialization 
        private bool _isInitialized;

        // The game object corresponding to this entity
        protected readonly GameObject GameObject;

        private Vector3 _lastPosition;
        private bool _lastScale;

        protected Entity(
            NetClient netClient,
            EntityType entityType,
            byte entityId,
            GameObject gameObject
        ) {
            _netClient = netClient;
            _entityType = entityType;
            _entityId = entityId;

            _fsmTransitionStores = new Dictionary<PlayMakerFSM, TransitionStore>();
            
            GameObject = gameObject;

            // Add a position interpolation component to the enemy so we can smooth out position updates
            if (GameObject != null) {
                GameObject.AddComponent<PositionInterpolation>();
            }
        }

        public void InitializeAsSceneHost() {
            if (_isInitialized) {
                Logger.Get().Info(this, "Entity is already initialized");
                return;
            }
            
            Logger.Get().Info(this, "Initializing entity as scene host");
            
            IsHostEntity = true;
            _isInitialized = true;
            
            // Register an update event to send position updates
            MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdate;
            
            InternalInitializeAsSceneHost();
        }

        /**
         * Overridable method for initializing the entity given that the local player is scene host
         */
        protected abstract void InternalInitializeAsSceneHost();

        public void InitializeAsSceneClient(byte? stateIndex) {
            if (_isInitialized) {
                Logger.Get().Info(this, "Entity is already initialized");
                return;
            }
            
            Logger.Get().Info(this, $"Initializing entity as scene client, with{(stateIndex.HasValue ? " state: " + stateIndex.Value : "out state")}");

            _isInitialized = true;

            InternalInitializeAsSceneClient(stateIndex);
        }

        /**
         * Overridable method for initializing the entity given that the local player is scene client
         */
        protected abstract void InternalInitializeAsSceneClient(byte? stateIndex);

        public void SwitchToSceneHost() {
            Logger.Get().Info(this, "Switching this entity as scene host");
            
            IsHostEntity = true;
            
            // Register an update event to send position updates
            MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdate;
            
            InternalSwitchToSceneHost();
        }

        /**
         * Overridable method for initializing the entity given that the local player has turned scene host
         */
        protected abstract void InternalSwitchToSceneHost();
        
        public void UpdatePosition(Vector2 position) {
            if (GameObject == null) {
                return;
            }
            
            var unityPos = new Vector3(
                position.X, 
                position.Y,
                GameObject.transform.position.z
            );

            GameObject.GetComponent<PositionInterpolation>().SetNewPosition(unityPos);
        }
        
        public void UpdateScale(bool scale) {
            if (GameObject == null) {
                return;
            }
            
            var transform = GameObject.transform;
            var localScale = transform.localScale;
            var currentScaleX = localScale.x;

            if (currentScaleX > 0 != scale) {
                GameObject.transform.localScale = new Vector3(
                    currentScaleX * -1,
                    localScale.y,
                    localScale.z
                );
            } 
        }
        
        public abstract void UpdateAnimation(byte animationIndex, byte[] animationInfo);

        public abstract void UpdateState(byte state);
        
        public virtual void Destroy() {
            MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdate;
        }

        private void OnUpdate() {
            if (GameObject == null) {
                return;
            }

            // Update the position and/or scale if they change
            var transform = GameObject.transform;
            
            var transformPos = transform.position;
            if (transformPos != _lastPosition) {
                _netClient.UpdateManager.UpdateEntityPosition(
                    _entityType, 
                    _entityId, 
                    new Vector2(transformPos.x, transformPos.y)
                );

                _lastPosition = transformPos;
            }
            
            var scale = transform.localScale.x > 0;
            if (scale != _lastScale) {
                _netClient.UpdateManager.UpdateEntityScale(
                    _entityType,
                    _entityId,
                    scale
                );

                _lastScale = scale;
            }
        }

        /**
         * Sends an animation update with the given animation index and no additional animation info
         */
        protected void SendAnimationUpdate(byte animationIndex) {
            SendAnimationUpdate(animationIndex, new byte[0]);
        }
        
        /**
         * Sends an animation update with the given animation index and a single byte as additional animation info
         */
        protected void SendAnimationUpdate(byte animationIndex, byte animationInfo) {
            SendAnimationUpdate(animationIndex, new [] { animationInfo });
        }
        
        /**
         * Sends an animation update with the given animation index and a list of bytes as additional animation info
         */
        protected void SendAnimationUpdate(byte animationIndex, List<byte> animationInfo) {
            SendAnimationUpdate(animationIndex, animationInfo.ToArray());
        }
        
        /**
         * Sends an animation update with the given animation index and an array of bytes as additional animation info
         */
        protected void SendAnimationUpdate(byte animationIndex, byte[] animationInfo) {
            _netClient.UpdateManager.UpdateEntityAnimation(
                _entityType,
                _entityId,
                animationIndex,
                animationInfo
            );
        }

        /**
         * Sends a state update with the given state index
         */
        protected void SendStateUpdate(byte stateIndex) {
            _netClient.UpdateManager.UpdateEntityState(
                _entityType,
                _entityId,
                stateIndex
            );
        }

        /**
         * Removes all transitions from the given FSM and stores them for later restoration
         */
        protected void RemoveAllTransitions(PlayMakerFSM fsm) {
            if (!_fsmTransitionStores.TryGetValue(fsm, out var transitionStore)) {
                transitionStore = new TransitionStore();
            }
            
            foreach (var state in fsm.FsmStates) {
                // Store the transitions of this state
                transitionStore.StateTransitions[state.Name] = state.Transitions;

                // And then replace the array by an empty one
                state.Transitions = new FsmTransition[0];
            }

            // Also store the global transitions and remove then from the FSM
            transitionStore.GlobalTransitions = fsm.FsmGlobalTransitions;
            fsm.Fsm.GlobalTransitions = new FsmTransition[0];

            _fsmTransitionStores[fsm] = transitionStore;
        }

        /**
         * Restores all transitions for the given FSM given that they have been removed and stored
         */
        protected void RestoreAllTransitions(PlayMakerFSM fsm) {
            if (!_fsmTransitionStores.TryGetValue(fsm, out var transitionStore)) {
                return;
            }

            var stateTransitions = transitionStore.StateTransitions;
            
            foreach (var stateNameTransitionPair in stateTransitions) {
                var stateName = stateNameTransitionPair.Key;
                var transitions = stateNameTransitionPair.Value;

                // Get the state by name and restore the transitions to the saved ones
                fsm.GetState(stateName).Transitions = transitions;
            }

            // Also reset the global transitions
            fsm.Fsm.GlobalTransitions = transitionStore.GlobalTransitions;
            
            // Remove the entry for this FSM
            _fsmTransitionStores.Remove(fsm);
        }
        
        /**
         * Create a state update method with the given action as body. This method is used
         * to wrap a given action in checks that ensure that we are allowed to
         * send state updates
         */
        protected Action CreateUpdateMethod(Action action) {
            return () => {
                if (!IsHostEntity) {
                    return;
                }

                action.Invoke();
            };
        }
    }
}