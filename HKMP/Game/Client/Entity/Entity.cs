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

        private readonly Dictionary<PlayMakerFSM, Dictionary<string, FsmTransition[]>> _stateTransitions;
        
        // The game object corresponding to this entity
        protected readonly GameObject GameObject;

        public bool IsControlled { get; private set; }
        public bool AllowEventSending { get; set; }

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

            _stateTransitions = new Dictionary<PlayMakerFSM, Dictionary<string, FsmTransition[]>>();
            
            GameObject = gameObject;

            // Add a position interpolation component to the enemy so we can smooth out position updates
            if (GameObject != null) {
                GameObject.AddComponent<PositionInterpolation>();
            }

            // Register an update event to send position updates
            MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdate;
        }

        private void OnUpdate() {
            // We don't send updates when this FSM is controlled or when we are not allowed to send events yet
            if (IsControlled || !AllowEventSending || GameObject == null) {
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

        public void TakeControl() {
            if (IsControlled) {
                return;
            }

            IsControlled = true;

            InternalTakeControl();
        }

        protected abstract void InternalTakeControl();

        public void ReleaseControl() {
            if (!IsControlled) {
                return;
            }

            IsControlled = false;

            InternalReleaseControl();
        }

        protected abstract void InternalReleaseControl();

        public abstract void SendInitialState();

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

        public abstract void InitializeWithState(byte state);

        public virtual void Destroy() {
            AllowEventSending = false;

            MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdate;
        }

        protected void SendAnimationUpdate(byte animationIndex) {
            SendAnimationUpdate(animationIndex, new List<byte>());
        }
        
        protected void SendAnimationUpdate(byte animationIndex, List<byte> animationInfo) {
            _netClient.UpdateManager.UpdateEntityAnimation(
                _entityType,
                _entityId,
                animationIndex,
                animationInfo.ToArray()
            );
        }

        protected void SendStateUpdate(byte state) {
            _netClient.UpdateManager.UpdateEntityState(
                _entityType,
                _entityId,
                state
            );
        }

        protected void RemoveAllTransitions(PlayMakerFSM fsm) {
            if (!_stateTransitions.TryGetValue(fsm, out var stateTransitions)) {
                stateTransitions = new Dictionary<string, FsmTransition[]>();
            }
            
            foreach (var state in fsm.FsmStates) {
                // Store the transitions of this state
                stateTransitions[state.Name] = state.Transitions;

                // And then replace the array by an empty one
                state.Transitions = new FsmTransition[0];
            }

            _stateTransitions[fsm] = stateTransitions;
        }

        protected void RestoreAllTransitions(PlayMakerFSM fsm) {
            var stateTransitions = _stateTransitions[fsm];
            
            foreach (var stateNameTransitionPair in stateTransitions) {
                var stateName = stateNameTransitionPair.Key;
                var transitions = stateNameTransitionPair.Value;

                // Get the state by name and restore the transitions to the saved ones
                fsm.GetState(stateName).Transitions = transitions;
            }
            
            // Remove the entry for this FSM
            _stateTransitions.Remove(fsm);
        }
        
        /**
         * Create a state update method with the given action as body. This method is used
         * to wrap a given action in checks that ensure that we are current allowed to
         * send state updates
         */
        protected Action CreateStateUpdateMethod(Action action) {
            return () => {
                if (IsControlled || !AllowEventSending) {
                    return;
                }

                action.Invoke();
            };
        }
    }
}