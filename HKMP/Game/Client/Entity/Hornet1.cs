using System;
using System.Collections.Generic;
using HKMP.Networking.Client;
using HKMP.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace HKMP.Game.Client.Entity {
    public class Hornet1 : Entity {
        private static readonly Dictionary<State, string> SimpleEventStates = new Dictionary<State, string> {
            {State.Wake, "Set Scale"},
            {State.Run, "Run Antic"},
            {State.Sphere, "Sphere Antic G"},
            {State.JumpAntic, "Jump Antic"},
            {State.Land, "Land"},
            {State.AirSphere, "Sphere Antic A"},
            {State.GroundDash, "GDash Antic"},
            {State.Evade, "Evade Antic"}
        };

        private static readonly string[] StateUpdateResetNames = {
            // After start and escalation
            "Idle",
            // The run state is immediately reached after run antic
            "Run",
            // After the sphere G sequence
            "Sphere Recover",
            // After the throw sequence
            "Throw Recover",
            // The jump antic state is only entered to start the animation
            "Jump Antic",
            // The jump sequence decides what to do when in the air
            "In Air",
            // After the jump sequence if we land, we end here
            "Land",
            // After the ground dash sequence
            "GDash Recover2",
            // After the evade sequence
            "After Evade"
        };

        public Hornet1(
            NetClient netClient, 
            EntityType entityType, 
            byte entityId, 
            GameObject gameObject
        ) : base(netClient, entityType, entityId, gameObject) {
            Fsm = gameObject.LocateMyFSM("Control");

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
            
            // We insert this method at index 1 to make sure the angle float
            // has been calculated
            Fsm.InsertMethod("Throw Antic", 1, CreateStateUpdateMethod(() => {
                var variables = new List<byte>();

                var angle = Fsm.FsmVariables.GetFsmFloat("Angle").Value;
                variables.AddRange(BitConverter.GetBytes(angle));
                
                Logger.Get().Info(this, $"Sending Throw state with variable: {angle}");
                
                SendStateUpdate((byte) State.ThrowAntic, variables);
            }));
            
            Fsm.InsertMethod("Jump", 0, CreateStateUpdateMethod(() => {
                var variables = new List<byte>();

                var jumpX = Fsm.FsmVariables.GetFsmFloat("Jump X").Value;
                variables.AddRange(BitConverter.GetBytes(jumpX));
                
                Logger.Get().Info(this, $"Sending Jump state with variable: {jumpX}");
                
                SendStateUpdate((byte) State.Jump, variables);
            }));
            
            Fsm.InsertMethod("ADash Antic", 2, CreateStateUpdateMethod(() => {
                var variables = new List<byte>();

                var angle = Fsm.FsmVariables.GetFsmFloat("Angle").Value;
                variables.AddRange(BitConverter.GetBytes(angle));
                
                Logger.Get().Info(this, $"Sending AirDash state with variable: {angle}");
                
                SendStateUpdate((byte) State.AirDash, variables);
            }));
        }

        protected override void InternalTakeControl() {
            foreach (var stateName in StateUpdateResetNames) {
                RemoveOutgoingTransitions(stateName);
            }
            
            // Remove the actions that let the object face a target
            RemoveAction("Sphere Antic G", typeof(FaceObject));
            RemoveAction("Throw Antic", typeof(FaceObject));
            RemoveAction("Jump Antic", typeof(FaceObject));
            RemoveAction("ADash Antic", typeof(FaceObject));
            RemoveAction("Sphere Antic A", typeof(FaceObject));
            RemoveAction("GDash Antic", typeof(FaceObject));
            RemoveAction("Evade Antic", typeof(FaceObject));
            
            // Remove the actions that override variables that we receive
            RemoveAction("ADash Antic", typeof(GetAngleToTarget2D));
            
            RemoveAction("Fire", typeof(FireAtTarget));
        }

        protected override void InternalReleaseControl() {
            RestoreAllOutgoingTransitions();
            
            // Restore the original actions
            RestoreAllActions();
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
                case State.ThrowAntic:
                    if (variableArray.Length == 4) {
                        var angle = BitConverter.ToSingle(variableArray, 0);
                        
                        Logger.Get().Info(this, $"Received Throw with variable: {angle}");

                        Fsm.FsmVariables.GetFsmFloat("Throw Antic").Value = angle;
                    } else {
                        Logger.Get().Info(this, $"Received Throw with incorrect variable array, length: {variableArray.Length}");
                    }

                    Fsm.SetState("Charge Antic");
                    break;
                case State.Jump:
                    if (variableArray.Length == 4) {
                        var jumpX = BitConverter.ToSingle(variableArray, 0);
                        
                        Logger.Get().Info(this, $"Received Jump with variable: {jumpX}");

                        Fsm.FsmVariables.GetFsmFloat("Jump X").Value = jumpX;
                    } else {
                        Logger.Get().Info(this, $"Received Jump with incorrect variable array, length: {variableArray.Length}");
                    }

                    Fsm.SetState("Jump");
                    break;
                case State.AirDash:
                    if (variableArray.Length == 4) {
                        var angle = BitConverter.ToSingle(variableArray, 0);
                        
                        Logger.Get().Info(this, $"Received AirDash with variable: {angle}");

                        Fsm.FsmVariables.GetFsmFloat("Angle").Value = angle;
                    } else {
                        Logger.Get().Info(this, $"Received AirDash with incorrect variable array, length: {variableArray.Length}");
                    }

                    Fsm.SetState("ADash Antic");
                    break;
            }
        }

        protected override bool IsInterruptingState(byte state) {
            return false;
        }

        private enum State {
            Wake = 0,
            Run,
            Sphere,
            ThrowAntic,
            JumpAntic,
            Jump,
            Land,
            AirDash,
            AirSphere,
            GroundDash,
            Evade
        }
    }
}