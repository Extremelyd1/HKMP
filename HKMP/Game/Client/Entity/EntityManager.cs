using System.Collections.Generic;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vector2 = Hkmp.Math.Vector2;

namespace Hkmp.Game.Client.Entity {
    public class EntityManager {
        private readonly NetClient _netClient;

        private readonly Dictionary<(EntityType, byte), IEntity> _entities;

        // Whether entity management is enabled
        private bool _isEnabled;

        // Whether we have received the scene status already (either scene host or scene client)
        private bool _receivedSceneStatus;
        // Whether we are the scene host or scene client
        private bool _isSceneHost;

        // A list of entity updates that have been received from entering the scene
        // These are cached so that they can be accessed if entities enable late
        private List<EntityUpdate> _cachedEntityUpdates;

        public EntityManager(NetClient netClient) {
            _netClient = netClient;
            _entities = new Dictionary<(EntityType, byte), IEntity>();

            ModHooks.Instance.OnEnableEnemyHook += OnEnableEnemyHook;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
        }

        /**
         * Called when we receive the response from entering a scene and we are the scene host
         */
        public void OnEnterSceneAsHost() {
            // We always keep track of whether we are the scene host
            // That way when entity syncing is enabled we know what to do
            _isSceneHost = true;
            _receivedSceneStatus = true;

            if (!_isEnabled) {
                return;
            }

            foreach (var entity in _entities.Values) {
                entity.InitializeAsSceneHost();
            }
        }

        /**
         * Called when we receive the response from entering a scene and we are the scene client
         */
        public void OnEnterSceneAsClient(List<EntityUpdate> entityUpdates) {
            // We always keep track of whether we are the scene host
            // That way when entity syncing is enabled we know what to do
            _isSceneHost = false;
            _receivedSceneStatus = true;

            _cachedEntityUpdates = entityUpdates;

            if (!_isEnabled) {
                return;
            }

            foreach (var entityUpdate in entityUpdates) {
                var entityType = entityUpdate.EntityType;
                var entityId = entityUpdate.Id;

                // Try to find the corresponding local entity for this entity update
                if (!_entities.TryGetValue(((EntityType) entityType, entityId), out var entity)) {
                    // Entity was not found, so we skip initializing it
                    // TODO: perhaps also remove this entity from this client as the host has no registration on it
                    continue;
                }

                // Check whether the entity update contains a state and optionally pass it the initialization method
                var state = new byte?();
                if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.State)) {
                    state = entityUpdate.State;
                }

                entity.InitializeAsSceneClient(state);

