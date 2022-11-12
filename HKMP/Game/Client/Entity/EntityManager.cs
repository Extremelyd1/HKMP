using System.Collections.Generic;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vector2 = Hkmp.Math.Vector2;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Entity {
    
    /// <summary>
    /// Manager class that handles entity creation, updating, networking and destruction.
    /// </summary>
    internal class EntityManager {
        /// <summary>
        /// Dictionary that maps all FSM names to game object names for all valid entities.
        /// Valid entities are entities that should be managed by the entity system.
        /// </summary>
        private readonly Dictionary<string, string[]> _validEntityFsms = new() {
            { "Crawler", new [] { "Crawler" } },
            { "chaser", new [] { "Buzzer" } },
            { "Zombie Swipe", new [] { "Zombie Runner", "Zombie Barger", "Zombie Hornhead" } },
            { "Bouncer Control", new [] { "Fly" } },
            { "BG Control", new [] { "Battle Gate" } },
            { "spitter", new [] { "Spitter" } },
            { "Zombie Guard", new [] { "Zombie Guard" } },
            { "Zombie Leap", new [] { "Zombie Leaper" } },
            { "Hatcher", new [] { "Hatcher" } },
            { "Control", new [] { "Hatcher Baby Spawner" } },
            { "ZombieShieldControl", new [] { "Zombie Shield" } },
            { "Worm Control", new [] { "Worm" } }
        };

        /// <summary>
        /// The net client for networking.
        /// </summary>
        private readonly NetClient _netClient;

        /// <summary>
        /// Dictionary mapping entity IDs to their respective entity instances.
        /// </summary>
        private readonly Dictionary<byte, Entity> _entities;

        /// <summary>
        /// Whether the client user is the scene host.
        /// </summary>
        private bool _isSceneHost;

        /// <summary>
        /// The last used ID of an entity.
        /// </summary>
        private byte _lastId;

        public EntityManager(NetClient netClient) {
            _netClient = netClient;
            _entities = new Dictionary<byte, Entity>();

            _lastId = 0;

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
        }

        /// <summary>
        /// Initializes the entity manager if we are the scene host.
        /// </summary>
        public void InitializeSceneHost() {
            Logger.Info("Releasing control of all registered entities");

            _isSceneHost = true;

            foreach (var entity in _entities.Values) {
                entity.InitializeHost();
            }
        }

        /// <summary>
        /// Initializes the entity manager if we are a scene client.
        /// </summary>
        public void InitializeSceneClient() {
            Logger.Info("Taking control of all registered entities");

            _isSceneHost = false;
        }

        /// <summary>
        /// Updates the entity manager if we become the scene host.
        /// </summary>
        public void BecomeSceneHost() {
            Logger.Info("Becoming scene host");

            _isSceneHost = true;

            foreach (var entity in _entities.Values) {
                entity.MakeHost();
            }
        }

        /// <summary>
        /// Update the position for the entity with the given ID.
        /// </summary>
        /// <param name="entityId">The entity ID.</param>
        /// <param name="position">The new position.</param>
        public void UpdateEntityPosition(byte entityId, Vector2 position) {
            if (_isSceneHost) {
                return;
            }

            if (!_entities.TryGetValue(entityId, out var entity)) {
                return;
            }

            entity.UpdatePosition(position);
        }

        /// <summary>
        /// Update the scale for the entity with the given ID.
        /// </summary>
        /// <param name="entityId">The entity ID.</param>
        /// <param name="scale">The new scale.</param>
        public void UpdateEntityScale(byte entityId, bool scale) {
            if (_isSceneHost) {
                return;
            }

            if (!_entities.TryGetValue(entityId, out var entity)) {
                return;
            }

            entity.UpdateScale(scale);
        }

        /// <summary>
        /// Update the animation for the entity with the given ID.
        /// </summary>
        /// <param name="entityId">The entity ID.</param>
        /// <param name="animationId">The ID of the animation.</param>
        /// <param name="animationWrapMode">The wrap mode of the animation.</param>
        /// <param name="alreadyInSceneUpdate">Whether this update is when we are entering the scene.</param>
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

        /// <summary>
        /// Update whether the entity with the given ID is active.
        /// </summary>
        /// <param name="entityId">The entity ID.</param>
        /// <param name="isActive">The new value for active.</param>        
        public void UpdateEntityIsActive(byte entityId, bool isActive) {
            if (_isSceneHost) {
                return;
            }

            if (!_entities.TryGetValue(entityId, out var entity)) {
                return;
            }

            entity.UpdateIsActive(isActive);
        }

        /// <summary>
        /// Update the entity with the given ID with the given generic data.
        /// </summary>
        /// <param name="entityId">The ID of the entity.</param>
        /// <param name="data">The list of data to update the entity with.</param>
        public void UpdateEntityData(byte entityId, List<EntityNetworkData> data) {
            if (_isSceneHost) {
                return;
            }

            if (!_entities.TryGetValue(entityId, out var entity)) {
                return;
            }

            entity.UpdateData(data);
        }

        /// <summary>
        /// Callback method for when the scene changes.
        /// </summary>
        /// <param name="oldScene">The old scene.</param>
        /// <param name="newScene">The new scene.</param>
        private void OnSceneChanged(Scene oldScene, Scene newScene) {
            Logger.Info("Clearing all registered entities");

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
                if (fsm.gameObject.scene != newScene) {
                    continue;
                }
                
                Logger.Info($"Found FSM: {fsm.Fsm.Name}, {fsm.gameObject.name}");

                if (!_validEntityFsms.TryGetValue(fsm.Fsm.Name, out var validObjNames)) {
                    continue;
                }

                var fsmGameObjName = fsm.gameObject.name;
                var hasValidObjName = false;
                foreach (var validObjName in validObjNames) {
                    if (fsmGameObjName.Contains(validObjName)) {
                        hasValidObjName = true;
                        break;
                    }
                }

                if (!hasValidObjName) {
                    continue;
                }

                Logger.Info($"Registering entity '{fsmGameObjName}' with ID '{_lastId}'");
                    
                _entities[_lastId] = new Entity(
                    _netClient,
                    _lastId,
                    fsm.gameObject
                );

                _lastId++;
            }
            
            // Find all Climber components
            foreach (var climber in Object.FindObjectsOfType<Climber>()) {
                Logger.Info($"Registering entity '{climber.name}' with ID '{_lastId}'");

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