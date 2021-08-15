// using System.Collections.Generic;
// using Hkmp.Networking.Client;
// using Hkmp.Util;
// using HutongGames.PlayMaker;
// using UnityEngine;
//
// namespace Hkmp.Game.Client.Entity {
//     public class BattleGate : Entity {
//         private static readonly Dictionary<State, string> SimpleEventStates = new Dictionary<State, string> {
//             {State.Close, "Close 1"},
//             {State.QuickClose, "Quick Close"},
//             {State.Open, "Open"},
//             {State.QuickOpen, "Quick Open"}
//         };
//
//         private static readonly string[] StateUpdateResetNames = {
//             // After the normal close sequence
//             "Close 2",
//
//             // We can immediately start receiving events after these states have been reached
//             "Quick Close",
//             "Open",
//             "Quick Open"
//         };
//
//         public BattleGate(
//             NetClient netClient,
//             byte entityId,
//             GameObject gameObject
//         ) : base(netClient, EntityType.BattleGate, entityId, gameObject) {
//             Fsm = gameObject.LocateMyFSM("BG Control");
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
//             var enumState = (State) state;
//
//             if (SimpleEventStates.TryGetValue(enumState, out var stateName)) {
//                 Logger.Get().Info(this, $"Received {enumState} state");
//                 Fsm.SetState(stateName);
//             }
//         }
//
//         protected override bool IsInterruptingState(byte state) {
//             // The Quick Open state is the only interrupting state
//             return state == (byte) State.QuickOpen;
//         }
//
//         private enum State {
//             Close,
//             QuickClose,
//             Open,
//             QuickOpen
//         }
//     }
// }