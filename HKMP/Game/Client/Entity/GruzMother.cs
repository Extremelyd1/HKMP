using System.Collections;
using System.Collections.Generic;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Game.Client.Entity {
    public class GruzMother : HealthManagedEntity {
        private readonly PlayMakerFSM _fsm;
        private readonly PlayMakerFSM _bouncerFsm;

        private Animation _lastAnimation;

        public GruzMother(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.GruzMother, entityId, gameObject) {
            _fsm = gameObject.LocateMyFSM("Big Fly Control");
            _bouncerFsm = gameObject.LocateMyFSM("bouncer_control");

            CreateAnimationEvents();
        }

        private void CreateAnimationEvents() {
            _fsm.InsertMethod("Wake Sound", 0, CreateUpdateMethod(() => {
                // Send the wake animation with a zero byte to indicate that it is not the Godhome variant
                SendAnimationUpdate((byte) Animation.Wake, new List<byte> {0});
                
                SendStateUpdate((byte) State.Active);
            }));

            _fsm.InsertMethod("GG Boss Wake", 0, CreateUpdateMethod(() => {
                // Send the wake animation with a one byte to indicate that it is the Godhome variant
                SendAnimationUpdate((byte) Animation.Wake, new List<byte> {1});
            }));

            _fsm.InsertMethod("Fly", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.Fly); }));

            _fsm.InsertMethod("Super End", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.Buzz); }));

            _fsm.InsertMethod("Charge Antic", 1,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.ChargeAntic); }));

            _fsm.InsertMethod("Charge", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.Charge); }));

            _fsm.InsertMethod("Charge Recover L", 0,
                CreateUpdateMethod(() => {
                    SendAnimationUpdate((byte) Animation.ChargeRecover, new List<byte> {0});
                }));
            _fsm.InsertMethod("Charge Recover U", 0,
                CreateUpdateMethod(() => {
                    SendAnimationUpdate((byte) Animation.ChargeRecover, new List<byte> {1});
                }));
            _fsm.InsertMethod("Charge Recover R", 0,
                CreateUpdateMethod(() => {
                    SendAnimationUpdate((byte) Animation.ChargeRecover, new List<byte> {2});
                }));
            _fsm.InsertMethod("Charge Recover D", 0,
                CreateUpdateMethod(() => {
                    SendAnimationUpdate((byte) Animation.ChargeRecover, new List<byte> {3});
                }));

            _fsm.InsertMethod("Slam Antic", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.ChargeAntic); }));

            _fsm.InsertMethod("Launch Up", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.Launch); }));
            _fsm.InsertMethod("Launch Down", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.Launch); }));

            _fsm.InsertMethod("Slam Down", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.SlamDown); }));

            _fsm.InsertMethod("Slam Up", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.SlamUp); }));

            _fsm.InsertMethod("Slam End", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.SlamEnd); }));
        }

        protected override void InternalInitializeAsSceneHost() {
            SendStateUpdate((byte) State.Asleep);
        }

        protected override void InternalInitializeAsSceneClient(byte? stateIndex) {
            RemoveAllTransitions(_fsm);
            RemoveAllTransitions(_bouncerFsm);

            if (stateIndex.HasValue) {
                var healthManager = GameObject.GetComponent<HealthManager>();
                healthManager.IsInvincible = false;
                healthManager.InvincibleFromDirection = 0;
                
                var state = (State) stateIndex.Value;
                
                Logger.Get().Info(this, $"Initializing with state: {state}");
                
                if (state == State.Active) {
                    _fsm.ExecuteActions("Wake", 4, 6);
                
                    _fsm.ExecuteActions("Fly", 2, 5, 7, 8);
                }
            }
        }

        protected override void InternalSwitchToSceneHost() {
            // We first restore all transitions and then we set the state of the main FSM
            RestoreAllTransitions(_fsm);
            RestoreAllTransitions(_bouncerFsm);
            
            // Based on the last animation we received, we can put the FSM back in a proper state
            switch (_lastAnimation) {
                case Animation.Wake:
                    _fsm.SetState("Fly");
                    break;
                case Animation.Fly: 
                    _fsm.SetState("Buzz");
                    break;
                case Animation.Buzz:
                    _fsm.SetState("Super Choose");
                    break;
                case Animation.ChargeAntic:
                case Animation.Charge:
                    _fsm.SetState("Charge");
                    break;
                case Animation.ChargeRecover:
                    _fsm.SetState("Recover End");
                    break;
                case Animation.Launch:
                case Animation.SlamDown:
                case Animation.SlamUp:
                    _fsm.SetState("Check Direction");
                    break;
                case Animation.SlamEnd:
                    _fsm.SetState("Super End");
                    break;
            }
        }

        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo) {
            base.UpdateAnimation(animationIndex, animationInfo);
            
            var animation = (Animation) animationIndex;

            _lastAnimation = animation;
            
            Logger.Get().Info(this, $"Received animation: {animation}");

            if (animation == Animation.Wake) {
                var wakeType = animationInfo[0];

                if (wakeType == 0) {
                    // This is the non-godhome wake
                    _fsm.ExecuteActions("Wake Sound", 1);
                }

                _fsm.ExecuteActions("Wake", 0, 1, 2, 3, 4, 5, 6, 7);
            }

            if (animation == Animation.Fly) {
                _fsm.ExecuteActions("Fly", 2);

                MonoBehaviourUtil.Instance.StartCoroutine(PlayFlyAnimation());
            }

            if (animation == Animation.Buzz) {
                _fsm.ExecuteActions("Buzz", 0, 1, 2);
            }

            if (animation == Animation.ChargeAntic) {
                _fsm.ExecuteActions("Charge Antic", 4, 5);
            }

            if (animation == Animation.Charge) {
                _fsm.ExecuteActions("Charge", 1, 2, 3);
            }

            if (animation == Animation.ChargeRecover) {
                _fsm.ExecuteActions("Charge Recover L", 1, 2);

                var recoverDir = animationInfo[0];

                var createObjectActionIndex = 2;

                string stateName = null;

                if (recoverDir == 0) {
                    createObjectActionIndex = 3;
                    stateName = "Charge Recover L";
                } else if (recoverDir == 1) {
                    stateName = "Charge Recover U";
                } else if (recoverDir == 2) {
                    stateName = "Charge Recover R";
                } else if (recoverDir == 3) {
                    stateName = "Charge Recover D";
                }

                if (stateName != null) {
                    _fsm.ExecuteActions(stateName, createObjectActionIndex, 4, 5);
                }

                _fsm.ExecuteActions("Charge Recover L", 6, 7);
            }

            if (animation == Animation.Launch) {
                _fsm.ExecuteActions("Launch Up", 3);
            }

            if (animation == Animation.SlamDown) {
                _fsm.ExecuteActions("Slam Down", 1, 3, 4, 5, 7, 8);
            }
            
            if (animation == Animation.SlamUp) {
                _fsm.ExecuteActions("Slam Up", 1, 3, 4, 5, 6, 7);
            }
            
            if (animation == Animation.SlamEnd) {
                _fsm.ExecuteActions("Slam End", 2);
            }
        }

        public override void UpdateState(byte stateIndex) {
        }

        private IEnumerator PlayFlyAnimation() {
            yield return new WaitForSeconds(1f);

            _fsm.ExecuteActions("Fly", 5, 7, 8);
        }

        private enum State {
            Asleep = 0,
            Active
        }

        private enum Animation {
            Wake = 0,
            Fly,
            Buzz,
            ChargeAntic,
            Charge,
            ChargeRecover,
            Launch,
            SlamDown,
            SlamUp,
            SlamEnd
        }
    }
}