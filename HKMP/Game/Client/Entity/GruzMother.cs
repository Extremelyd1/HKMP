using System;
using System.Collections.Generic;
using HKMP.Networking.Client;
using HKMP.Util;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace HKMP.Game.Client.Entity {
    public class GruzMother : Entity {
        private static readonly Dictionary<State, string> SimpleEventStates = new Dictionary<State, string> {
            {State.WakeSound, "Wake Sound"},
            {State.Slam, "Slam Antic"},
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
        private readonly HealthManager _healthManager;
        
        private FsmStateAction[] _slamActions;
        private FsmStateAction[] _chargeActions;

        private FsmTransition[] _bounceTransitions;

        private bool _allowDeath;

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
            _healthManager = gameObject.GetComponent<HealthManager>();

            CreateEvents();

            CreateHooks();
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
            
            // We insert this method at index 7 to make sure the charge angle float has been set
            // and just before the back angle is calculated
            Fsm.InsertMethod("Charge Antic", 7, CreateStateUpdateMethod(() => {
                var variables = new List<byte>();

                var chargeAngle = Fsm.FsmVariables.GetFsmFloat("Charge Angle").Value;
                variables.AddRange(BitConverter.GetBytes(chargeAngle));
                
                Logger.Get().Info(this, $"Sending Charge Antic state with variable: {chargeAngle}");
                
                SendStateUpdate((byte) State.ChargeAntic, variables);
            }));
            
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

            //
            // Insert methods for resetting the update state, so we can start/receive the next update
            //
            foreach (var stateName in StateUpdateResetNames) {
                Fsm.InsertMethod(stateName, 0, StateUpdateDone);
            }
        }

        private void CreateHooks() {
            On.HealthManager.Die += HealthManagerOnDieHook;
        }

        private void HealthManagerOnDieHook(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion) {
            if (self != _healthManager) {
                return;
            }

            if (IsControlled) {
                if (!_allowDeath) {
                    return;
                }

                orig(self, attackDirection, attackType, ignoreEvasion);
                return;
            }

            if (!AllowEventSending) {
                orig(self, attackDirection, attackType, ignoreEvasion);
                return;
            }

            var variables = new List<byte>();

            variables.AddRange(attackDirection.HasValue
                ? BitConverter.GetBytes(attackDirection.Value)
                : BitConverter.GetBytes(0f));
            variables.Add((byte) attackType);
            variables.AddRange(BitConverter.GetBytes(ignoreEvasion));
                
            Logger.Get().Info(this, $"Sending Die state with variables ({variables.Count} bytes): {attackDirection}, {attackType}, {ignoreEvasion}");

            SendStateUpdate((byte) State.Die, variables);

            orig(self, attackDirection, attackType, ignoreEvasion);

            Destroy();
        }


        protected override void InternalTakeControl() {
            foreach (var stateName in StateUpdateResetNames) {
                RemoveOutgoingTransitions(stateName);
            }
            
            // Make sure that the local player can't trigger the wake up
            RemoveOutgoingTransition("Sleep", "Wake Sound");
            
            // Remove the actions that let the object face a target
            Fsm.RemoveAction("Slam Antic", typeof(FaceObject));
            Fsm.RemoveAction("Charge Antic", typeof(FaceObject));
            // Also remove the action that calculates the angle to the wrong target
            Fsm.RemoveAction("Charge Antic", typeof(GetAngleToTarget2D));

            var stoppedState = _bouncerFsm.GetState("Stopped");
            _bounceTransitions = stoppedState.Transitions;
            stoppedState.Transitions = new FsmTransition[0];
            
            _bouncerFsm.SendEvent("STOP");
        }

        protected override void InternalReleaseControl() {
            RestoreAllOutgoingTransitions();

            // Restore the original actions
            Fsm.GetState("Slam Antic").Actions = _slamActions;
            Fsm.GetState("Charge Antic").Actions = _chargeActions;

            _bouncerFsm.GetState("Stopped").Transitions = _bounceTransitions;
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
                case State.ChargeAntic:
                    if (variableArray.Length == 4) {
                        var chargeAngle = BitConverter.ToSingle(variableArray, 0);
                        
                        Logger.Get().Info(this, $"Received Charge Antic with variable: {chargeAngle}");

                        Fsm.FsmVariables.GetFsmFloat("Charge Angle").Value = chargeAngle;
                    } else {
                        Logger.Get().Info(this, $"Received Charge Antic with incorrect variable array, length: {variableArray.Length}");
                    }

                    Fsm.SetState("Charge Antic");
                    break;
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
                case State.Die:
                    if (variableArray.Length == 6) {
                        float? directionFloat = BitConverter.ToSingle(variableArray, 0);
                        var attackType = (AttackTypes) variableArray[4];
                        var ignoreEvasion = BitConverter.ToBoolean(variableArray, 5);
                        
                        Logger.Get().Info(this, $"Received Die state with variable: {directionFloat}, {attackType}, {ignoreEvasion}");

                        _allowDeath = true;
                        _healthManager.Die(directionFloat, attackType, ignoreEvasion);
                        
                        // We destroy after death to make sure we don't interfere with anything else
                        Destroy();
                    } else {
                        Logger.Get().Info(this, $"Received Die state with incorrect variable array, length: {variableArray.Length}");
                    }

                    break;
            }
        }

        protected override bool IsInterruptingState(byte state) {
            return ((State) state).Equals(State.Die);
        }

        public override void Destroy() {
            base.Destroy();

            On.HealthManager.Die -= HealthManagerOnDieHook;
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
            ChargeRecoverU,
            Die
        }
    }
}