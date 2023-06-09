using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity.Component;

/// <inheritdoc />
/// This component manages the velocity of the entity.
internal class VelocityComponent : EntityComponent {
    /// <summary>
    /// The host <see cref="Rigidbody2D"/> unity component of the entity.
    /// </summary>
    private readonly Rigidbody2D _rigidbody;

    /// <summary>
    /// The last value of 'velocity' for the rigidbody.
    /// </summary>
    private Vector2 _lastVelocity;

    /// <summary>
    /// The velocity received from updates to this component. Used to keep track of the velocity as we cannot
    /// apply it the rigidbody of our host object as long as the host object is not active.
    /// </summary>
    private Vector2? _receivedVelocity;

    public VelocityComponent(
        NetClient netClient,
        byte entityId,
        HostClientPair<GameObject> gameObject,
        Rigidbody2D rigidbody
    ) : base(netClient, entityId, gameObject) {
        _rigidbody = rigidbody;
        _lastVelocity = rigidbody.velocity;

        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdate;
    }

    /// <summary>
    /// Callback method to check for updates.
    /// </summary>
    private void OnUpdate() {
        if (IsControlled) {
            return;
        }

        if (GameObject.Host == null) {
            return;
        }

        if (_receivedVelocity.HasValue && GameObject.Host.activeInHierarchy) {
            _rigidbody.velocity = _receivedVelocity.Value;
            _receivedVelocity = null;
        }

        var newVelocity = _rigidbody.velocity;
        if (newVelocity != _lastVelocity) {
            _lastVelocity = newVelocity;
            
            var data = new EntityNetworkData {
                Type = EntityNetworkData.DataType.Velocity
            };
            data.Packet.Write(newVelocity.x);
            data.Packet.Write(newVelocity.y);

            SendData(data);
        }
    }

    /// <inheritdoc />
    public override void InitializeHost() {
    }

    /// <inheritdoc />
    public override void Update(EntityNetworkData data) {
        if (!IsControlled) {
            return;
        }
        
        var velocity = new Vector2(
            data.Packet.ReadFloat(),
            data.Packet.ReadFloat()
        );
        _receivedVelocity = velocity;
    }

    /// <inheritdoc />
    public override void Destroy() {
        MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdate;
    }
}