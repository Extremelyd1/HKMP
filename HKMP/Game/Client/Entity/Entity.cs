using System;
using System.Collections.Generic;
using Hkmp.Collection;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using UnityEngine;
using Vector2 = Hkmp.Math.Vector2;

namespace Hkmp.Game.Client.Entity {
    internal class Entity {
        private readonly NetClient _netClient;
        private readonly byte _entityId;

        private readonly GameObject _gameObject;

        private readonly tk2dSpriteAnimator _animator;
        private readonly BiLookup<string, byte> _animationClipNameIds;

        private readonly PlayMakerFSM[] _fsms;

        private readonly Climber _climber;

        private bool _isControlled;

        private Vector3 _lastPosition;
        private Vector3 _lastScale;

        private Vector3 _lastRotation;

        public Entity(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) {
            _netClient = netClient;
            _entityId = entityId;
            _gameObject = gameObject;

            _isControlled = true;

            // Add a position interpolation component to the enemy so we can smooth out position updates
            _gameObject.AddComponent<PositionInterpolation>();

            // Register an update event to send position updates
            MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdate;

            _animator = _gameObject.GetComponent<tk2dSpriteAnimator>();
            if (_animator != null) {
                _animationClipNameIds = new BiLookup<string, byte>();

                var index = 0;
                foreach (var animationClip in _animator.Library.clips) {
                    _animationClipNameIds.Add(animationClip.name, (byte)index++);

                    if (index > byte.MaxValue) {
                        Logger.Get().Error(this,
                            $"Too many animation clips to fit in a byte for entity: {_gameObject.name}");
                        break;
                    }
                }

                On.tk2dSpriteAnimator.Play_tk2dSpriteAnimationClip_float_float += OnAnimationPlayed;
            }

            _climber = _gameObject.GetComponent<Climber>();
            if (_climber != null) {
                MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdateRotation;

                _climber.enabled = false;
            }

            _fsms = _gameObject.GetComponents<PlayMakerFSM>();
            foreach (var fsm in _fsms) {
                fsm.enabled = false;
            }
        }

        private void OnUpdate() {
            // We don't send updates when this entity is controlled
            if (_isControlled) {
                return;
            }

            if (_gameObject == null) {
                return;
            }

            var transform = _gameObject.transform;

            var newPosition = transform.position;
            if (newPosition != _lastPosition) {
                _lastPosition = newPosition;

                _netClient.UpdateManager.UpdateEntityPosition(
                    _entityId,
                    new Vector2(newPosition.x, newPosition.y)
                );
            }

            var newScale = transform.localScale;
            if (newScale != _lastScale) {
                _lastScale = newScale;

                _netClient.UpdateManager.UpdateEntityScale(
                    _entityId,
                    newScale.x > 0
                );
            }
        }

        private void OnAnimationPlayed(
            On.tk2dSpriteAnimator.orig_Play_tk2dSpriteAnimationClip_float_float orig, 
            tk2dSpriteAnimator self, 
            tk2dSpriteAnimationClip clip, 
            float clipStartTime, 
            float overrideFps
        ) {
            orig(self, clip, clipStartTime, overrideFps);

            if (self != _animator) {
                return;
            }
            
            if (_isControlled) {
                return;
            }

            if (!_animationClipNameIds.TryGetValue(clip.name, out var animationId)) {
                Logger.Get().Warn(this, $"Entity '{_gameObject.name}' played unknown animation: {clip.name}");
                return;
            }

            Logger.Get().Info(this, $"Entity '{_gameObject.name}' sends animation: {clip.name}, {animationId}, {clip.wrapMode}");
            _netClient.UpdateManager.UpdateEntityAnimation(
                _entityId,
                animationId,
                (byte) clip.wrapMode
            );
        }

