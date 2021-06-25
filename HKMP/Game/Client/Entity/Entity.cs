using System;
using System.Collections.Generic;
using System.Linq;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using HutongGames.PlayMaker;
using UnityEngine;
using Vector2 = Hkmp.Math.Vector2;

namespace Hkmp.Game.Client.Entity {
    /**
     * Abstract class that implements the entity interface. This class provides some base functionality to
     * classes extending it that is commonly used for controlling and updating the state and its variables
     */
    public abstract class Entity : IEntity {
        private readonly NetClient _netClient;
        private readonly EntityType _entityType;
        private readonly byte _entityId;

        // A queue containing state and/or variable updates that still need to be processed
        private readonly Queue<StateVariableUpdate> _stateVariableUpdates;
        // Whether this entity is currently in an update state, which will queue new incoming events for
        // later execution
        private bool _inUpdateState;

        // The game object corresponding to this entity
        protected readonly GameObject GameObject;

        public bool IsControlled { get; private set; }
        public bool AllowEventSending { get; set; }

        // Dictionary containing per state name an array of transitions that the state normally has
        // This is used to revert nulling out the transitions to prevent it from continuing
        private readonly Dictionary<string, FsmTransition[]> _stateTransitions;

        // Dictionary containing per state name an array of actions that the state normally has
        // This is used to revert removing/altering actions
        private readonly Dictionary<string, FsmStateAction[]> _stateActions;

        // The main FSM used for controlling the entity
        protected PlayMakerFSM Fsm;

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
            GameObject = gameObject;

            _stateVariableUpdates = new Queue<StateVariableUpdate>();

            _stateTransitions = new Dictionary<string, FsmTransition[]>();
            _stateActions = new Dictionary<string, FsmStateAction[]>();

            // Add a position interpolation component to the enemy so we can smooth out position updates
            GameObject.AddComponent<PositionInterpolation>();

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

