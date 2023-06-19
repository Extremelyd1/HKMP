using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity.Component;

/// <inheritdoc />
/// This component manages the damage that an entity deals to the player.
internal class DamageHeroComponent : EntityComponent {
    /// <summary>
    /// The host-client pair of <see cref="DamageHero"/> unity components of the entity.
    /// </summary>
    private readonly HostClientPair<DamageHero> _damageHero;

    /// <summary>
    /// The last value of damage dealt for the damage hero.
    /// </summary>
    private int _lastDamageDealt;

    public DamageHeroComponent(
        NetClient netClient,
        byte entityId,
        HostClientPair<GameObject> gameObject,
        HostClientPair<DamageHero> damageHero
    ) : base(netClient, entityId, gameObject) {
        _damageHero = damageHero;
        _lastDamageDealt = damageHero.Host.damageDealt;

        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdate;
    }

    /// <summary>
    /// Callback method to check for damage hero updates.
    /// </summary>
    private void OnUpdate() {
        if (IsControlled) {
            return;
        }

        if (GameObject.Host == null) {
            return;
        }

        var newDamageDealt = _damageHero.Host.damageDealt;
        if (newDamageDealt != _lastDamageDealt) {
            _lastDamageDealt = newDamageDealt;
            
            var data = new EntityNetworkData {
                Type = EntityComponentType.DamageHero
            };
            data.Packet.Write((byte) newDamageDealt);

            SendData(data);
        }
    }

    /// <inheritdoc />
    public override void InitializeHost() {
    }

    /// <inheritdoc />
    public override void Update(EntityNetworkData data) {
        var damageDealt = data.Packet.ReadByte();
        _damageHero.Host.damageDealt = damageDealt;
        _damageHero.Client.damageDealt = damageDealt;
    }

    /// <inheritdoc />
    public override void Destroy() {
        MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdate;
    }
}