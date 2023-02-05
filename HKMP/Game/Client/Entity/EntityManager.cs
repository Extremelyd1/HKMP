using System.Collections.Generic;
using Hkmp.Networking.Client;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Hkmp.Logging.Logger;
using Vector2 = Hkmp.Math.Vector2;

namespace Hkmp.Game.Client.Entity;

internal class EntityManager {
    private readonly NetClient _netClient;

    private readonly Dictionary<(EntityType, byte), IEntity> _entities;

    private bool _isSceneHost;

    public EntityManager(NetClient netClient) {
        _netClient = netClient;
        _entities = new Dictionary<(EntityType, byte), IEntity>();

        // ModHooks.Instance.OnEnableEnemyHook += OnEnableEnemyHook;

        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
    }

    public void OnBecomeSceneHost() {
        Logger.Info("Releasing control of all registered entities");

        _isSceneHost = true;

        foreach (var entity in _entities.Values) {
            if (entity.IsControlled) {
                entity.ReleaseControl();
            }

            entity.AllowEventSending = true;
        }
    }

    public void OnBecomeSceneClient() {
        Logger.Info("Taking control of all registered entities");

        _isSceneHost = false;

        foreach (var entity in _entities.Values) {
            if (!entity.IsControlled) {
                entity.TakeControl();
            }

            entity.AllowEventSending = false;
        }
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene) {
        Logger.Info("Clearing all registered entities");

        foreach (var entity in _entities.Values) {
            entity.Destroy();
        }

        _entities.Clear();
    }

    private bool OnEnableEnemyHook(GameObject enemy, bool isDead) {
        var enemyName = enemy.name;

        IEntity entity = null;

        if (enemyName.StartsWith("False Knight New")) {
            var trimmedName = enemyName.Replace("False Knight New", "").Trim();

            byte enemyId;
            if (trimmedName.Length == 0) {
                enemyId = 0;
            } else {
                if (!byte.TryParse(trimmedName, out enemyId)) {
                    Logger.Info($"Could not parse enemy index as byte ({enemyName})");

                    return isDead;
                }
            }

            Logger.Info($"Registering enabled enemy, name: {enemyName}, id: {enemyId}");

            entity = new FalseKnight(_netClient, enemyId, enemy);

            _entities[(EntityType.FalseKnight, enemyId)] = entity;
        }

        if (entity == null) {
            return isDead;
        }

        if (_isSceneHost) {
            Logger.Info("Releasing control of registered enemy");

            if (entity.IsControlled) {
                entity.ReleaseControl();
            }

            entity.AllowEventSending = true;
        } else {
            Logger.Info("Taking control of registered enemy");

            if (!entity.IsControlled) {
                entity.TakeControl();
            }

            entity.AllowEventSending = false;
        }

        return isDead;
    }

    public void UpdateEntityPosition(EntityType entityType, byte id, Vector2 position) {
        if (!_entities.TryGetValue((entityType, id), out var entity)) {
            Logger.Info(
                $"Tried to update entity position for (type, ID) = ({entityType}, {id}), but there was no entry");
            return;
        }

        // Check whether the entity is already controlled, and if not
        // take control of it
        if (!entity.IsControlled) {
            entity.TakeControl();
        }

        entity.UpdatePosition(position);
    }

    public void UpdateEntityState(EntityType entityType, byte id, byte stateIndex, List<byte> variables) {
        if (!_entities.TryGetValue((entityType, id), out var entity)) {
            Logger.Info(
                $"Tried to update entity state for (type, ID) = ({entityType}, {id}), but there was no entry");
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
}
