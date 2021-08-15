// using System;
// using System.Collections.Generic;
// using Hkmp.Networking.Client;
// using Hkmp.Util;
// using HutongGames.PlayMaker.Actions;
// using UnityEngine;
//
// namespace Hkmp.Game.Client.Entity {
//     public class VengeflyKing : HealthManagedEntity {
//         private static readonly Dictionary<State, string> SimpleEventStates = new Dictionary<State, string> {
//             {State.Unfurl, "Unfurl"},
//             {State.SwoopRight, "Swoop R"},
//             {State.SwoopLeft, "Swoop L"},
//             {State.RoarRight, "Roar Right"},
//             {State.RoarLeft, "Roar Left"}
//         };
//         private static readonly Dictionary<State, string> SimpleEventStatesCol = new Dictionary<State, string> {
//             {State.Unfurl, "Intro Roar Antic"},
//             {State.SwoopRight, "Swoop R"},
//             {State.SwoopLeft, "Swoop L"},
//             {State.RoarRight, "Roar Right"},
//             {State.RoarLeft, "Roar Left"}
//         };
//         
//         private static readonly string[] StateUpdateResetNames = {
//             // After the initialization sequence, swoop and summon sequences
//             "Idle",
//             // After the swoop antic
//             "Swoop Direction",
//             // After the summon antic
//             "Check Dir",
//         };
//
//         // The colosseum variant has a slightly different FSM, which is used in the colosseum and in Godhome
//         private readonly bool _colosseum;
//         
//         public VengeflyKing(
//             NetClient netClient,
//             byte entityId,
//             GameObject gameObject,
//             bool colosseum
//         ) : base(netClient, EntityType.VengeflyKing, entityId, gameObject) {
//             _colosseum = colosseum;
//             
//             Fsm = gameObject.LocateMyFSM("Big Buzzer");
//
//             CreateEvents();
//         }
//
//         private void CreateEvents() {
//             //
//             // Insert methods for sending updates over network for reached states
//             //
//             var eventStates = _colosseum ? SimpleEventStatesCol : SimpleEventStates;
//             foreach (var stateNamePair in eventStates) {
//                 Fsm.InsertMethod(stateNamePair.Value, 0, CreateStateUpdateMethod(() => {
//                     Logger.Get().Info(this, $"Sending {stateNamePair.Key} state");
//                     SendStateUpdate((byte) stateNamePair.Key);
//                 }));
//             }
//
//             // Send this at index 1 so we know that the swoop isn't cancelled
//             Fsm.InsertMethod("Swoop Antic", 1, CreateStateUpdateMethod(() => {
//                 Logger.Get().Info(this, "Sending SwoopAntic state");
//                 SendStateUpdate((byte) State.SwoopAntic);
//             }));
//             
//             // Send this at index 1 so we know that the swoop isn't cancelled
//             Fsm.InsertMethod("Summon Antic", 1, CreateStateUpdateMethod(() => {
//                 Logger.Get().Info(this, "Sending SummonAntic state");
//                 SendStateUpdate((byte) State.SummonAntic);
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
//             
//             if (!_colosseum) {
//                 // Prevent the FSM from starting the Unfurl sequence itself
//                 RemoveOutgoingTransitions("Hanging");
//             }
//
//             RemoveAction("Idle", typeof(FaceObject));
//             // Remove the action that makes sure that the fly keeps it distance to the incorrect hero
//             RemoveAction("Idle", typeof(DistanceFly));
//             // Remove the actions that do movement by themselves to prevent interference with networked movement
//             RemoveAction("Swoop L", typeof(SetVelocity2d));
//             RemoveAction("Swoop R", typeof(SetVelocity2d));
//             RemoveAction("Swoop L", typeof(iTweenMoveBy));
//             RemoveAction("Swoop R", typeof(iTweenMoveBy));
//             RemoveAction("Swoop Rise", typeof(iTweenMoveBy));
//         }
//
//         protected override void InternalReleaseControl() {
//             RestoreAllOutgoingTransitions();
//             
//             RestoreAllActions();
//         }
//
//         protected override void StartQueuedUpdate(byte state, List<byte> variables) {
//             base.StartQueuedUpdate(state, variables);
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
//                 case State.SwoopAntic:
//                     Logger.Get().Info(this, "Received SwoopAntic state");
//                     Fsm.SetState("Swoop Antic");
//                     break;
//                 case State.SummonAntic:
//                     Logger.Get().Info(this, "Received SummonAntic state");
//                     Fsm.SetState("Summon Antic");
//                     break;
//             }
//         }
//
//         private enum State {
//             Unfurl,
//             IntroRoarAntic,
//             SwoopAntic,
//             SwoopRight,
//             SwoopLeft,
//             SummonAntic,
//             RoarRight,
//             RoarLeft,
//         }
//     }
// }