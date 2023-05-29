using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity.Component;

// TODO: optimization idea: only add this component to objects where the FSM has an action that enables/disables the
// mesh renderer

/// <inheritdoc />
/// This component manages the mesh renderer of the entity.
internal class MeshRendererComponent : EntityComponent {
    /// <summary>
    /// The host-client pair of <see cref="MeshRenderer"/> unity components of the entity.
    /// </summary>
    private readonly HostClientPair<MeshRenderer> _meshRenderer;

    /// <summary>
    /// The last value of 'enabled' for the mesh renderer.
    /// </summary>
    private bool _lastEnabled;

    public MeshRendererComponent(
        NetClient netClient,
        byte entityId,
        HostClientPair<GameObject> gameObject,
        HostClientPair<MeshRenderer> meshRenderer
    ) : base(netClient, entityId, gameObject) {
        _meshRenderer = meshRenderer;
        _lastEnabled = meshRenderer.Host.enabled;

        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdate;
    }

    /// <summary>
    /// Callback method to check for mesh renderer updates.
    /// </summary>
    private void OnUpdate() {
        if (IsControlled) {
            return;
        }

        if (GameObject.Host == null) {
            return;
        }

        var newEnabled = _meshRenderer.Host.enabled;
        if (newEnabled != _lastEnabled) {
            _lastEnabled = newEnabled;
            
            var data = new EntityNetworkData {
                Type = EntityNetworkData.DataType.MeshRenderer
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
        _meshRenderer.Host.enabled = enabled;
        _meshRenderer.Client.enabled = enabled;
    }

    /// <inheritdoc />
    public override void Destroy() {
        MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdate;
    }
}