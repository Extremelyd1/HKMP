using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Entity.Component;

// TODO: make sure that the data sent on death is saved as state on the server, so new clients entering
// scenes can start with the entity disabled/already dead
// TODO: periodically (or on hit) sync the health of the entity so on scene host transfer we can reset health
/// <inheritdoc />
/// This component manages the <see cref="HealthManager"/> component of the entity.
internal class HealthManagerComponent : EntityComponent {
    /// <summary>
    /// Host-client pair of health manager components of the entity.
    /// </summary>
    private readonly HostClientPair<HealthManager> _healthManager;

    /// <summary>
    /// Boolean indicating whether the health manager of the client entity is allowed to die.
    /// </summary>
    private bool _allowDeath;

    /// <summary>
    /// The last value for the "invincible" variable of the health manager.
    /// </summary>
    private bool _lastInvincible;

    /// <summary>
    /// The last value for the "invincibleFromDirection" variable of the health manager.
    /// </summary>
    private int _lastInvincibleFromDirection;

    public HealthManagerComponent(
        NetClient netClient,
        ushort entityId,
        HostClientPair<GameObject> gameObject,
        HostClientPair<HealthManager> healthManager
    ) : base(netClient, entityId, gameObject) {
        _healthManager = healthManager;

        _lastInvincible = healthManager.Host.IsInvincible;
        _lastInvincibleFromDirection = healthManager.Host.InvincibleFromDirection;

        On.HealthManager.Die += HealthManagerOnDie;
        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdate;
    }

    /// <summary>
    /// Callback method for when the health manager dies.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The health manager instance.</param>
    /// <param name="attackDirection">The direction of the attack that caused the death.</param>
    /// <param name="attackType">The type of attack that caused the death.</param>
    /// <param name="ignoreEvasion">Whether to ignore evasion.</param>
    private void HealthManagerOnDie(
        On.HealthManager.orig_Die orig,
        HealthManager self,
        float? attackDirection,
        AttackTypes attackType,
        bool ignoreEvasion
    ) {
        if (self != _healthManager.Host && self != _healthManager.Client) {
            orig(self, attackDirection, attackType, ignoreEvasion);
            return;
        }

        if (self == _healthManager.Client) {
            if (!_allowDeath) {
                Logger.Info("HealthManager Die was called on client entity");
            } else {
                Logger.Info("HealthManager Die was called on client entity, but it is allowed death");

                orig(self, attackDirection, attackType, ignoreEvasion);

                _allowDeath = false;
            }

            return;
        }

        Logger.Info("HealthManager Die was called on host entity");

        orig(self, attackDirection, attackType, ignoreEvasion);

        var data = new EntityNetworkData {
            Type = EntityComponentType.Death
        };

        if (attackDirection.HasValue) {
            data.Packet.Write(true);
            data.Packet.Write(attackDirection.Value);
        } else {
            data.Packet.Write(false);
        }

        data.Packet.Write((byte)attackType);

        data.Packet.Write(ignoreEvasion);

        SendData(data);
    }

    /// <summary>
    /// Callback method for updates to check whether invincibility changes.
    /// </summary>
    private void OnUpdate() {
        var data = new EntityNetworkData {
            Type = EntityComponentType.Invincibility
        };

        var shouldSend = false;
        
        var newInvincible = _healthManager.Host.IsInvincible;
        if (newInvincible != _lastInvincible) {
            _lastInvincible = newInvincible;
            shouldSend = true;
        }
        data.Packet.Write(newInvincible);

        var newInvincibleFromDir = _healthManager.Host.InvincibleFromDirection;
        if (newInvincibleFromDir != _lastInvincibleFromDirection) {
            _lastInvincibleFromDirection = newInvincibleFromDir;
            shouldSend = true;
        }
        data.Packet.Write((byte) newInvincibleFromDir);

        if (shouldSend) {
            SendData(data);
        }
    }
    
    /// <inheritdoc />
    public override void InitializeHost() {
    }

    /// <inheritdoc />
    public override void Update(EntityNetworkData data) {
        Logger.Info("Received health manager update");

        if (!IsControlled) {
            Logger.Info("  Entity was not controlled");
            return;
        }

        if (data.Type == EntityComponentType.Death) {
            var attackDirection = new float?();
            if (data.Packet.ReadBool()) {
                attackDirection = data.Packet.ReadFloat();
            }

            var attackType = (AttackTypes) data.Packet.ReadByte();
            var ignoreEvasion = data.Packet.ReadBool();

            // Set a boolean to indicate that the client health manager is allowed to execute the Die method
            _allowDeath = true;
            _healthManager.Client.Die(attackDirection, attackType, ignoreEvasion);
        } else if (data.Type == EntityComponentType.Invincibility) {
            var newInvincible = data.Packet.ReadBool();
            var newInvincibleFromDir = data.Packet.ReadByte();

            _healthManager.Host.IsInvincible = newInvincible;
            _healthManager.Host.InvincibleFromDirection = newInvincibleFromDir;
            _healthManager.Client.IsInvincible = newInvincible;
            _healthManager.Client.InvincibleFromDirection = newInvincibleFromDir;
        }
    }

    /// <inheritdoc />
    public override void Destroy() {
        On.HealthManager.Die -= HealthManagerOnDie;
        MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdate;
    }
}