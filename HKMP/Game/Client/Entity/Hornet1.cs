using System;
using System.Collections.Generic;
using Hkmp.Networking.Client;
using Hkmp.Util;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hkmp.Game.Client.Entity {
    public class Hornet1 : HealthManagedEntity {
        private static readonly Dictionary<State, string> SimpleEventStates = new Dictionary<State, string> {
            {State.Wake, "Set Scale"},
            {State.Run, "Run Antic"},
            {State.Sphere, "Sphere Antic G"},
            {State.JumpAntic, "Jump Antic"},
            {State.Land, "Land"},
            {State.AirSphere, "Sphere Antic A"},
            {State.GroundDash, "GDash Antic"},
            {State.Stun, "Stun Start"},
            {State.Idle, "Idle"}
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
            "After Evade",
            // After the stun sequence
            "Stun Recover"
        };

        private readonly PlayMakerFSM _stunControlFsm;
        private readonly FsmStateAction[] _stunStateActions;

        private readonly PlayMakerFSM _encounterFsm;

        private bool _isInitialized;

        public Hornet1(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.Hornet1, entityId, gameObject) {
            Fsm = gameObject.LocateMyFSM("Control");

            _stunControlFsm = gameObject.LocateMyFSM("Stun Control");
            _stunStateActions = _stunControlFsm.GetState("Stun").Actions;

            // This object does not exists in god home fights
            var encounterObject = GameObject.Find("Hornet Infected Knight Encounter");
            if (encounterObject != null) {
                _encounterFsm = encounterObject.LocateMyFSM("Encounter");
                //
                // Logger.Get().Info(this, "Test 1");
                // var methodCallAction = _encounterFsm.GetAction<CallMethodProper>("Dialogue", 0);
                // Logger.Get().Info(this, "Test 2");
                // var ownerDefaultTarget = _encounterFsm.Fsm.GetOwnerDefaultTarget(methodCallAction.gameObject);
                // Logger.Get().Info(this, "Test 3");
                // var dialogueBox = ownerDefaultTarget.GetComponent<DialogueBox>();
                //
                //
            }

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

            // We insert this method at index 2 to make sure that it is only sent when
            // we passed the bool test
            Fsm.InsertMethod("Evade Antic", 2, CreateStateUpdateMethod(() => {
                Logger.Get().Info(this, $"Sending Evade state");

                SendStateUpdate((byte) State.Evade);
            }));
            
            if (_encounterFsm != null) {
                // We insert a method in the encounter FSM to make sure that the dialogue box is closed when the host is
                // done
                _encounterFsm.InsertMethod("Box Down", 0, CreateStateUpdateMethod(() => {
                    Logger.Get().Info(this, "Sending Box Down state");

                    SendStateUpdate((byte) State.BoxDown);
                }));
                
                // Insert method that resets the state update after we reach the end of the encounter FSM
                _encounterFsm.InsertMethod("Start Fight", _encounterFsm.GetState("Start Fight").Actions.Length,
                    StateUpdateDone);
            }

            //
            // Insert methods for resetting the update state, so we can start/receive the next update
            //
            foreach (var stateName in StateUpdateResetNames) {
                var state = Fsm.GetState(stateName);

                Fsm.InsertMethod(stateName, state.Actions.Length, StateUpdateDone);
            }
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
            RemoveAction("Stun Start", typeof(FaceObject));
            RemoveAction("Idle", typeof(FaceObject));

            // Remove the actions that override variables that we receive
            RemoveAction("ADash Antic", typeof(GetAngleToTarget2D));
            RemoveAction("Throw Antic", typeof(GetAngleToTarget2D));

            // Remove the actions that immediately transition out of a state
            RemoveAction("ADash Antic", typeof(BoolTest));
            RemoveAction("Evade Antic", typeof(BoolTest));
            RemoveAction("Run", typeof(BoolTest));

            RemoveAction("Fire", typeof(FireAtTarget));

            // Remove the actions in the state that calls the global stun event
            _stunControlFsm.GetState("Stun").Actions = new FsmStateAction[0];
        }

        protected override void InternalReleaseControl() {
            RestoreAllOutgoingTransitions();

            // Restore the original actions
            RestoreAllActions();

            // Restore the actions in the stun state
            _stunControlFsm.GetState("Stun").Actions = _stunStateActions;
        }

        protected override void StartQueuedUpdate(byte state, List<byte> variables) {
            if (!_isInitialized) {
                Initialize();
                _isInitialized = true;
            }

            base.StartQueuedUpdate(state, variables);

            var variableArray = variables.ToArray();

            var enumState = (State) state;

            if (SimpleEventStates.TryGetValue(enumState, out var stateName)) {
                Logger.Get().Info(this, $"Received {enumState} state");
                Fsm.SetState(stateName);

                return;
            }

            switch (enumState) {
                case State.Evade:
                    Fsm.SetState("Evade Antic");

                    break;
                case State.ThrowAntic:
                    if (variableArray.Length == 4) {
                        var angle = BitConverter.ToSingle(variableArray, 0);

                        Logger.Get().Info(this, $"Received Throw with variable: {angle}");

                        Fsm.FsmVariables.GetFsmFloat("Angle").Value = angle;
                    } else {
                        Logger.Get().Info(this,
                            $"Received Throw with incorrect variable array, length: {variableArray.Length}");
                    }

                    Fsm.SetState("Throw Antic");
                    break;
                case State.Jump:
                    if (variableArray.Length == 4) {
                        var jumpX = BitConverter.ToSingle(variableArray, 0);

                        Logger.Get().Info(this, $"Received Jump with variable: {jumpX}");

                        Fsm.FsmVariables.GetFsmFloat("Jump X").Value = jumpX;
                    } else {
                        Logger.Get().Info(this,
                            $"Received Jump with incorrect variable array, length: {variableArray.Length}");
                    }

                    Fsm.SetState("Jump");
                    break;
                case State.AirDash:
                    if (variableArray.Length == 4) {
                        var angle = BitConverter.ToSingle(variableArray, 0);

                        Logger.Get().Info(this, $"Received AirDash with variable: {angle}");

                        Fsm.FsmVariables.GetFsmFloat("Angle").Value = angle;
                    } else {
                        Logger.Get().Info(this,
                            $"Received AirDash with incorrect variable array, length: {variableArray.Length}");
                    }

                    Fsm.SetState("ADash Antic");
                    break;
                case State.BoxDown:
                    // Find all instances of DialogueBox
                    var dialogueBoxes = Object.FindObjectsOfType<DialogueBox>();
                    // The second instance should be the non-prompting one (so no yes/no dialogue)
                    var dialoguePageControl = dialogueBoxes[1].gameObject.LocateMyFSM("Dialogue Page Control");
                    // By sending the HERO DAMAGED event, the dialogue box will cancel
                    dialoguePageControl.SendRemoteFsmEvent("HERO DAMAGED");
                    
                    // We need to manually advance this FSM since it didn't receive the proper event
                    _encounterFsm.SetState("Box Down");
                    
                    // And for some reason the player doesn't get control back, so we manually call that as well
                    HeroController.instance.RegainControl();
                    
                    break;
            }
        }

        protected override bool IsInterruptingState(byte state) {
            // The Stun state is the only interrupting state
            return base.IsInterruptingState(state) || state == (byte) State.Stun;
        }

        private void Initialize() {
            // Most of these properties are set in the Wake state
            var rigidbody = GameObject.GetComponent<Rigidbody2D>();
            rigidbody.isKinematic = false;

            GameObject.GetComponent<MeshRenderer>().enabled = true;

            var boxCollider = GameObject.GetComponent<BoxCollider2D>();
            boxCollider.enabled = true;

            // These vectors are from the FSM variables
            boxCollider.size = new Vector2(0.8946984f, 2.564674f);
            boxCollider.offset = new Vector2(0.1200523f, -0.2645378f);

            // The health manager and damage hero components have their values adjusted in the GG sequence
            var healthManager = GameObject.GetComponent<HealthManager>();
            healthManager.IsInvincible = false;
            healthManager.InvincibleFromDirection = 0;

            GameObject.GetComponent<DamageHero>().damageDealt = 1;

            if (_encounterFsm != null) {
                // TODO: deal with conversation box popping up
                _encounterFsm.SetState("Start Fight");
            }

            // TODO: Possibly we also need to start the music
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
            Evade,
            Stun,
            Idle,

            // Encounter FSM states
            BoxDown
        }
    }
}