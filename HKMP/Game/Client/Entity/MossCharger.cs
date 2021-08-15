// using System;
// using System.Collections.Generic;
// using Hkmp.Networking.Client;
// using Hkmp.Util;
// using UnityEngine;
//
// namespace Hkmp.Game.Client.Entity {
//     public class MossCharger : HealthManagedEntity {
//         private static readonly Dictionary<State, string> SimpleEventStates = new Dictionary<State, string> {
//             {State.Shake, "Shake"},
//             {State.Submerge, "Submerge 1"},
//             {State.HitRight, "Hit Right"},
//             {State.HitLeft, "Hit Left"}
//         };
//
//         private static readonly string[] StateUpdateResetNames = {
//             // After the moss charger has been awoken, this is the end of the sequence
//             "Roar End",
//             // This is the state we end up in after the Submerge sequence
//             "Hidden",
//             // This is the state after the Emerge sequence
//             "Charge",
//             // This is the state at the end of the Leap sequence
//             "Land"
//         };
//
//         public MossCharger(
//             NetClient netClient,
//             byte entityId,
//             GameObject gameObject
//         ) : base(netClient, EntityType.MossCharger, entityId, gameObject) {
//             Fsm = gameObject.LocateMyFSM("Mossy Control");
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
//             Fsm.InsertMethod("Emerge", 0, CreateStateUpdateMethod(() => {
//                 var variables = new List<byte>();
//
//                 var appearX = Fsm.FsmVariables.GetFsmFloat("Appear X").Value;
//                 variables.AddRange(BitConverter.GetBytes(appearX));
//                 
//                 Logger.Get().Info(this, $"Sending Emerge state with variable: {appearX}");
//                 
//                 SendStateUpdate((byte) State.Emerge, variables);
//             }));
//             
//             Fsm.InsertMethod("Leap Start", 0, CreateStateUpdateMethod(() => {
//                 var variables = new List<byte>();
//
//                 var appearX = Fsm.FsmVariables.GetFsmFloat("Appear X").Value;
//                 variables.AddRange(BitConverter.GetBytes(appearX));
//                 
//                 Logger.Get().Info(this, $"Sending Leap state with variable: {appearX}");
//                 
//                 SendStateUpdate((byte) State.Leap, variables);
//             }));
//             
//             //
//             // Insert methods for resetting the update state, so we can start/receive the next update
//             //
//             foreach (var stateName in StateUpdateResetNames) {
//                 var state = Fsm.GetState(stateName);
//
//                 Fsm.InsertMethod(stateName, state.Actions.Length, StateUpdateDone);
//             }
//         }
//
//         protected override void InternalTakeControl() {
//             foreach (var stateName in StateUpdateResetNames) {
//                 RemoveOutgoingTransitions(stateName);
//             }
//         }
//
//         protected override void InternalReleaseControl() {
//             RestoreAllOutgoingTransitions();
//         }
//
//         protected override void StartQueuedUpdate(byte state, List<byte> variables) {
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
//                 case State.Emerge:
//                     if (variableArray.Length == 4) {
//                         var appearX = BitConverter.ToSingle(variableArray, 0);
//                         
//                         Logger.Get().Info(this, $"Received Emerge with variable: {appearX}");
//
//                         Fsm.FsmVariables.GetFsmFloat("Appear X").Value = appearX;
//                     } else {
//                         Logger.Get().Info(this, $"Received Emerge with incorrect variable array, length: {variableArray.Length}");
//                     }
//
//                     Fsm.SetState("Emerge");
//                     break;
//                 case State.Leap:
//                     if (variableArray.Length == 4) {
//                         var appearX = BitConverter.ToSingle(variableArray, 0);
//                         
//                         Logger.Get().Info(this, $"Received Leap with variable: {appearX}");
//
//                         Fsm.FsmVariables.GetFsmFloat("Appear X").Value = appearX;
//                     } else {
//                         Logger.Get().Info(this, $"Received Leap with incorrect variable array, length: {variableArray.Length}");
//                     }
//
//                     Fsm.SetState("Leap Start");
//                     break;
//             }
//         }
//
//         private enum State {
//             Shake,
//             Submerge,
//             Emerge,
//             HitRight,
//             HitLeft,
//             Leap,
//         }
//     }
// }