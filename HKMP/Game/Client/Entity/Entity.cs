using System;
using System.Collections.Generic;
using HKMP.Networking.Client;
using HutongGames.PlayMaker;
using ModCommon.Util;
using Object = UnityEngine.Object;

namespace HKMP.Game.Client.Entity {
    public abstract class Entity : IEntity {

        protected readonly NetClient NetClient;
        private readonly EntityType _entityType;
        private readonly byte _entityId;
        
        public bool IsControlled { get; private set; }
        public bool AllowEventSending { get; set; }

        private readonly Dictionary<string, FsmTransition[]> _stateTransitions;

        protected Dictionary<byte, string> VariableIndices;

        private HashSet<string> _multiOutgoingTransitionStates;
        private HashSet<string> _methodCreatedStates;
        
        protected PlayMakerFSM Fsm;

        private string _lastState;

        protected Entity(NetClient netClient, EntityType entityType, byte entityId) {
            NetClient = netClient;
            _entityType = entityType;
            _entityId = entityId;
            
            _stateTransitions = new Dictionary<string, FsmTransition[]>();
        }

        public virtual void TakeControl() {
            // If the FSM is already controlled, we skip, otherwise we
            // override the stored transitions with empty ones
            if (IsControlled) {
                return;
            }
            
            IsControlled = true;

            foreach (var state in Fsm.FsmStates) {
                // If there is more than 1 transition out of this state
                if (state.Transitions.Length > 1) {
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

        public void UpdateState(byte stateIndex) {
            // If the index is out of bounds, we return
            if (Fsm.FsmStates.Length <= stateIndex) {
                Logger.Warn(this, $"Tried to update entity state to {stateIndex}, but it was out of bounds");
                return;
            }

            Logger.Info(this, $"Received state update, stateIndex: {stateIndex}");
            
            Fsm.SetState(Fsm.FsmStates[stateIndex].Name);
        }

        public void UpdateVariables(byte[] variableArray) {
            var currentIndex = 0;

            while (currentIndex < variableArray.Length) {
                var variableId = variableArray[currentIndex++];

                var variableName = VariableIndices[variableId];
                
                Logger.Info(this, $"Received variable update, variableName: {variableName}");
                
                SetFsmVariable(Fsm, variableName, variableArray, ref currentIndex);
            }
        }
        
        public void Destroy() {
            AllowEventSending = false;

            Object.Destroy(Fsm);
        }

        protected void CreateStateEventSending() {
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
                            Fsm.InsertMethod(toState, 0, () => {
                                // If the last state was a state were multiple outgoing transitions were possible
                                // And we happen to go to this state, we need to send an event for this
                                if (_multiOutgoingTransitionStates.Contains(_lastState)) {
                                    Logger.Info(this, $"Sending state update, name: {toState}");
                                    
                                    SendStateUpdate(GetStateIndexByName(Fsm, toState));
                                }
                                
                                // And we still update the last state
                                _lastState = toState;
                            });

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

        protected void SendStateUpdate(int stateIndex) {
            // We don't send updates when this FSM is controlled or when we are not allowed to send events yet
            if (IsControlled || !AllowEventSending) {
                return;
            }

            if (stateIndex > byte.MaxValue) {
                Logger.Warn(this, $"Tried sending state update, but state index was larger than max byte size ({stateIndex})");
                return;
            }
            
            Logger.Info(this, $"Sending state update, index: {stateIndex}");

            NetClient.SendEntityStateUpdate(_entityType, _entityId, (byte) stateIndex);
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

            NetClient.SendEntityVariableUpdate(_entityType, _entityId, byteList);
        }
        
        protected void SendVariableUpdate(byte variableId, int intVar) {
            // We don't send updates when this FSM is controlled or when we are not allowed to send events yet
            if (IsControlled || !AllowEventSending) {
                return;
            }
        
            var byteList = new List<byte> {
                variableId, 
                (byte) VariableType.Int, 
                (byte) intVar
            };

            NetClient.SendEntityVariableUpdate(_entityType, _entityId, byteList);
        }
        
        protected void SendVariableUpdate(byte variableId, float floatVar) {
            // We don't send updates when this FSM is controlled or when we are not allowed to send events yet
            if (IsControlled || !AllowEventSending) {
                return;
            }
            
            var byteList = new List<byte> {
                variableId, 
                (byte) VariableType.Float, 
                (byte) floatVar
            };

            NetClient.SendEntityVariableUpdate(_entityType, _entityId, byteList);
        }
        
        protected static void SetFsmVariable(PlayMakerFSM fsm, string variableName, byte[] variableArray, ref int currentIndex) {
            if (!fsm.FsmVariables.Contains(variableName)) {
                Logger.Info(typeof(Entity),
                    $"Tried to update FSM variable with name: {variableName}, but the FSM does not contain such a variable");
                return;
            }
            
            var typeByte = variableArray[currentIndex++];
            var type = (VariableType) typeByte;

            switch (type) {
                case VariableType.Bool:
                    var boolVar = BitConverter.ToBoolean(variableArray, currentIndex);
                    currentIndex += 1;

                    fsm.FsmVariables.GetFsmBool(variableName).Value = boolVar;
                    break;
                case VariableType.Int:
                    var intVar = BitConverter.ToInt32(variableArray, currentIndex);
                    currentIndex += 4;
                    
                    fsm.FsmVariables.GetFsmInt(variableName).Value = intVar;
                    break;
                case VariableType.Float:
                    var floatVar = BitConverter.ToSingle(variableArray, currentIndex);
                    currentIndex += 4;

                    fsm.FsmVariables.GetFsmFloat(variableName).Value = floatVar;
                    break;
            }
        }

        private static int GetStateIndexByName(PlayMakerFSM fsm, string stateName) {
            for (var i = 0; i < fsm.FsmStates.Length; i++) {
                if (fsm.FsmStates[i].Name.Equals(stateName)) {
                    return i;
                }
            }

            return -1;
        }
    }
}