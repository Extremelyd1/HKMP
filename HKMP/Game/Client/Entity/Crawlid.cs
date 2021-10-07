using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hkmp.Game.Client.Entity {
    public class Crawlid : HealthManagedEntity {
        private readonly PlayMakerFSM _fsm;

        private readonly Dictionary<int, string> _animationIds;

        private readonly tk2dSpriteAnimator _animator;

        private readonly string _defaultState;

        public Crawlid(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.Crawlid, entityId, gameObject) {
            _fsm = gameObject.LocateMyFSM("Crawler");
            _animator = gameObject.GetComponent<tk2dSpriteAnimator>();
            // Get the initial scale of the object on enter. This is used to determine which way the sprite it facing. 
            _animationIds = new Dictionary<int, string>();
            _defaultState = "Wait";

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
        }
        protected override void InternalInitializeAsSceneClient(byte? stateIndex) {
            RemoveAllTransitions(_fsm);
            _fsm.SetState(_defaultState);
            _animator.Stop();
        }
        protected override void InternalSwitchToSceneHost() {
            RestoreAllTransitions(_fsm);
            _fsm.SetState("Walk");
        }
        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo) {
            base.UpdateAnimation(animationIndex, animationInfo);

            // Check if the animation is _strictly_ an animation
            if (animationIndex >= Enum.GetNames(typeof(Animation)).Length && animationIndex != 255) {
                // We must stop the previous animation in order to play the new one. 
                _animator.Stop();
                _animator.Play(_animationIds[animationIndex]);
            }
        }

        public override void UpdateState(byte stateIndex) {
        }
        public void Log(string s) {
            Logger.Get().Info(this, s);
        }
        public int GetAnimationId(string animationName) => _animationIds.FirstOrDefault(x => x.Value == animationName).Key;
        private enum Animation {
        }
    }
}