                // After that we update the position and scale
                if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Position)) {
                    entity.UpdatePosition(entityUpdate.Position);
                }

                if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Scale)) {
                    entity.UpdateScale(entityUpdate.Scale);
                }
            }
        }

        /**
         * Called when the scene host leaves the scene we are in and as a result, we have become scene host
         */
        public void OnSwitchToSceneHost() {
            _isSceneHost = true;

            if (!_isEnabled) {
                return;
            }

            foreach (var entity in _entities.Values) {
                entity.SwitchToSceneHost();
            }
        }

        /**
         * Called when the setting for entity sync has changed
         */
        public void OnEntitySyncSettingChanged(bool syncEntities) {
            // For now we only keep track of the setting, but it only takes effect once you enter a new scene
            _isEnabled = syncEntities;
        }

        private void OnSceneChanged(Scene oldScene, Scene newScene) {
            Logger.Get().Info(this, "Clearing all registered entities");

            _receivedSceneStatus = false;

            foreach (var entity in _entities.Values) {
                entity.Destroy();
            }

            _entities.Clear();

            ThreadUtil.RunActionOnMainThread(OnSceneChangedCheckBattleGateObjects);
        }

        private void OnSceneChangedCheckBattleGateObjects() {
            var bgObjects = GameObject.FindGameObjectsWithTag("Battle Gate");
            if (bgObjects.Length != 0) {
                Logger.Get().Info(this, $"Found Battle Gate objects, registering them ({bgObjects.Length})");

                for (var i = 0; i < bgObjects.Length; i++) {
                    // Somehow gates with this name have a different FSM, but still share the Battle Gate tag
                    if (bgObjects[i].name.Contains("Battle Gate Prayer")) {
                        continue;
                    }

                    var bgEntity = new BattleGate(_netClient, (byte) i, bgObjects[i]);

                    _entities[(EntityType.BattleGate, (byte) i)] = bgEntity;
                }
            }
        }

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

            Logger.Get().Info(this, $"Registering enabled enemy, type: {entityType}, id: {entityId}");

            _entities[(entityType, entityId)] = entity;
            
            if (!_isEnabled) {
                return isDead;
            }

            // If we have already received the scene status (either scene host or scene client), we still need to
            // initialize this entity probably
            if (_receivedSceneStatus) {
                if (_isSceneHost) {
                    entity.InitializeAsSceneHost();
                } else if (_cachedEntityUpdates != null) {
                    // If we are a scene host and we have a cache of entity updates, we need to find the
                    // entity update that corresponds to this entity and initialize them with the state
                    foreach (var entityUpdate in _cachedEntityUpdates) {
                        if (entityUpdate.EntityType == (byte) entityType && entityUpdate.Id == entityId) {
                            entity.InitializeAsSceneClient(
                                entityUpdate.UpdateTypes.Contains(EntityUpdateType.State)
                                ? entityUpdate.State
                                : new byte?()
                            );
                            
                            // After that we update the position and scale
                            if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Position)) {
                                entity.UpdatePosition(entityUpdate.Position);
                            }

                            if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Scale)) {
                                entity.UpdateScale(entityUpdate.Scale);
                            }

                            break;
                        }
                    }
                }
            }

            return isDead;
        }

        public void UpdateEntityPosition(EntityType entityType, byte id, Vector2 position) {
            if (_isSceneHost) {
                return;
            }

            if (!_entities.TryGetValue((entityType, id), out var entity)) {
                Logger.Get().Info(this,
                    $"Tried to update entity position for (type, ID) = ({entityType}, {id}), but there was no entry");
                return;
            }

            entity.UpdatePosition(position);
        }

        public void UpdateEntityScale(EntityType entityType, byte id, bool scale) {
            if (_isSceneHost) {
                return;
            }

            if (!_entities.TryGetValue((entityType, id), out var entity)) {
                Logger.Get().Info(this,
                    $"Tried to update entity scale for (type, ID) = ({entityType}, {id}), but there was no entry");
                return;
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
                Logger.Get().Info(this,
                    $"Tried to update entity animation for (type, ID) = ({entityType}, {id}), but there was no entry");
                return;
            }

            entity.UpdateAnimation(animationIndex, animationInfo);
        }

        public void UpdateEntityState(
            EntityType entityType,
            byte id,
            byte state
        ) {
            if (_isSceneHost) {
                return;
            }

            if (!_entities.TryGetValue((entityType, id), out var entity)) {
                Logger.Get().Info(this,
                    $"Tried to update entity state for (type, ID) = ({entityType}, {id}), but there was no entry");
                return;
            }

            entity.UpdateState(state);
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

            if (enemyName.Contains("Giant Fly")) {
                entityType = EntityType.GruzMother;

                entityId = GetEnemyId(enemyName.Replace("Giant Fly", ""));


                entity = new GruzMother(_netClient, entityId, gameObject);
                return true;
            }

            if (enemyName.Contains("False Knight New")) {
                entityType = EntityType.FalseKnight;

                entityId = GetEnemyId(enemyName.Replace("False Knight New", ""));

                entity = new FalseKnight(_netClient, entityId, gameObject);
                return true;
            }
            
            if (enemyName.Contains("Mega Moss Charger")) {
                entityType = EntityType.MossCharger;
            
                entityId = GetEnemyId(enemyName.Replace("Mega Moss Charger", ""));
            
                entity = new MassiveMossCharger(_netClient, entityId, gameObject);
            
                return true;
            }
            if (enemyName.Contains("Zombie Runner")) {
                entityType = EntityType.WanderingHusk;

                entityId = GetEnemyId(enemyName.Replace("Zombie Runner", ""));

                entity = new WanderingHusk(_netClient, entityId, gameObject);

                return true;
            };
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