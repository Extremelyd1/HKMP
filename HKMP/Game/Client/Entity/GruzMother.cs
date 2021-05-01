using System;
using System.Collections.Generic;
using HKMP.Networking.Client;
using HKMP.Util;
using HutongGames.PlayMaker;
using UnityEngine;

namespace HKMP.Game.Client.Entity {
    public class GruzMother : Entity {
        private static readonly Dictionary<State, string> SimpleEventStates = new Dictionary<State, string> {
            {State.WakeSound, "Wake Sound"},
            {State.Slam, "Slam Antic"},
            {State.ChargeAntic, "Charge Antic"},
            {State.TurnLeft, "Turn Left"},
            {State.TurnRight, "Turn Right"},
            {State.SlamDown, "Slam Down"},
            {State.SlamUp, "Slam Up"},
            {State.SlamEnd, "Slam End"},
            {State.Buzz, "Buzz"},
            {State.Choose, "Super Choose"},
            {State.ChargeRecoverL, "Charge Recover L"},
            {State.ChargeRecoverR, "Charge Recover R"},
            {State.ChargeRecoverD, "Charge Recover D"},
            {State.ChargeRecoverU, "Charge Recover U"},
        };

        private static readonly string[] StateUpdateResetNames = {
            // After the slam antic
            "Check Direction",
            // We reach this state after a random wait from Buzz
            "Super Choose",
            // We reach this state after the slam and charge sequences
            "Buzz",
            // All the slamming sequences end here
            "Flying",
            // After the charge antic
            "Charge"
        };

        private readonly PlayMakerFSM _bouncerFsm; 
        
        private FsmStateAction[] _slamActions;
        private FsmStateAction[] _chargeActions;

        private FsmStateAction[] _flyActions;

        public GruzMother(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(
            netClient,
            EntityType.GruzMother,
            entityId,
            gameObject
        ) {
            Fsm = gameObject.LocateMyFSM("Big Fly Control");

            _bouncerFsm = gameObject.LocateMyFSM("bouncer_control");

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
            
            Fsm.InsertMethod("Go Left", 0, CreateStateUpdateMethod(() => {
                var variables = new List<byte>();

                var slamTimeFloat = Fsm.FsmVariables.GetFsmFloat("Slam Time").Value;
                variables.AddRange(BitConverter.GetBytes(slamTimeFloat));
                
                Logger.Get().Info(this, $"Sending Go Left state with variable: {slamTimeFloat}");

                SendStateUpdate((byte) State.GoLeft, variables);
            }));
            Fsm.InsertMethod("Go Right", 0, CreateStateUpdateMethod(() => {
                var variables = new List<byte>();

                var slamTimeFloat = Fsm.FsmVariables.GetFsmFloat("Slam Time").Value;
                variables.AddRange(BitConverter.GetBytes(slamTimeFloat));
                
                Logger.Get().Info(this, $"Sending Go Right state with variable: {slamTimeFloat}");

                SendStateUpdate((byte) State.GoRight, variables);
            }));

            // Store the array of actions of these states so we can revert them later
            _slamActions = Fsm.GetState("Slam Antic").Actions;
            _chargeActions = Fsm.GetState("Charge Antic").Actions;

            _flyActions = _bouncerFsm.GetState("Fly 2").Actions;

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
            
            // Make sure that the local player can't trigger the wake up
            RemoveOutgoingTransition("Sleep", "Wake Sound");
            
            // Remove the actions that let the object face a target
            Fsm.RemoveAction("Slam Antic", 4);
            Fsm.RemoveAction("Charge Antic", 5);
            // Also an action in a separate FSM that flips the sprite every frame
            // based on a bool value
            _bouncerFsm.RemoveAction("Fly 2", 0);
        }

        protected override void InternalReleaseControl() {
            RestoreAllOutgoingTransitions();

            // Restore the original actions
            Fsm.GetState("Slam Antic").Actions = _slamActions;
            Fsm.GetState("Charge Antic").Actions = _chargeActions;

            _bouncerFsm.GetState("Fly 2").Actions = _flyActions;
            
            
        }

        protected override void StartQueuedUpdate(byte state, List<byte> variables) {
            var variableArray = variables.ToArray();

            var enumState = (State) state;

            if (SimpleEventStates.TryGetValue(enumState, out var stateName)) {
                Logger.Get().Info(this, $"Received {enumState} state");
                Fsm.SetState(stateName);
                
                return;
            }

            switch (enumState) {
                case State.GoLeft:
                    if (variableArray.Length == 4) {
                        var slamTimeFloat = BitConverter.ToSingle(variableArray, 0);
                        
                        Logger.Get().Info(this, $"Received Go Left state with variable: {slamTimeFloat}");

                        Fsm.FsmVariables.GetFsmFloat("Slam Time").Value = slamTimeFloat;
                    } else {
                        Logger.Get().Info(this, $"Received Go Left state with incorrect variable array, length: {variableArray.Length}");
                    }

                    Fsm.SetState("Go Left");
                    break;
                case State.GoRight:
                    if (variableArray.Length == 4) {
                        var slamTimeFloat = BitConverter.ToSingle(variableArray, 0);
                        
                        Logger.Get().Info(this, $"Received Go Right state with variable: {slamTimeFloat}");

                        Fsm.FsmVariables.GetFsmFloat("Slam Time").Value = slamTimeFloat;
                    } else {
                        Logger.Get().Info(this, $"Received Go Right state with incorrect variable array, length: {variableArray.Length}");
                    }

                    Fsm.SetState("Go Right");
                    break;
            }
        }

        protected override bool IsInterruptingState(byte state) {
            return false;
        }

        private enum State {
            WakeSound = 0,
            Slam,
            ChargeAntic,
            GoLeft,
            GoRight,
            TurnLeft,
            TurnRight,
            SlamDown,
            SlamUp,
            SlamEnd,
            Buzz,
            Choose,
            ChargeRecoverL,
            ChargeRecoverR,
            ChargeRecoverD,
            ChargeRecoverU
        }
    }
}