        public void UpdatePosition(Math.Vector2 position) {
            if (GameObject == null) {
                return;
            }
            
            var unityPos = new Vector3(position.X, position.Y);

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

        public void UpdateState(byte state, List<byte> variables) {
            if (IsInterruptingState(state)) {
                Logger.Get().Info(this, "Received update is interrupting state, starting update");

                _inUpdateState = true;

                // Since we interrupt everything that was going on, we can clear the existing queue
                _stateVariableUpdates.Clear();

                StartQueuedUpdate(state, variables);

                return;
            }

            if (!_inUpdateState) {
                Logger.Get().Info(this, "Queue is empty, starting new update");

                _inUpdateState = true;

                // If we are not currently updating the state, we can queue it immediately
                StartQueuedUpdate(state, variables);

                return;
            }

            Logger.Get().Info(this, $"Queue is non-empty, queueing new update, current FSM state: {Fsm.ActiveStateName}");

            // There is already an update running, so we queue this one
            _stateVariableUpdates.Enqueue(new StateVariableUpdate {
                State = state,
                Variables = variables
            });
        }

        /**
         * Called when the previous state update is done.
         * Usually called on specific points in the entity's FSM.
         */
        protected void StateUpdateDone() {
            // If the queue is empty when we are done, we reset the boolean
            // so that a new state update can be started immediately
            if (_stateVariableUpdates.Count == 0) {
                Logger.Get().Info(this, "Queue is empty");
                _inUpdateState = false;
                return;
            }

            Logger.Get().Info(this, "Queue is non-empty, starting next");

            // Get the next queued update and start it
            var stateVariableUpdate = _stateVariableUpdates.Dequeue();
            StartQueuedUpdate(stateVariableUpdate.State, stateVariableUpdate.Variables);
        }

        /**
         * Start a (previously queued) update with given state index and variable list.
         */
        protected abstract void StartQueuedUpdate(byte state, List<byte> variables);

        /**
         * Whether the given state index represents a state that should interrupt
         * other updating states.
         */
        protected abstract bool IsInterruptingState(byte state);

        public virtual void Destroy() {
            AllowEventSending = false;

            MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdate;
        }

        /**
         * Send a state update for this entity with the given byte as state index
         */
        protected void SendStateUpdate(byte state) {
            _netClient.UpdateManager.UpdateEntityState(_entityType, _entityId, state);
        }
        
        /**
         * Send a state update for this entity with the given byte as state index and the given list
         * of bytes representing variables
         */
        protected void SendStateUpdate(byte state, List<byte> variables) {
            _netClient.UpdateManager.UpdateEntityStateAndVariables(_entityType, _entityId, state, variables);
        }

        /**
         * Remove all outgoing transitions of the given state. This stores the original array
         * to allow for reverting.
         */
        protected void RemoveOutgoingTransitions(string stateName) {
            _stateTransitions[stateName] = Fsm.GetState(stateName).Transitions;

            foreach (var transition in _stateTransitions[stateName]) {
                Logger.Get().Info(this, $"Removing transition in state: {stateName}, to: {transition.ToState}");
            }

            Fsm.GetState(stateName).Transitions = new FsmTransition[0];
        }

        /**
         * Remove a specific outgoing transition of the given state. This stores the original array
         * to allow for reverting.
         */
        protected void RemoveOutgoingTransition(string stateName, string toState) {
            // Get the current array of transitions
            var originalTransitions = Fsm.GetState(stateName).Transitions;

            // We don't want to overwrite the originally stored transitions,
            // so we only store it if the key doesn't exist yet
            if (!_stateTransitions.TryGetValue(stateName, out _)) {
                _stateTransitions[stateName] = originalTransitions;
            }

            // Try to find the transition that has a destination state with the given name
            var newTransitions = originalTransitions.ToList();
            foreach (var transition in originalTransitions) {
                if (transition.ToState.Equals(toState)) {
                    newTransitions.Remove(transition);
                    break;
                }
            }

            Fsm.GetState(stateName).Transitions = newTransitions.ToArray();
        }

        /**
         * Restore all stored outgoing transitions of states that have been modified.
         */
        protected void RestoreAllOutgoingTransitions() {
            foreach (var stateTransitionPair in _stateTransitions) {
                Fsm.GetState(stateTransitionPair.Key).Transitions = stateTransitionPair.Value;
            }
            
            _stateTransitions.Clear();
        }
        
        private void SaveActions(string stateName) {
            // Get the current array of actions
            var originalActions = Fsm.GetState(stateName).Actions;
            
            // We don't want to overwrite the originally stored actions,
            // so we only store it if the key doesn't exist yet
            if (!_stateActions.TryGetValue(stateName, out _)) {
                _stateActions[stateName] = originalActions;
            }
        }

        /**
         * Remove an action of a given state by index. This stores the original array
         * to allow for reverting.
         */
        protected void RemoveAction(string stateName, int index) {
            SaveActions(stateName);
            
            // Now remove the action by index
            Fsm.RemoveAction(stateName, index);
        }

        /**
         * Remove an action of a given state by type. This stores the original array
         * to allow for reverting.
         */
        protected void RemoveAction(string stateName, Type type) {
            SaveActions(stateName);
            
            // Now remove the action by type
            Fsm.RemoveAction(stateName, type);
        }

        /**
         * Restores all stored original actions of states.
         */
        protected void RestoreAllActions() {
            foreach (var stateActionPair in _stateActions) {
                Fsm.GetState(stateActionPair.Key).Actions = stateActionPair.Value;
            }
            
            _stateActions.Clear();
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

        /**
         * Restores the outgoing transitions of the state with the given name
         */
        protected void RestoreOutgoingTransitions(string stateName) {
            if (!_stateTransitions.TryGetValue(stateName, out var transitions)) {
                Logger.Get().Warn(this,
                    $"Tried to restore transitions for state named: {stateName}, but they are not stored");
                return;
            }

            Fsm.GetState(stateName).Transitions = transitions;
            _stateTransitions.Remove(stateName);
        }

        private class StateVariableUpdate {
            public byte State { get; set; }
            public List<byte> Variables { get; set; }
        }
    }
}