using System;
using System.Collections.Generic;
using HKMP.Networking.Client;
using HKMP.Util;
using UnityEngine;

namespace HKMP.Game.Client.Entity {
    public class ZombieRunner : HealthManagedEntity{
        private static readonly Dictionary<State, string> SimpleEventStates = new Dictionary<State, string> {
            {State.Ready, "Ready"},
            {State.FaceRight, "Face Right"},
            {State.FaceLeft, "Face Left"},
            {State.Reset, "Reset"}
        };

        private static readonly string[] StateUpdateResetNames = {
            "Left or Right?",
            "R?",
            "R? 2"
        };

        public ZombieRunner(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(
            netClient,
            EntityType.ZombieRunner,
            entityId,
            gameObject
        ) {
            Fsm = gameObject.LocateMyFSM("Zombie Swipe");

            CreateEvents();
        }

        private void CreateEvents() {
            //
            // Insert methods for sending updates over network for reached states
            //
            foreach (var stateNamePair in SimpleEventStates) {
                Fsm.InsertMethod(stateNamePair.Value, 0, CreateStateUpdateMethod(() => {
                    Logger.Get().Info(this, $"Sending {stateNamePair.Key} state");
                    SendStateUpdate((byte) stateNamePair.Key);
                }));
            }
            
            Fsm.InsertMethod("Reverse", 0, CreateStateUpdateMethod(() => {
                var variables = new List<byte>();

                // Get the Jump X variable from the FSM and add it as bytes to the variables list
                var reverseBool = Fsm.FsmVariables.GetFsmBool("Reverse").Value;
                variables.AddRange(BitConverter.GetBytes(reverseBool));

                Logger.Get().Info(this, $"Sending Reverse state with variable: {reverseBool}");

                SendStateUpdate((byte) State.Reverse, variables);
            }));
            
            Fsm.InsertMethod("Reverse Back", 0, CreateStateUpdateMethod(() => {
                var variables = new List<byte>();

                // Get the Jump X variable from the FSM and add it as bytes to the variables list
                var reverseBool = Fsm.FsmVariables.GetFsmBool("Reverse").Value;
                variables.AddRange(BitConverter.GetBytes(reverseBool));

                Logger.Get().Info(this, $"Sending Reverse Back state with variable: {reverseBool}");

                SendStateUpdate((byte) State.ReverseBack, variables);
            }));
            
            
            //
            // Insert methods for resetting the update state, so we can start/receive the next update
            //
            foreach (var stateName in StateUpdateResetNames) {
                Fsm.InsertMethod(stateName, 0, StateUpdateDone);
            }
        }


        protected override void InternalTakeControl() {
            foreach (var stateName in StateUpdateResetNames) {
                RemoveOutgoingTransitions(stateName);
            }
        }

        protected override void InternalReleaseControl() {
            RestoreAllOutgoingTransitions();

            // Restore the original actions
            RestoreAllActions();
        }

        protected override void StartQueuedUpdate(byte state, List<byte> variables) {
            base.StartQueuedUpdate(state, variables);
            
            var variableArray = variables.ToArray();

            var enumState = (State) state;

            if (SimpleEventStates.TryGetValue(enumState, out var stateName)) {
                Logger.Get().Info(this, $"Received {enumState} state");
                Fsm.SetState(stateName);
                return;
            }
            
            switch (enumState) {
                case State.Reverse:
                    if (variableArray.Length == 4) {
                        var reverseBool = BitConverter.ToBoolean(variableArray, 0);

                        Logger.Get().Info(this, $"Received Reverse state with variable: {reverseBool}");

                        Fsm.FsmVariables.GetFsmBool("Reverse").Value = reverseBool;
                    } else {
                        Logger.Get().Warn(this, $"Received Reverse state with incorrect variable array, length: {variableArray.Length}");
                    }

                    Fsm.SetState("Reverse");
                    break;
                case State.ReverseBack:
                    if (variableArray.Length == 4) {
                        var reverseBool = BitConverter.ToBoolean(variableArray, 0);

                        Logger.Get().Info(this, $"Received Reverse Back state with variable: {reverseBool}");

                        Fsm.FsmVariables.GetFsmBool("Reverse Back").Value = reverseBool;
                    } else {
                        Logger.Get().Warn(this, $"Received Reverse Back state with incorrect variable array, length: {variableArray.Length}");
                    }

                    Fsm.SetState("Reverse Back");
                    break;
            }
        }

        private enum State {
            Ready = 0,
            Reverse,
            ReverseBack,
            FaceRight,
            FaceLeft,
            Reset
        }
    }
}