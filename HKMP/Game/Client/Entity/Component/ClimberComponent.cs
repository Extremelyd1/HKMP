using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using UnityEngine;

namespace Hkmp.Game.Client.Entity.Component;

/// <inheritdoc />
/// This component manages the climber behaviour of the entity.
internal class ClimberComponent : EntityComponent {
    /// <summary>
    /// The <see cref="Climber"/> unity component of the entity.
    /// </summary>
    private readonly Climber _climber;

    public ClimberComponent(
        NetClient netClient,
        byte entityId,
        HostClientPair<GameObject> gameObject,
        Climber climber
    ) : base(netClient, entityId, gameObject) {
        _climber = climber;
        _climber.enabled = false;
    }

    /// <inheritdoc />
    public override void InitializeHost() {
        if (_climber != null) {
            _climber.enabled = true;
        }
    }

    /// <inheritdoc />
    public override void Update(EntityNetworkData data) {
    }

    /// <inheritdoc />
    public override void Destroy() {
    }
}