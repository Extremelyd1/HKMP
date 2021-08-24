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
            _fsm.InsertMethod("Wake Sound", 0, CreateStateUpdateMethod(() => {
                // Send the wake animation with a zero byte to indicate that it is not the Godhome variant
                SendAnimationUpdate((byte) Animation.Wake, new List<byte> {0});
                
                SendStateUpdate((byte) State.Active);
            }));

            _fsm.InsertMethod("GG Boss Wake", 0, CreateStateUpdateMethod(() => {
                // Send the wake animation with a one byte to indicate that it is the Godhome variant
                SendAnimationUpdate((byte) Animation.Wake, new List<byte> {1});
            }));

            _fsm.InsertMethod("Fly", 0, CreateStateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.Fly); }));

            _fsm.InsertMethod("Super End", 0,
                CreateStateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.Buzz); }));

            _fsm.InsertMethod("Charge Antic", 1,
                CreateStateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.ChargeAntic); }));

            _fsm.InsertMethod("Charge", 0,
                CreateStateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.Charge); }));

            _fsm.InsertMethod("Charge Recover L", 0,
                CreateStateUpdateMethod(() => {
                    SendAnimationUpdate((byte) Animation.ChargeRecover, new List<byte> {0});
                }));
            _fsm.InsertMethod("Charge Recover U", 0,
                CreateStateUpdateMethod(() => {
                    SendAnimationUpdate((byte) Animation.ChargeRecover, new List<byte> {1});
                }));
            _fsm.InsertMethod("Charge Recover R", 0,
                CreateStateUpdateMethod(() => {
                    SendAnimationUpdate((byte) Animation.ChargeRecover, new List<byte> {2});
                }));
            _fsm.InsertMethod("Charge Recover D", 0,
                CreateStateUpdateMethod(() => {
                    SendAnimationUpdate((byte) Animation.ChargeRecover, new List<byte> {3});
                }));

            _fsm.InsertMethod("Slam Antic", 0,
                CreateStateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.ChargeAntic); }));

            _fsm.InsertMethod("Launch Up", 0,
                CreateStateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.Launch); }));
            _fsm.InsertMethod("Launch Down", 0,
                CreateStateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.Launch); }));

            _fsm.InsertMethod("Slam Down", 0,
                CreateStateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.SlamDown); }));

            _fsm.InsertMethod("Slam Up", 0,
                CreateStateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.SlamUp); }));

            _fsm.InsertMethod("Slam End", 0,
                CreateStateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.SlamEnd); }));
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
                
                var state = (State) stateIndex;
                
                Logger.Get().Info(this, $"Initializing with state: {state}");
                
                if (state == State.Active) {
                    _fsm.GetAction<DestroyObject>("Wake", 4).Execute();
                    _fsm.GetAction<SendEventByName>("Wake", 6).Execute();
                
                    _fsm.GetAction<Tk2dPlayAnimation>("Fly", 2).Execute();
                    _fsm.GetAction<ActivateGameObject>("Fly", 5).Execute();
                    _fsm.GetAction<TransitionToAudioSnapshot>("Fly", 7).Execute();
                    _fsm.GetAction<ApplyMusicCue>("Fly", 8).Execute();
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
                    _fsm.GetAction<AudioPlayerOneShotSingle>("Wake Sound", 1).Execute();
                }

                _fsm.GetAction<SetGameObject>("Wake", 0).Execute();
                _fsm.GetAction<ActivateGameObject>("Wake", 1).Execute();
                
                _fsm.GetAction<SetFsmBool>("Wake", 2).Execute();
                _fsm.GetAction<SetFsmString>("Wake", 3).Execute();
                
                _fsm.GetAction<DestroyObject>("Wake", 4).Execute();
                
                _fsm.GetAction<SendEventByName>("Wake", 5).Execute();
                _fsm.GetAction<SendEventByName>("Wake", 6).Execute();
                
                _fsm.GetAction<Tk2dPlayAnimation>("Wake", 7).Execute();
            }

            if (animation == Animation.Fly) {
                _fsm.GetAction<Tk2dPlayAnimation>("Fly", 2).Execute();

                MonoBehaviourUtil.Instance.StartCoroutine(PlayFlyAnimation());
            }

            if (animation == Animation.Buzz) {
                _fsm.GetAction<AudioPlay>("Buzz", 0).Execute();
                _fsm.GetAction<SetAudioClip>("Buzz", 1).Execute();
                
                _fsm.GetAction<SendEventByName>("Buzz", 2).Execute();
            }

            if (animation == Animation.ChargeAntic) {
                _fsm.GetAction<AudioStop>("Charge Antic", 4).Execute();
                
                _fsm.GetAction<Tk2dPlayAnimation>("Charge Antic", 5).Execute();
            }

            if (animation == Animation.Charge) {
                _fsm.GetAction<Tk2dPlayAnimation>("Charge", 1).Execute();
                
                _fsm.GetAction<SetAudioClip>("Charge", 2).Execute();
                _fsm.GetAction<AudioPlay>("Charge", 3).Execute();
            }

            if (animation == Animation.ChargeRecover) {
                _fsm.GetAction<AudioStop>("Charge Recover L", 1).Execute();
                _fsm.GetAction<AudioPlayerOneShotSingle>("Charge Recover L", 2).Execute();

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
                    _fsm.GetAction<CreateObject>(stateName, createObjectActionIndex).Execute();
                    _fsm.GetAction<CreateObject>(stateName, 4).Execute();
                    
                    _fsm.GetAction<SpawnRandomObjects>(stateName, 5).Execute();
                }

                _fsm.GetAction<SendEventByName>("Charge Recover L", 6).Execute();
                
                _fsm.GetAction<Tk2dPlayAnimation>("Charge Recover L", 7).Execute();
            }

            if (animation == Animation.Launch) {
                _fsm.GetAction<Tk2dPlayAnimation>("Launch Up", 3).Execute();
            }

            if (animation == Animation.SlamDown) {
                _fsm.GetAction<AudioPlayerOneShotSingle>("Slam Down", 1).Execute();
                
                _fsm.GetAction<Tk2dPlayAnimation>("Slam Down", 3).Execute();
                
                _fsm.GetAction<CreateObject>("Slam Down", 4).Execute();
                _fsm.GetAction<CreateObject>("Slam Down", 5).Execute();
                
                _fsm.GetAction<SpawnRandomObjects>("Slam Down", 7).Execute();
                
                _fsm.GetAction<SendEventByName>("Slam Down", 8).Execute();
            }
            
            if (animation == Animation.SlamUp) {
                _fsm.GetAction<AudioPlayerOneShotSingle>("Slam Up", 1).Execute();
                
                _fsm.GetAction<Tk2dPlayAnimation>("Slam Up", 3).Execute();
                
                _fsm.GetAction<CreateObject>("Slam Up", 4).Execute();
                _fsm.GetAction<CreateObject>("Slam Up", 5).Execute();
                
                _fsm.GetAction<SpawnRandomObjects>("Slam Up", 6).Execute();
                
                _fsm.GetAction<SendEventByName>("Slam Up", 7).Execute();
            }
            
            if (animation == Animation.SlamEnd) {
                _fsm.GetAction<Tk2dPlayAnimation>("Slam End", 2).Execute();
            }
        }

        public override void UpdateState(byte state) {
        }

        private IEnumerator PlayFlyAnimation() {
            yield return new WaitForSeconds(1f);

            _fsm.GetAction<ActivateGameObject>("Fly", 5).Execute();
            
            _fsm.GetAction<TransitionToAudioSnapshot>("Fly", 7).Execute();
         
            _fsm.GetAction<ApplyMusicCue>("Fly", 8).Execute();
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