using System.Collections.Generic;
using Hkmp.Networking.Client;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vector2 = Hkmp.Math.Vector2;

namespace Hkmp.Game.Client.Entity {
    public class EntityManager {
        private readonly NetClient _netClient;

        private readonly Dictionary<(EntityType, byte), IEntity> _entities;

        private readonly Dictionary<(EntityType, byte), Vector2> _cachedPosition;
        private readonly Dictionary<(EntityType, byte), bool> _cachedScale;
        private readonly Dictionary<(EntityType, byte), (byte, byte[])> _cachedAnimation;
        private readonly Dictionary<(EntityType, byte), byte> _cachedState;

        // Whether entity management is enabled
        private bool _isEnabled;

        private bool _isSceneHost;

        public EntityManager(NetClient netClient) {
            _netClient = netClient;
            _entities = new Dictionary<(EntityType, byte), IEntity>();

            _cachedPosition = new Dictionary<(EntityType, byte), Vector2>();
            _cachedScale = new Dictionary<(EntityType, byte), bool>();
            _cachedAnimation = new Dictionary<(EntityType, byte), (byte, byte[])>();
            _cachedState = new Dictionary<(EntityType, byte), byte>();
            
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

            // ThreadUtil.RunActionOnMainThread(OnSceneChangedCheckBattleGateObjects);
        }

        // private void OnSceneChangedCheckBattleGateObjects() {
        //     var bgObjects = GameObject.FindGameObjectsWithTag("Battle Gate");
        //     if (bgObjects.Length != 0) {
        //         Logger.Get().Info(this, $"Found Battle Gate objects, registering them ({bgObjects.Length})");
        //
        //         for (var i = 0; i < bgObjects.Length; i++) {
        //             // Somehow gates with this name have a different FSM, but still share the Battle Gate tag
        //             if (bgObjects[i].name.Contains("Battle Gate Prayer")) {
        //                 continue;
        //             }
        //                 
        //             var bgEntity = new BattleGate(_netClient, (byte) i, bgObjects[i]);
        //
        //             RegisterNewEntity(bgEntity, EntityType.BattleGate, (byte) i);
        //         }
        //     }
        // }

        private bool OnEnableEnemyHook(GameObject enemy, bool isDead) {
            var enemyName = enemy.name;
            
            Logger.Get().Info(this, $"OnEnableEnemyHook, name: {enemyName}");

            if (!InstantiateEntity(
                enemyName,
                enemy,
                out var entityType,
                out var entity,
                out var entityId
            )) {
                return isDead;
            }

            RegisterNewEntity(entity, entityType, entityId);

            return isDead;
        }

        private void RegisterNewEntity(IEntity entity, EntityType entityType, byte entityId) {
            Logger.Get().Info(this, $"Registering enabled enemy, type: {entityType}, id: {entityId}");
            
            _entities[(entityType, entityId)] = entity;

            if (!_netClient.IsConnected || !_isEnabled) {
                return;
            }

            if (_isSceneHost) {
                Logger.Get().Info(this, "  Player is scene host, relaying to entity");

                entity.AllowEventSending = true;
                
                entity.SendInitialState();
            } else {
                Logger.Get().Info(this, "  Player is scene client, taking control of entity");

                if (!entity.IsControlled) {
                    entity.TakeControl();
                }

                entity.AllowEventSending = false;

                if (_cachedPosition.TryGetValue((entityType, entityId), out var position)) {
                    Logger.Get().Info(this, $"  Retroactively updating position of entity: {entityType}, {entityId}");
                    
                    entity.UpdatePosition(position);
                    _cachedPosition.Remove((entityType, entityId));
                }
                
                if (_cachedScale.TryGetValue((entityType, entityId), out var scale)) {
                    Logger.Get().Info(this, $"  Retroactively updating scale of entity: {entityType}, {entityId}");
                    
                    entity.UpdateScale(scale);
                    _cachedScale.Remove((entityType, entityId));
                }

                if (_cachedAnimation.TryGetValue((entityType, entityId), out var animation)) {
                    Logger.Get().Info(this, $"  Retroactively updating animation of entity: {entityType}, {entityId}");

                    entity.UpdateAnimation(animation.Item1, animation.Item2);
                    _cachedAnimation.Remove((entityType, entityId));
                }

                if (_cachedState.TryGetValue((entityType, entityId), out var state)) {
                    Logger.Get().Info(this, $"  Retroactively updating state of entity: {entityType}, {entityId}");

                    entity.InitializeWithState(state);
                    _cachedState.Remove((entityType, entityId));
                }
            }
        }

