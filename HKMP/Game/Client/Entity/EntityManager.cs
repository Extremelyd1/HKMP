using System.Collections.Generic;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vector2 = Hkmp.Math.Vector2;

namespace Hkmp.Game.Client.Entity {
    internal class EntityManager {
        private readonly Dictionary<string, string> _validEntityFsms = new() {
            { "Crawler", "Crawler" },
            { "chaser", "Buzzer" },
            { "Zombie Swipe", "Zombie Runner" },
            { "Bouncer Control", "Fly" },
            { "BG Control", "Battle Gate" },
            { "spitter", "Spitter" }
        };

        private readonly NetClient _netClient;

        private readonly Dictionary<byte, Entity> _entities;

        private bool _isSceneHost;

        private byte _lastId;

        public EntityManager(NetClient netClient) {
            _netClient = netClient;
            _entities = new Dictionary<byte, Entity>();

            _lastId = 0;

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
        }

        public void InitializeSceneHost() {
            Logger.Get().Info(this, "Releasing control of all registered entities");

            _isSceneHost = true;

            foreach (var entity in _entities.Values) {
                entity.InitializeHost();
            }
        }

        public void InitializeSceneClient() {
            Logger.Get().Info(this, "Taking control of all registered entities");

            _isSceneHost = false;
        }

        public void BecomeSceneHost() {
            Logger.Get().Info(this, "Becoming scene host");

            _isSceneHost = true;

            foreach (var entity in _entities.Values) {
                entity.MakeHost();
            }
        }

        public void UpdateEntityPosition(byte entityId, Vector2 position) {
            if (_isSceneHost) {
                return;
            }

            if (!_entities.TryGetValue(entityId, out var entity)) {
                return;
            }

            entity.UpdatePosition(position);
        }

        public void UpdateEntityScale(byte entityId, bool scale) {
            if (_isSceneHost) {
                return;
            }

            if (!_entities.TryGetValue(entityId, out var entity)) {
                return;
            }

            entity.UpdateScale(scale);
        }

        public void UpdateEntityAnimation(
            byte entityId, 
            byte animationId, 
            byte animationWrapMode,
            bool alreadyInSceneUpdate
        ) {
            if (_isSceneHost) {
                return;
            }

            if (!_entities.TryGetValue(entityId, out var entity)) {
                return;
            }

            entity.UpdateAnimation(animationId, (tk2dSpriteAnimationClip.WrapMode) animationWrapMode, alreadyInSceneUpdate);
        }

        public void UpdateEntityIsActive(byte entityId, bool isActive) {
            if (_isSceneHost) {
                return;
            }

            if (!_entities.TryGetValue(entityId, out var entity)) {
                return;
            }

            entity.UpdateIsActive(isActive);
        }

        public void UpdateEntityData(byte entityId, List<EntityNetworkData> data) {
            if (_isSceneHost) {
                return;
            }

            if (!_entities.TryGetValue(entityId, out var entity)) {
                return;
            }

            entity.UpdateData(data);
        }

        private void OnSceneChanged(Scene oldScene, Scene newScene) {
            Logger.Get().Info(this, "Clearing all registered entities");

            foreach (var entity in _entities.Values) {
                entity.Destroy();
            }

            _entities.Clear();

            _lastId = 0;

            if (!_netClient.IsConnected) {
                return;
            }
            
            // Find all PlayMakerFSM components
            foreach (var fsm in Object.FindObjectsOfType<PlayMakerFSM>()) {
                // Logger.Get().Info(this, $"Found FSM: {fsm.Fsm.Name}, {fsm.gameObject.name}");

                if (!_validEntityFsms.TryGetValue(fsm.Fsm.Name, out var objectName)) {
                    continue;
                }

                var fsmGameObjectName = fsm.gameObject.name;
                if (!fsmGameObjectName.Contains(objectName)) {
                    continue;
                }

                Logger.Get().Info(this, $"Registering entity '{fsmGameObjectName}' with ID '{_lastId}'");
                    
                _entities[_lastId] = new Entity(
                    _netClient,
                    _lastId,
                    fsm.gameObject
                );

                _lastId++;
            }
            
            // Find all Climber components
            foreach (var climber in Object.FindObjectsOfType<Climber>()) {
                Logger.Get().Info(this, $"Registering entity '{climber.name}' with ID '{_lastId}'");

                _entities[_lastId] = new Entity(
                    _netClient,
                    _lastId,
                    climber.gameObject
                );

                _lastId++;
            }
        }
    }
}