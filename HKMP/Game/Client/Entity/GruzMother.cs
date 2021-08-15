// using System;
// using System.Collections.Generic;
// using Hkmp.Networking.Client;
// using Hkmp.Util;
// using HutongGames.PlayMaker;
// using HutongGames.PlayMaker.Actions;
// using UnityEngine;
//
// namespace Hkmp.Game.Client.Entity {
//     public class GruzMother : HealthManagedEntity {
//         private static readonly Dictionary<State, string> SimpleEventStates = new Dictionary<State, string> {
//             {State.WakeSound, "Wake Sound"},
//             {State.Slam, "Slam Antic"},
//             {State.TurnLeft, "Turn Left"},
//             {State.TurnRight, "Turn Right"},
//             {State.SlamDown, "Slam Down"},
//             {State.SlamUp, "Slam Up"},
//             {State.SlamEnd, "Slam End"},
//             {State.Buzz, "Buzz"},
//             {State.Choose, "Super Choose"},
//             {State.ChargeRecoverL, "Charge Recover L"},
//             {State.ChargeRecoverR, "Charge Recover R"},
//             {State.ChargeRecoverD, "Charge Recover D"},
//             {State.ChargeRecoverU, "Charge Recover U"},
//         };
//
//         private static readonly string[] StateUpdateResetNames = {
//             // After the slam antic
//             "Check Direction",
//             // We reach this state after a random wait from Buzz
//             "Super Choose",
//             // We reach this state after the slam and charge sequences
//             "Buzz",
//             // All the slamming sequences end here
//             "Flying",
//             // After the charge antic
//             "Charge"
//         };
//
//         private readonly PlayMakerFSM _bouncerFsm;
//
//         private FsmTransition[] _bounceTransitions;
//         
//         private bool _isInitialized;
//
//         public GruzMother(
//             NetClient netClient,
//             byte entityId,
//             GameObject gameObject
//         ) : base(
//             netClient,
//             EntityType.GruzMother,
//             entityId,
//             gameObject
//         ) {
//             Fsm = gameObject.LocateMyFSM("Big Fly Control");
//
//             _bouncerFsm = gameObject.LocateMyFSM("bouncer_control");
//
//             CreateEvents();
//         }
//
//         private void CreateEvents() {
//             //
//             // Insert methods for sending updates over network for reached states
//             //
//             foreach (var stateNamePair in SimpleEventStates) {
//                 Fsm.InsertMethod(stateNamePair.Value, 0, CreateStateUpdateMethod(() => {
//                     Logger.Get().Info(this, $"Sending {stateNamePair.Key} state");
//                     SendStateUpdate((byte) stateNamePair.Key);
//                 }));
//             }
//             
//             // We insert this method at index 7 to make sure the charge angle float has been set
//             // and just before the back angle is calculated
//             Fsm.InsertMethod("Charge Antic", 7, CreateStateUpdateMethod(() => {
//                 var variables = new List<byte>();
//
//                 var chargeAngle = Fsm.FsmVariables.GetFsmFloat("Charge Angle").Value;
//                 variables.AddRange(BitConverter.GetBytes(chargeAngle));
//                 
//                 Logger.Get().Info(this, $"Sending Charge Antic state with variable: {chargeAngle}");
//                 
//                 SendStateUpdate((byte) State.ChargeAntic, variables);
//             }));
//             
//             Fsm.InsertMethod("Go Left", 0, CreateStateUpdateMethod(() => {
//                 var variables = new List<byte>();
//
//                 var slamTimeFloat = Fsm.FsmVariables.GetFsmFloat("Slam Time").Value;
//                 variables.AddRange(BitConverter.GetBytes(slamTimeFloat));
//                 
//                 Logger.Get().Info(this, $"Sending Go Left state with variable: {slamTimeFloat}");
//
//                 SendStateUpdate((byte) State.GoLeft, variables);
//             }));
//             Fsm.InsertMethod("Go Right", 0, CreateStateUpdateMethod(() => {
//                 var variables = new List<byte>();
//
//                 var slamTimeFloat = Fsm.FsmVariables.GetFsmFloat("Slam Time").Value;
//                 variables.AddRange(BitConverter.GetBytes(slamTimeFloat));
//                 
//                 Logger.Get().Info(this, $"Sending Go Right state with variable: {slamTimeFloat}");
//
//                 SendStateUpdate((byte) State.GoRight, variables);
//             }));
//
//             //
//             // Insert methods for resetting the update state, so we can start/receive the next update
//             //
//             foreach (var stateName in StateUpdateResetNames) {
//                 Fsm.InsertMethod(stateName, 0, StateUpdateDone);
//             }
//         }
//
//
//         protected override void InternalTakeControl() {
//             foreach (var stateName in StateUpdateResetNames) {
//                 RemoveOutgoingTransitions(stateName);
//             }
//             
//             // Make sure that the local player can't trigger the wake up
//             RemoveOutgoingTransition("Sleep", "Wake Sound");
//             
//             // Remove the actions that let the object face a target
//             RemoveAction("Slam Antic", typeof(FaceObject));
//             RemoveAction("Charge Antic", typeof(FaceObject));
//             // Also remove the action that calculates the angle to the wrong target
//             RemoveAction("Charge Antic", typeof(GetAngleToTarget2D));
//
//             var stoppedState = _bouncerFsm.GetState("Stopped");
//             _bounceTransitions = stoppedState.Transitions;
//             stoppedState.Transitions = new FsmTransition[0];
//             
//             _bouncerFsm.SendEvent("STOP");
//         }
//
//         protected override void InternalReleaseControl() {
//             RestoreAllOutgoingTransitions();
//
//             // Restore the original actions
//             RestoreAllActions();
//
//             _bouncerFsm.GetState("Stopped").Transitions = _bounceTransitions;
//         }
//
//         protected override void StartQueuedUpdate(byte state, List<byte> variables) {
//             if (!_isInitialized) {
//                 Initialize();
//                 _isInitialized = true;
//             }
//             
//             base.StartQueuedUpdate(state, variables);
//             
//             var variableArray = variables.ToArray();
//
//             var enumState = (State) state;
//
//             if (SimpleEventStates.TryGetValue(enumState, out var stateName)) {
//                 Logger.Get().Info(this, $"Received {enumState} state");
//                 Fsm.SetState(stateName);
//                 
//                 return;
//             }
//
//             switch (enumState) {
//                 case State.ChargeAntic:
//                     if (variableArray.Length == 4) {
//                         var chargeAngle = BitConverter.ToSingle(variableArray, 0);
//                         
//                         Logger.Get().Info(this, $"Received Charge Antic with variable: {chargeAngle}");
//
//                         Fsm.FsmVariables.GetFsmFloat("Charge Angle").Value = chargeAngle;
//                     } else {
//                         Logger.Get().Info(this, $"Received Charge Antic with incorrect variable array, length: {variableArray.Length}");
//                     }
//
//                     Fsm.SetState("Charge Antic");
//                     break;
//                 case State.GoLeft:
//                     if (variableArray.Length == 4) {
//                         var slamTimeFloat = BitConverter.ToSingle(variableArray, 0);
//                         
//                         Logger.Get().Info(this, $"Received Go Left state with variable: {slamTimeFloat}");
//
//                         Fsm.FsmVariables.GetFsmFloat("Slam Time").Value = slamTimeFloat;
//                     } else {
//                         Logger.Get().Info(this, $"Received Go Left state with incorrect variable array, length: {variableArray.Length}");
//                     }
//
//                     Fsm.SetState("Go Left");
//                     break;
//                 case State.GoRight:
//                     if (variableArray.Length == 4) {
//                         var slamTimeFloat = BitConverter.ToSingle(variableArray, 0);
//                         
//                         Logger.Get().Info(this, $"Received Go Right state with variable: {slamTimeFloat}");
//
//                         Fsm.FsmVariables.GetFsmFloat("Slam Time").Value = slamTimeFloat;
//                     } else {
//                         Logger.Get().Info(this, $"Received Go Right state with incorrect variable array, length: {variableArray.Length}");
//                     }
//
//                     Fsm.SetState("Go Right");
//                     break;
//             }
//         }
//
//         private void Initialize() {
//             // Remove invincibility
//             var healthManager = GameObject.GetComponent<HealthManager>();
//             healthManager.IsInvincible = false;
//             healthManager.InvincibleFromDirection = 0;
//             
//             // Activate the Hero Damager child so we can start taking damage
//             var heroDamagerChild = GameObject.FindGameObjectInChildren("Hero Damager");
//             heroDamagerChild.SetActive(true);
//         }
//
//         private enum State {
//             WakeSound = 0,
//             Slam,
//             ChargeAntic,
//             GoLeft,
//             GoRight,
//             TurnLeft,
//             TurnRight,
//             SlamDown,
//             SlamUp,
//             SlamEnd,
//             Buzz,
//             Choose,
//             ChargeRecoverL,
//             ChargeRecoverR,
//             ChargeRecoverD,
//             ChargeRecoverU
//         }
//     }
// }