using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity.Component;

/// <inheritdoc />
/// This component manages the sprite renderer of the entity.
internal class SpriteRendererComponent : EntityComponent {
    /// <summary>
    /// The host-client pair of <see cref="SpriteRenderer"/> unity components of the entity.
    /// </summary>
    private readonly HostClientPair<SpriteRenderer> _spriteRenderer;

    /// <summary>
    /// The last value of 'enabled' for the sprite renderer.
    /// </summary>
    private bool _lastEnabled;

    public SpriteRendererComponent(
        NetClient netClient,
        ushort entityId,
        HostClientPair<GameObject> gameObject,
        HostClientPair<SpriteRenderer> spriteRenderer
    ) : base(netClient, entityId, gameObject) {
        _spriteRenderer = spriteRenderer;
        _lastEnabled = spriteRenderer.Host.enabled;

        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdate;
    }

    /// <summary>
    /// Callback method to check for sprite renderer updates.
    /// </summary>
    private void OnUpdate() {
        if (IsControlled) {
            return;
        }

        if (GameObject.Host == null) {
            return;
        }

        var newEnabled = _spriteRenderer.Host.enabled;
        if (newEnabled != _lastEnabled) {
            _lastEnabled = newEnabled;
            
            var data = new EntityNetworkData {
                Type = EntityComponentType.SpriteRenderer
            };
            data.Packet.Write(newEnabled);

            SendData(data);
        }
    }

    /// <inheritdoc />
    public override void InitializeHost() {
    }

    /// <inheritdoc />
    public override void Update(EntityNetworkData data) {
        var enabled = data.Packet.ReadBool();
        _spriteRenderer.Host.enabled = enabled;
        _spriteRenderer.Client.enabled = enabled;
    }

    /// <inheritdoc />
    public override void Destroy() {
        MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdate;
    }
}