        public void UpdateEntityPosition(EntityType entityType, byte id, Vector2 position) {
            if (_isSceneHost) {
                return;
            }
            
            if (!_entities.TryGetValue((entityType, id), out var entity)) {
                // Logger.Get().Info(this,
                //     $"Tried to update entity position for (type, ID) = ({entityType}, {id}), but there was no entry");

                _cachedPosition[(entityType, id)] = position;
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
            if (_isSceneHost) {
                return;
            }
            
            if (!_entities.TryGetValue((entityType, id), out var entity)) {
                // Logger.Get().Info(this, $"Tried to update entity scale for (type, ID) = ({entityType}, {id}), but there was no entry");

                _cachedScale[(entityType, id)] = scale;
                return;
            }

            // Check whether the entity is already controlled, and if not
            // take control of it
            if (!entity.IsControlled) {
                entity.TakeControl();
            }

            entity.UpdateScale(scale);
        }

        public void UpdateEntityAnimation(
            EntityType entityType, 
            byte id, 
            byte animationIndex,
            byte[] animationInfo
        ) {
            if (_isSceneHost) {
                return;
            }
            
            if (!_entities.TryGetValue((entityType, id), out var entity)) {
                // Logger.Get().Info(this, $"Tried to update entity state for (type, ID) = ({entityType}, {id}), but there was no entry");

                _cachedAnimation[(entityType, id)] = (animationIndex, animationInfo);
                return;
            }

            // Check whether the entity is already controlled, and if not
            // take control of it
            if (!entity.IsControlled) {
                entity.TakeControl();
            }

            // Simply update the state with this new index
            entity.UpdateAnimation(animationIndex, animationInfo);
        }

        public void InitializeEntityWithState(
            EntityType entityType,
            byte id,
            byte state
        ) {
            if (_isSceneHost) {
                return;
            }
            
            if (!_entities.TryGetValue((entityType, id), out var entity)) {
                _cachedState[(entityType, id)] = state;
                return;
            }

            if (!entity.IsControlled) {
                entity.TakeControl();
            }

            entity.InitializeWithState(state);
        }

        private bool InstantiateEntity(
            string enemyName,
            GameObject gameObject,
            out EntityType entityType,
            out IEntity entity,
            out byte entityId
        ) {
            entityType = EntityType.None;
            entity = null;
            entityId = 0;

            // if (enemyName.Contains("False Knight New")) {
            //     entityType = EntityType.FalseKnight;
            //
            //     entityId = GetEnemyId(enemyName.Replace("False Knight New", ""));
            //     
            //     entity = new FalseKnight(_netClient, entityId, gameObject);
            //     return true;
            // }
            //
            if (enemyName.Contains("Giant Fly")) {
                entityType = EntityType.GruzMother;
            
                entityId = GetEnemyId(enemyName.Replace("Giant Fly", ""));
            
            
                entity = new GruzMother(_netClient, entityId, gameObject);
                return true;
            }
            //
            // if (enemyName.Contains("Hornet Boss 1")) {
            //     entityType = EntityType.Hornet1;
            //
            //     entityId = GetEnemyId(enemyName.Replace("Hornet Boss 1", ""));
            //
            //     entity = new Hornet1(_netClient, entityId, gameObject);
            //
            //     return true;
            // }
            //
            // if (enemyName.Contains("Mega Moss Charger")) {
            //     entityType = EntityType.MossCharger;
            //
            //     entityId = GetEnemyId(enemyName.Replace("Mega Moss Charger", ""));
            //
            //     entity = new MossCharger(_netClient, entityId, gameObject);
            //
            //     return true;
            // }
            //
            // // The colosseum variant has a different name, so we check the larger substring first
            // if (enemyName.Contains("Giant Buzzer Col")) {
            //     entityType = EntityType.VengeflyKing;
            //     
            //     entityId = GetEnemyId(enemyName.Replace("Giant Buzzer Col", ""));
            //
            //     entity = new VengeflyKing(_netClient, entityId, gameObject, true);
            //
            //     return true;
            // }
            //
            // if (enemyName.Contains("Giant Buzzer")) {
            //     entityType = EntityType.VengeflyKing;
            //
            //     entityId = GetEnemyId(enemyName.Replace("Giant Buzzer", ""));
            //
            //     entity = new VengeflyKing(_netClient, entityId, gameObject, false);
            //
            //     return true;
            // }
            
            return false;
        }

        private byte GetEnemyId(string leftoverObjectName) {
            var nameSplit = leftoverObjectName.Split(' ');
            if (nameSplit.Length == 0) {
                return 0;
            }
            
            var enemyIndexString = nameSplit[nameSplit.Length - 1];
            // Remove brackets from the string to account for format such as "EnemyName (1)"
            enemyIndexString = enemyIndexString.Replace("(", "").Replace(")", "");

            byte.TryParse(enemyIndexString, out var enemyId);

            return enemyId;
        }
    }
}