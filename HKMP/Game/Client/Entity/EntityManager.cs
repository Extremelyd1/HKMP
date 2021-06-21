using System.Collections.Generic;
using HKMP.Networking.Client;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vector2 = HKMP.Math.Vector2;

namespace HKMP.Game.Client.Entity {
    public class EntityManager {
        private readonly NetClient _netClient;

        private readonly Dictionary<(EntityType, byte), IEntity> _entities;

        // Whether entity management is enabled
        private bool _isEnabled;

        private bool _isSceneHost;

        public EntityManager(NetClient netClient) {
            _netClient = netClient;
            _entities = new Dictionary<(EntityType, byte), IEntity>();
            
            ModHooks.Instance.OnEnableEnemyHook += OnEnableEnemyHook;
            
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
        }

        public void OnBecomeSceneHost() {
            // We always keep track of whether we are the scene host
            // That way when entity syncing is enabled we know what to do
            _isSceneHost = true;
            
            if (!_isEnabled) {
                return;
            }
            
            Logger.Get().Info(this, "Scene host: releasing control of all registered entities");

            foreach (var entity in _entities.Values) {
                if (entity.IsControlled) {
                    entity.ReleaseControl();
                }
                
                entity.AllowEventSending = true;
            }
        }

        public void OnBecomeSceneClient() {
            // We always keep track of whether we are the scene host
            // That way when entity syncing is enabled we know what to do
            _isSceneHost = false;
            
            if (!_isEnabled) {
                return;
            }
            
            Logger.Get().Info(this, "Scene client: taking control of all registered entities");

            foreach (var entity in _entities.Values) {
                if (!entity.IsControlled) {
                    entity.TakeControl();
                }

                entity.AllowEventSending = false;
            }
        }

        public void OnEntitySyncSettingChanged(bool syncEntities) {
            if (syncEntities == _isEnabled) {
                return;
            }

            _isEnabled = syncEntities;
            
            if (syncEntities) {
                // Based on whether we are scene host, we execute the respective method to
                // manage existing entities
                if (_isSceneHost) {
                    OnBecomeSceneHost();
                } else {
                    OnBecomeSceneClient();
                }
            } else {
                Logger.Get().Info(this, "Entity sync disabled, releasing control of all registered entities");

                foreach (var entity in _entities.Values) {
                    if (entity.IsControlled) {
                        entity.ReleaseControl();
                    }
                
                    entity.AllowEventSending = true;
                }
            }
        }
        
        private void OnSceneChanged(Scene oldScene, Scene newScene) {
            Logger.Get().Info(this, "Clearing all registered entities");
            
            foreach (var entity in _entities.Values) {
                entity.Destroy();
            }
            
            _entities.Clear();
        }

        private bool OnEnableEnemyHook(GameObject enemy, bool isDead) {
            var enemyName = enemy.name;

            if (!InstantiateEntity(
                enemyName,
                enemy,
                out var entityType,
                out var entity,
                out var enemyId
            )) {
                return isDead;
            }

            Logger.Get().Info(this, $"Registering enabled enemy, name: {enemyName}, id: {enemyId}");
            
            _entities[(entityType, enemyId)] = entity;

            if (!_netClient.IsConnected || !_isEnabled) {
                return isDead;
            }
            
            if (_isSceneHost) {
                Logger.Get().Info(this, "Releasing control of registered enemy");
                
                if (entity.IsControlled) {
                    entity.ReleaseControl();
                }
                
                entity.AllowEventSending = true;
            } else {
                Logger.Get().Info(this, "Taking control of registered enemy");
                
                if (!entity.IsControlled) {
                    entity.TakeControl();
                }
                
                entity.AllowEventSending = false;
            }
            
            return isDead;
        }

        public void UpdateEntityPosition(EntityType entityType, byte id, Vector2 position) {
            if (!_entities.TryGetValue((entityType, id), out var entity)) {
                Logger.Get().Info(this, $"Tried to update entity position for (type, ID) = ({entityType}, {id}), but there was no entry");
                return;
            }

            if (_isSceneHost) {
                return;
            }
            
            // Check whether the entity is already controlled, and if not
            // take control of it
            if (!entity.IsControlled) {
                entity.TakeControl();
            }

            entity.UpdatePosition(position);
        }

        public void UpdateEntityScale(EntityType entityType, byte id, bool scale) {
            if (!_entities.TryGetValue((entityType, id), out var entity)) {
                Logger.Get().Info(this, $"Tried to update entity scale for (type, ID) = ({entityType}, {id}), but there was no entry");
                return;
            }

            if (_isSceneHost) {
                return;
            }
            
            // Check whether the entity is already controlled, and if not
            // take control of it
            if (!entity.IsControlled) {
                entity.TakeControl();
            }

            entity.UpdateScale(scale);
        }

        public void UpdateEntityState(EntityType entityType, byte id, byte stateIndex, List<byte> variables) {
            if (!_entities.TryGetValue((entityType, id), out var entity)) {
                Logger.Get().Info(this, $"Tried to update entity state for (type, ID) = ({entityType}, {id}), but there was no entry");
                return;
            }

            if (_isSceneHost) {
                return;
            }

            // Check whether the entity is already controlled, and if not
            // take control of it
            if (!entity.IsControlled) {
                entity.TakeControl();
            }

            // Simply update the state with this new index
            entity.UpdateState(stateIndex, variables);
        }

        private bool InstantiateEntity(
            string enemyName,
            GameObject gameObject,
            out EntityType entityType,
            out IEntity entity,
            out byte enemyId
        ) {
            entityType = EntityType.None;
            entity = null;
            enemyId = 0;

            if (enemyName.Contains("False Knight New")) {
                entityType = EntityType.FalseKnight;

                enemyId = GetEnemyId(enemyName.Replace("False Knight New", ""));
                
                entity = new FalseKnight(_netClient, enemyId, gameObject);
                return true;
            }
            
            if (enemyName.Contains("Giant Fly")) {
                entityType = EntityType.GruzMother;

                enemyId = GetEnemyId(enemyName.Replace("Giant Fly", ""));


                entity = new GruzMother(_netClient, enemyId, gameObject);
                return true;
            }

            if (enemyName.Contains("Hornet Boss 1")) {
                entityType = EntityType.Hornet1;

                enemyId = GetEnemyId(enemyName.Replace("Hornet Boss 1", ""));

                entity = new Hornet1(_netClient, enemyId, gameObject);

                return true;
            }

            if (enemyName.Contains("Zombie Runner")) {
                entityType = EntityType.ZombieRunner;
                
                enemyId = GetEnemyId(enemyName.Replace("Zombie Runner", ""));

                entity = new ZombieRunner(_netClient, enemyId, gameObject);

                return true;
            }

            return false;
        }

        private byte GetEnemyId(string leftoverObjectName) {
            var nameSplit = leftoverObjectName.Split(' ');
            var enemyIndexString = nameSplit[nameSplit.Length - 1];

            byte.TryParse(enemyIndexString, out var enemyId);

            return enemyId;
        }
    }
}