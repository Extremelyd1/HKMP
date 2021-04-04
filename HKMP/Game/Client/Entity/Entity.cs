using System;
using System.Collections.Generic;
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
        private readonly GameObject _gameObject;
        
        public bool IsControlled { get; private set; }
        public bool AllowEventSending { get; set; }

        private readonly Dictionary<string, FsmTransition[]> _stateTransitions;

        protected readonly HashSet<string> ControlledStates;
        protected readonly Dictionary<byte, ControlledVariable> ControlledVariables;
        
        private HashSet<string> _multiOutgoingTransitionStates;
        private HashSet<string> _methodCreatedStates;
        
        protected PlayMakerFSM Fsm;

        private string _lastState;

        protected Entity(
            NetClient netClient, 
            EntityType entityType, 
            byte entityId,
            GameObject gameObject
        ) {
            _netClient = netClient;
            _entityType = entityType;
            _entityId = entityId;
            _gameObject = gameObject;
            
            _stateTransitions = new Dictionary<string, FsmTransition[]>();

            ControlledStates = new HashSet<string>();
            ControlledVariables = new Dictionary<byte, ControlledVariable>();
            
            // Add a position interpolation component to the enemy so we can smooth out position updates
            // _gameObject.AddComponent<PositionInterpolation>();

            // Register an update event to send position updates
            // MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdate;
        }

        private void OnUpdate() {
            // We don't send updates when this FSM is controlled or when we are not allowed to send events yet
            if (IsControlled || !AllowEventSending) {
                return;
            }
            
            _netClient.UpdateManager.UpdateEntityPosition(_entityType, _entityId, _gameObject.transform.position);
        }

        public virtual void TakeControl() {
            // If the FSM is already controlled, we skip, otherwise we
            // override the stored transitions with empty ones
            if (IsControlled) {
                return;
            }
            
            IsControlled = true;

            foreach (var state in Fsm.FsmStates) {
                // If this is a controlled state
                if (ControlledStates.Contains(state.Name)) {
                    // Store the transitions so we can restore them later
                    _stateTransitions[state.Name] = state.Transitions;
                    
                    // And remove them so the state machine doesn't automatically advance in this state
                    state.Transitions = new FsmTransition[0];
                }
            }
        }

        public virtual void ReleaseControl() {
            if (!IsControlled) {
                return;
            }
            
            IsControlled = false;
            
            foreach (var state in Fsm.FsmStates) {
                // If state name is a key in the dictionary
                if (_stateTransitions.TryGetValue(state.Name, out var transitions)) {
                    // Reset the transitions
                    state.Transitions = transitions;
                }
            }
        }

        public void UpdatePosition(Vector2 position) {
            // _gameObject.GetComponent<PositionInterpolation>().SetNewPosition(position);
        }

        public void UpdateState(byte stateIndex) {
            // If the index is out of bounds, we return
            if (Fsm.FsmStates.Length <= stateIndex) {
                Logger.Warn(this, $"Tried to update entity state to {stateIndex}, but it was out of bounds");
                return;
            }

            Logger.Info(this, $"Received state update, stateIndex: {stateIndex}, name: {GetStateNameByIndex(Fsm, stateIndex)}");
            
            Fsm.SetState(Fsm.FsmStates[stateIndex].Name);
        }

        public void UpdateVariables(byte[] variableArray) {
            var currentIndex = 0;

            while (currentIndex < variableArray.Length) {
                var variableId = variableArray[currentIndex++];

                if (!ControlledVariables.TryGetValue(variableId, out var controlledVariable)) {
                    Logger.Info(this,
                        $"Received variable update, but no corresponding controlled variable could be found for ID: {variableId}");
                    return;
                }
                
                Logger.Info(this, $"Received variable update, ID: {variableId}");

                controlledVariable.UpdateVariable(variableArray, ref currentIndex);
            }
        }
        
        public void Destroy() {
            AllowEventSending = false;

            Object.Destroy(Fsm);

            MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdate;
        }

        protected void CreateDefaultControlledStates() {
            foreach (var state in Fsm.FsmStates) {
                // If there is more than 1 transition out of this state,
                // we add it to the list of controlled states
                if (state.Transitions.Length > 1) {
                    ControlledStates.Add(state.Name);
                }
            }
        }

        protected void CreateDefaultStateEventSending() {
            _multiOutgoingTransitionStates = new HashSet<string>();
            _methodCreatedStates = new HashSet<string>();
                        
            foreach (var state in Fsm.FsmStates) {
                if (state.Transitions.Length > 1) {
                    // This state has more than 1 outgoing transition
                    _multiOutgoingTransitionStates.Add(state.Name);

                    foreach (var transition in state.Transitions) {
                        var toState = transition.ToState;
                        
                        // Make sure we only insert a method once
                        if (!_methodCreatedStates.Contains(toState)) {
                            CreateStateEventSendMethod(toState, true);

                            _methodCreatedStates.Add(toState);
                        }
                    }
                }
            }

            // For each state that does not already have a method inserted,
            // we need to update the last state variable
            foreach (var state in Fsm.FsmStates) {
                var stateName = state.Name;
                if (!_methodCreatedStates.Contains(stateName)) {
                    Fsm.InsertMethod(stateName, 0, () => {
                        _lastState = stateName;
                    });
                }
            }
        }

        protected void CreateStateEventSendMethod(string stateName, bool onlyFromMultiOutgoingState) {
            if (onlyFromMultiOutgoingState) {
                Fsm.InsertMethod(stateName, 0, () => {
                    // If the last state was a state were multiple outgoing transitions were possible
                    // And we happen to go to this state, we need to send an event for this
                    if (_multiOutgoingTransitionStates.Contains(_lastState)) {
                        SendStateUpdate(GetStateIndexByName(Fsm, stateName));
                    }
                                
                    // And we still update the last state
                    _lastState = stateName;
                });
            } else {
                Fsm.InsertMethod(stateName, 0, () => {
                    SendStateUpdate(GetStateIndexByName(Fsm, stateName));

                    // And we still update the last state
                    _lastState = stateName;
                });
            }
        }

        private void SendStateUpdate(int stateIndex) {
            // We don't send updates when this FSM is controlled or when we are not allowed to send events yet
            if (IsControlled || !AllowEventSending) {
                return;
            }

            if (stateIndex > byte.MaxValue) {
                Logger.Warn(this, $"Tried sending state update, but state index was larger than max byte size ({stateIndex})");
                return;
            }
            
            Logger.Info(this, $"Sending state update, index: {stateIndex}, name: {GetStateNameByIndex(Fsm, stateIndex)}");

            _netClient.UpdateManager.UpdateEntityState(_entityType, _entityId, (byte) stateIndex);
        }

        protected void SendVariableUpdate(byte variableId, bool boolVar) {
            // We don't send updates when this FSM is controlled or when we are not allowed to send events yet
            if (IsControlled || !AllowEventSending) {
                return;
            }
            
            var byteList = new List<byte> {
                variableId, 
                (byte) VariableType.Bool, 
                (byte) (boolVar ? 1 : 0)
            };

            _netClient.UpdateManager.UpdateEntityVariables(_entityType, _entityId, byteList);
        }
        
        protected void SendVariableUpdate(byte variableId, int intVar) {
            // We don't send updates when this FSM is controlled or when we are not allowed to send events yet
            if (IsControlled || !AllowEventSending) {
                return;
            }
        
            var byteList = new List<byte> {
                variableId, 
                (byte) VariableType.Int
            };
            byteList.AddRange(BitConverter.GetBytes(intVar));

            _netClient.UpdateManager.UpdateEntityVariables(_entityType, _entityId, byteList);
        }
        
        protected void SendVariableUpdate(byte variableId, float floatVar) {
            // We don't send updates when this FSM is controlled or when we are not allowed to send events yet
            if (IsControlled || !AllowEventSending) {
                return;
            }
            
            var byteList = new List<byte> {
                variableId, 
                (byte) VariableType.Float
            };
            byteList.AddRange(BitConverter.GetBytes(floatVar));

            Logger.Info(this, $"Sending variable update, id: {variableId}, var: {floatVar}");

            _netClient.UpdateManager.UpdateEntityVariables(_entityType, _entityId, byteList);
        }

        protected static int GetStateIndexByName(PlayMakerFSM fsm, string stateName) {
            for (var i = 0; i < fsm.FsmStates.Length; i++) {
                if (fsm.FsmStates[i].Name.Equals(stateName)) {
                    return i;
                }
            }

            return -1;
        }

        protected static string GetStateNameByIndex(PlayMakerFSM fsm, int stateIndex) {
            return fsm.FsmStates[stateIndex].Name;
        }
    }
}