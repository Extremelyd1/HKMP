using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hkmp.Game.Client.Entity {
    public class Vengefly : HealthManagedEntity {
        private readonly PlayMakerFSM _fsm;

        private string _lastAnimation;

        private readonly Dictionary<int, string> _animationIds;

        private readonly tk2dSpriteAnimator _animator;

        public Vengefly(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.Vengefly, entityId, gameObject) {
            _fsm = gameObject.LocateMyFSM("chaser");
            _animator = gameObject.GetComponent<tk2dSpriteAnimator>();
            _animationIds = new Dictionary<int, string>();

            CreateAnimationEvents();
        }

        private void CreateAnimationEvents() {
            // Some animations are not controlled by the FSM. Hence we must make all animations in the entity's Walker component to trigger `AnimationEventTriggered` to send state updates. 
            if (_animator != null) {
                foreach (var clip in _animator.Library.clips) {
                    // Add animation to dictionary
                    _animationIds.Add(_animationIds.Count + Enum.GetNames(typeof(Animation)).Length, clip.name);
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
                Logger.Get().Warn(this, "Animator not found");
            }
            // Making each animation send an update
            _animator.AnimationEventTriggered = (caller, currentClip, currentFrame) => {
                if (IsHostEntity) {
                    SendAnimationUpdate((byte)GetAnimationId(currentClip.name));
                }
            };
        }
        protected override void InternalInitializeAsSceneHost() {
            SendStateUpdate((byte)State.Active);
        }
        protected override void InternalInitializeAsSceneClient(byte? stateIndex) {
            RemoveAllTransitions(_fsm);
            _fsm.SetState("Initiate");
            _animator.Stop();
        }
        protected override void InternalSwitchToSceneHost() {
            RestoreAllTransitions(_fsm);
            // Put the FSM back into the correct state based on the last state. 
            switch (_lastAnimation) {
                case "Idle":
                case "TurnToIdle":
                    _fsm.SetState("Idle");
                    break;
                case "TurnToFly":
                case "Startle":
                    _fsm.SetState("Chase Start");
                    break;
                default:
                    _fsm.SetState("Idle");
                    break;
            }

        }
        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo) {
            base.UpdateAnimation(animationIndex, animationInfo);

            // Check if the animation is _strictly_ an animation
            if (animationIndex >= Enum.GetNames(typeof(Animation)).Length && animationIndex != 255) {
                var animationName = _animationIds[animationIndex];
                // We must stop the previous animation in order to play the new one. 
                _animator.Stop();
                _animator.Play(_animationIds[animationIndex]);
                _lastAnimation = animationName;

                if(animationName == "Startle") {
                    // Play the audio associated with this animation
                    _fsm.ExecuteActions("Startle", 0);
                    _fsm.ExecuteActions("Chase Start", 0);
                }
                else if(animationName == "Idle") {
                    _fsm.ExecuteActions("Stop", 0);
                }
            }
        }

        public override void UpdateState(byte stateIndex) {
        }

        public int GetAnimationId(string animationName) => _animationIds.FirstOrDefault(x => x.Value == animationName).Key;
        private enum State {
            Active = 0,
        }

        private enum Animation {
            Idle,
            Startle,
            Stop,
            ChaseStart,
            IdleTurn,
            ChaseTurn,
        }
    }
}