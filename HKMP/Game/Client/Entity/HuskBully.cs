using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity {
    public class HuskBully : HealthManagedEntity {
        private readonly PlayMakerFSM _fsm;

        private Animation _lastAnimation;

        private readonly Walker _walker;

        private readonly AudioSource _audioSource;

        private readonly HutongGames.PlayMaker.Actions.WalkLeftRight a;


        public HuskBully(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.HuskBully, entityId, gameObject) {
            _fsm = gameObject.LocateMyFSM("Zombie Swipe");
            _walker = gameObject.GetComponent<Walker>();
            _audioSource = gameObject.GetComponent<AudioSource>();
            CreateAnimationEvents();
            var walkerAnimator = gameObject.GetComponent<Walker>().GetComponent<tk2dSpriteAnimator>();


            // Some animations are not controlled by the FSM. Hence we must make all animations in the entity's Walker component to trigger `AnimationEventTriggered` to send state updates. 
            if (walkerAnimator != null) {
                foreach (var clip in walkerAnimator.Library.clips) {
                    // Skip clips with no frames
                    if (clip.frames.Length == 0) {
                        continue;
                    }
                    var firstFrame = clip.frames[0];
                    // Enable event triggering on the first frame
                    firstFrame.triggerEvent = true;
                    // Also include the clip name as event info
                    firstFrame.eventInfo = clip.name;
                }
            }
            else {
                Logger.Get().Warn(this, "Walker animator not found");
            }
            // Making each animation send an update
            walkerAnimator.AnimationEventTriggered = (caller, currentClip, currentFrame) => {
                if (currentClip.name == "Idle") {
                    SendAnimationUpdate((byte)Animation.Idle);
                }
                else if (currentClip.name == "Walk") {
                    SendAnimationUpdate((byte)Animation.Walk);
                }
                else if (currentClip.name == "Turn") {
                    SendAnimationUpdate((byte)Animation.Turn);
                }
            };
        }

        private void CreateAnimationEvents() {
            _fsm.InsertMethod("Anticipate", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Anticipate); }));
            _fsm.InsertMethod("Lunge", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Lunge); }));
            _fsm.InsertMethod("Cooldown", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Cooldown); }));
            _fsm.InsertMethod("Idle", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Idle); }));
        }
        protected override void InternalInitializeAsSceneHost() {
            SendStateUpdate((byte)State.Active);
            _walker.enabled = true;
        }
        protected override void InternalInitializeAsSceneClient(byte? stateIndex) {
            RemoveAllTransitions(_fsm);
            _walker.Stop(Walker.StopReasons.Bored); // Stops enemy from 'drifting' when entering the room. 
            _walker.enabled = false;
        }
        protected override void InternalSwitchToSceneHost() {
            RestoreAllTransitions(_fsm);
            _walker.enabled = true;
            // Put the FSM back into the correct state based on the last state. 
            switch (_lastAnimation) {
                case Animation.Walk:
                    _fsm.SetState("Idle");
                    break;
                case Animation.Anticipate:
                    _fsm.SetState("Lunge");
                    break;
                case Animation.Cooldown:
                    _fsm.SetState("Idle");
                    break;
                case Animation.Lunge:
                    _fsm.SetState("Cooldown");
                    break;
            }

        }

        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo) {
            base.UpdateAnimation(animationIndex, animationInfo);

            var animation = (Animation)animationIndex;

            _lastAnimation = animation;

            if (animation == Animation.Anticipate) {
                _fsm.ExecuteActions("Anticipate", 2, 3, 5);
            }

            if (animation == Animation.Lunge) {
                _fsm.ExecuteActions("Lunge", 1, 2);
            }

            if (animation == Animation.Cooldown) {
                _fsm.ExecuteActions("Cooldown", 1, 2);
            }

            if (animation == Animation.Idle) {
                // This animation is not controlled by the FSM. It must be started manually from the entity's `Walker` `SpriteAnimator`
                _walker.GetComponent<tk2dSpriteAnimator>().Play("Idle");
                _audioSource.Stop();
            }
            if (animation == Animation.Walk) {
                _audioSource.Play();
                _walker.GetComponent<tk2dSpriteAnimator>().Play("Walk");
            }
            if (animation == Animation.Turn) {
                _walker.GetComponent<tk2dSpriteAnimator>().Play("Turn");
            }
        }

        public override void UpdateState(byte stateIndex) {
        }
        private enum State {
            Active = 0,
        }

        private enum Animation {
            Anticipate = 0,
            Lunge,
            Cooldown,
            Idle,
            Walk,
            Turn,
        }
    }
}