        private void OnUpdateRotation() {
            if (_isControlled) {
                return;
            }

            if (_gameObject == null) {
                return;
            }

            var transform = _gameObject.transform;

            var newRotation = transform.rotation.eulerAngles;
            if (newRotation != _lastRotation) {
                _lastRotation = newRotation;

                var data = new EntityNetworkData {
                    Type = EntityNetworkData.DataType.Rotation
                };
                data.Data.AddRange(BitConverter.GetBytes(newRotation.z));

                _netClient.UpdateManager.AddEntityData(
                    _entityId,
                    data
                );
            }
        }

        public void InitializeHost() {
            if (_climber != null) {
                _climber.enabled = true;
            }

            foreach (var fsm in _fsms) {
                fsm.enabled = true;
            }

            _isControlled = false;
        }

        // TODO: parameters should be all FSM details to kickstart all FSMs of the game object
        public void MakeHost() {
            // TODO: read all variables from the parameters and set the FSM variables of all FSMs
            
            InitializeHost();
        }

        public void UpdatePosition(Vector2 position) {
            var unityPos = new Vector3(position.X, position.Y);

            _gameObject.GetComponent<PositionInterpolation>().SetNewPosition(unityPos);
        }

        public void UpdateScale(bool scale) {
            var transform = _gameObject.transform;
            var localScale = transform.localScale;
            var currentScaleX = localScale.x;

            if (currentScaleX > 0 != scale) {
                transform.localScale = new Vector3(
                    currentScaleX * -1,
                    localScale.y,
                    localScale.z
                );
            }
        }

        public void UpdateAnimation(byte animationId, tk2dSpriteAnimationClip.WrapMode wrapMode, bool alreadyInSceneUpdate) {
            if (_animator == null) {
                Logger.Get().Warn(this,
                    $"Entity '{_gameObject.name}' received animation while animator does not exist");
                return;
            }

            if (!_animationClipNameIds.TryGetValue(animationId, out var clipName)) {
                Logger.Get().Warn(this, $"Entity '{_gameObject.name}' received unknown animation ID: {animationId}");
                return;
            }
            
            Logger.Get().Info(this, $"Entity '{_gameObject.name}' received animation: {animationId}, {clipName}, {wrapMode}");

            if (alreadyInSceneUpdate) {
                // Since this is an animation update from an entity that was already present in a scene,
                // we need to determine where to start playing this specific animation
                if (wrapMode == tk2dSpriteAnimationClip.WrapMode.Loop) {
                    _animator.Play(clipName);
                    return;
                }
                
                var clip = _animator.GetClipByName(clipName);

                if (wrapMode == tk2dSpriteAnimationClip.WrapMode.LoopSection) {
                    // The clip loops in a specific section in the frames, so we start playing
                    // it from the start of that section
                    _animator.PlayFromFrame(clipName, clip.loopStart);
                    return;
                }

                if (wrapMode == tk2dSpriteAnimationClip.WrapMode.Once ||
                    wrapMode == tk2dSpriteAnimationClip.WrapMode.Single) {
                    // Since the clip was played once, it stops on the last frame,
                    // so we emulate that by only "playing" the last frame of the clip
                    var clipLength = clip.frames.Length;
                    _animator.PlayFromFrame(clipName, clipLength - 1);
                    return;
                }
            }
            
            // Otherwise, default to just playing the clip
            _animator.Play(clipName);
        }

        public void UpdateData(List<EntityNetworkData> entityNetworkData) {
            foreach (var data in entityNetworkData) {
                if (data.Type == EntityNetworkData.DataType.Rotation) {
                    var rotation = BitConverter.ToSingle(data.Data.ToArray(), 0);
                    
                    var transform = _gameObject.transform;
                    var eulerAngles = transform.eulerAngles;
                    transform.eulerAngles = new Vector3(
                        eulerAngles.x,
                        eulerAngles.y,
                        rotation
                    );
                }
            }
        }

        public void Destroy() {
            MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdate;
            On.tk2dSpriteAnimator.Play_tk2dSpriteAnimationClip_float_float -= OnAnimationPlayed;
            MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdateRotation;
        }
    }
}