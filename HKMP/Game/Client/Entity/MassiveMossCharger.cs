using System.Collections;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Game.Client.Entity {
    public class MassiveMossCharger : HealthManagedEntity {
        private readonly PlayMakerFSM _fsm;

        private Animation _lastAnimation;

        private bool _submergeFromRoar;
        
        public MassiveMossCharger(
            NetClient netClient, 
            byte entityId, 
            GameObject gameObject
        ) : base(netClient, EntityType.MossCharger, entityId, gameObject) {
            _fsm = gameObject.LocateMyFSM("Mossy Control");
            
            CreateAnimationEvents();
        }

        private void CreateAnimationEvents() {
            _fsm.InsertMethod("Shake", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.Shake);
                
                SendStateUpdate((byte) State.Active);
            }));
            
            _fsm.InsertMethod("Roar End", 0, CreateUpdateMethod(() => {
                _submergeFromRoar = true;
            }));
            
            _fsm.InsertMethod("Submerge 1", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.Submerge, (byte) (_submergeFromRoar ? 0 : 1));

                _submergeFromRoar = false;

                SendStateUpdate((byte) State.Hidden);
            }));
            
            _fsm.InsertMethod("Emerge", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.Emerge);
            }));
            
            _fsm.InsertMethod("Leap Start", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.Leap);
            }));
            
            _fsm.InsertMethod("Land", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.Land);
            }));
            
            _fsm.InsertMethod("Init", 29, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.InitSubmerge);
                
                SendStateUpdate((byte) State.Hidden);
            }));
        }

        protected override void InternalInitializeAsSceneHost() {
            var activeStateName = _fsm.ActiveStateName;

            switch (activeStateName) {
                case "Dormant":
                case "Start Pause":
                case "Init":
                case "Sleep":
                case "GG Pause":
                    SendStateUpdate((byte) State.Asleep);
                    break;
                case "Shake":
                case "Title?":
                case "Music":
                case "Roar":
                case "Leap Start":
                case "Leap Launch":
                case "In Air":
                case "Land":
                case "Emerge":
                case "Hit Right":
                case "Hit Left":
                case "Charge":
                    SendStateUpdate((byte) State.Active);
                    break;
                default:
                    SendStateUpdate((byte) State.Hidden);
                    break;
            }
        }

        protected override void InternalInitializeAsSceneClient(byte? stateIndex) {
            RemoveAllTransitions(_fsm);
            
            if (stateIndex.HasValue) {
                var state = (State) stateIndex.Value;

                if (state != State.Asleep) {
                    ExecuteMusicActions();
                }

                if (state == State.Active) {
                    _fsm.ExecuteActions("Emerge", 4, 12);
                }

                if (state == State.Hidden) {
                    _fsm.ExecuteActions("Submerge CD", 0, 4, 5);
                }
            }
        }

        protected override void InternalSwitchToSceneHost() {
            RestoreAllTransitions(_fsm);

            switch (_lastAnimation) {
                case Animation.Shake:
                    _fsm.SetState("Roar End");
                    break;
                case Animation.Submerge:
                    _fsm.SetState("Submerge CD");
                    break;
                case Animation.Emerge:
                    _fsm.SetState("Charge");
                    break;
                case Animation.Leap:
                    _fsm.SetState("In Air");
                    break;
                default:
                    _fsm.SetState("Hidden");
                    break;
            }
        }

        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo) {
            base.UpdateAnimation(animationIndex, animationInfo);

            var animation = (Animation) animationIndex;

            _lastAnimation = animation;
            
            Logger.Get().Info(this, $"Received animation: {animation}");

            if (animation == Animation.Shake) {
                MonoBehaviourUtil.Instance.StartCoroutine(PlayShakeAnimation());
            }

            if (animation == Animation.Submerge) {
                var fromRoarEnd = animationInfo[0] == 0;

                if (fromRoarEnd) {
                    _fsm.ExecuteActions("Roar End", 1, 2, 3);
                }

                MonoBehaviourUtil.Instance.StartCoroutine(PlaySubmergeAnimation());
            }

            if (animation == Animation.Emerge) {
                _fsm.ExecuteActions("Emerge", 1, 2, 4, 5, 8, 9, 11, 12, 14);
                
                _fsm.GetAction<Tk2dPlayAnimationWithEvents>("Emerge", 13).Execute(() => {
                    _fsm.ExecuteActions("Charge", 0, 1, 2);
                });
            }

            if (animation == Animation.Leap) {
                _fsm.ExecuteActions("Leap Start", 1, 2, 3, 4, 8, 9);
                
                _fsm.GetAction<Tk2dPlayAnimationWithEvents>("Leap Start", 5).Execute(() => {
                    _fsm.ExecuteActions("Leap Launch", 0, 1);
                });
            }

            if (animation == Animation.Land) {
                _fsm.ExecuteActions("Land", 3, 4, 5, 6);
            }

            if (animation == Animation.InitSubmerge) {
                _fsm.ExecuteActions("Hidden", 2, 3);
            }
        }

        public override void UpdateState(byte state) {
        }

        private IEnumerator PlayShakeAnimation() {
            _fsm.ExecuteActions("Shake", 1, 2, 3, 4, 5, 6);

            yield return new WaitForSeconds(3f);

            if (BossSceneController.IsBossScene) {
                _fsm.ExecuteActions("Title?", 1, 2, 3, 4, 5);
            }
            
            ExecuteMusicActions();
            
            _fsm.ExecuteActions("Roar", 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        private void ExecuteMusicActions() {
            if (!BossSequenceController.IsInSequence) {
                _fsm.ExecuteActions("Music", 1);

                if (BossSceneController.IsBossScene) {
                    _fsm.ExecuteActions("Music", 3);
                }
            }
        }

        private IEnumerator PlaySubmergeAnimation() {
            var spriteAnimator = GameObject.GetComponent<tk2dSpriteAnimator>();
        
            _fsm.ExecuteActions("Submerge 1", 1, 2, 5, 6);
            spriteAnimator.Play("Disappear 1");

            yield return new WaitForSeconds(0.1666667f);
            
            _fsm.ExecuteActions("Submerge 2", 2, 3);
            spriteAnimator.Play("Disappear 2");

            yield return new WaitForSeconds(0.3333333f);
            
            _fsm.ExecuteActions("Submerge 3", 2, 3, 4);
            spriteAnimator.Play("Disappear 3");

            yield return new WaitForSeconds(0.1666667f);
            
            _fsm.ExecuteActions("Submerge 4", 2);
            spriteAnimator.Play("Disappear 4");

            yield return new WaitForSeconds(0.3333333f);
            
            _fsm.ExecuteActions("Submerge CD", 0, 1, 2, 4, 5);

            yield return new WaitForSeconds(0.75f);
            
            _fsm.ExecuteActions("Play Range", 0);
        }

        private enum State {
            Asleep = 0,
            Hidden,
            Active
        }

        private enum Animation {
            Shake = 0,
            Submerge,
            Emerge,
            Leap,
            Land,
            InitSubmerge
        }
    }
}