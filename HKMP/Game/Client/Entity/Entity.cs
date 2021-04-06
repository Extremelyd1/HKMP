using System;
using System.Collections.Generic;
using HKMP.Fsm;
using HKMP.Networking.Client;
using HKMP.Util;
using HutongGames.PlayMaker;
using ModCommon.Util;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HKMP.Game.Client.Entity {
    public abstract class Entity : IEntity {
        private readonly NetClient _netClient;
        private readonly EntityType _entityType;
        private readonly byte _entityId;

        private Queue<StateVariableUpdate> _stateVariableUpdates;
        private bool _inUpdateState;
        
        protected readonly GameObject GameObject;

        public bool IsControlled { get; private set; }
        public bool AllowEventSending { get; set; }

        // Dictionary containing per state name an array of transitions that the state normally has
        // This is used to revert nulling out the transitions to prevent it from continuing
        private readonly Dictionary<string, FsmTransition[]> _stateTransitions;

        protected PlayMakerFSM Fsm;

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

            // Add a position interpolation component to the enemy so we can smooth out position updates
            GameObject.AddComponent<PositionInterpolation>();

            // Register an update event to send position updates
            MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdate;
        }

        private void OnUpdate() {
            // We don't send updates when this FSM is controlled or when we are not allowed to send events yet
            if (IsControlled || !AllowEventSending) {
                return;
            }
            
            _netClient.UpdateManager.UpdateEntityPosition(_entityType, _entityId, GameObject.transform.position);
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

        public void UpdatePosition(Vector2 position) {
            GameObject.GetComponent<PositionInterpolation>().SetNewPosition(position);
        }

        public void UpdateState(byte state, List<byte> variables) {
            if (!_inUpdateState) {
                Logger.Info(this, "Queue is empty, starting new update");
                
                _inUpdateState = true;
                
                // If we are not currently updating the state, we can queue it immediately
                StartQueuedUpdate(state, variables);
                return;
            }
            
            Logger.Info(this, "Queue is non-empty, queueing new update");
            
            // There is already an update running, so we queue this one
            _stateVariableUpdates.Enqueue(new StateVariableUpdate {
                State = state,
                Variables = variables
            });
        }

        protected void StateUpdateDone() {
            // If the queue is empty when we are done, we reset the boolean
            // so that a new state update can be started immediately
            if (_stateVariableUpdates.Count == 0) {
                Logger.Info(this, "Queue is empty");
                _inUpdateState = false;
                return;
            }
            
            Logger.Info(this, "Queue is non-empty, starting next");

            // Get the next queued update and start it
            var stateVariableUpdate = _stateVariableUpdates.Dequeue();
            StartQueuedUpdate(stateVariableUpdate.State, stateVariableUpdate.Variables);
        }

        protected abstract void StartQueuedUpdate(byte state, List<byte> variable);

        public void Destroy() {
            AllowEventSending = false;

            MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdate;
        }

        protected void SendStateUpdate(byte state) {
            _netClient.UpdateManager.UpdateEntityState(_entityType, _entityId, state);
        }
        
        protected void SendStateUpdate(byte state, List<byte> variables) {
            _netClient.UpdateManager.UpdateEntityStateAndVariables(_entityType, _entityId, state, variables);
        }

        protected void RemoveOutgoingTransitions(string stateName) {
            _stateTransitions[stateName] = Fsm.GetState(stateName).Transitions;
            
            Fsm.GetState(stateName).Transitions = new FsmTransition[0];
        }

        protected Action CreateStateUpdateMethod(Action action) {
            return () => {
                if (IsControlled || !AllowEventSending) {
                    return;
                }

                action.Invoke();
            };
        }

        protected void RestoreAllOutgoingTransitions() {
            foreach (var stateTransitionPair in _stateTransitions) {
                Fsm.GetState(stateTransitionPair.Key).Transitions = stateTransitionPair.Value;
            }
            
            _stateTransitions.Clear();
        }

        protected void RestoreOutgoingTransitions(string stateName) {
            if (!_stateTransitions.TryGetValue(stateName, out var transitions)) {
                Logger.Warn(this,
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