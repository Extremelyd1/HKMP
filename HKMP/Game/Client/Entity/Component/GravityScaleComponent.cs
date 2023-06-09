using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity.Component; 

/// <inheritdoc />
/// This component manages the gravity scale of an entity.
internal class GravityScaleComponent : EntityComponent {
    /// <summary>
    /// The host <see cref="Rigidbody2D"/> unity component of the entity.
    /// </summary>
    private readonly Rigidbody2D _rigidbody;
    
    /// <summary>
    /// The last value of the gravity scale.
    /// </summary>
    private float _lastScale;
    
    /// <summary>
    /// The gravity scale received from updates to this component. Used to keep track of the gravity scale as we
    /// cannot apply it the rigidbody of our host object as long as the host object is not active.
    /// </summary>
    private float? _receivedGravityScale;
    
    public GravityScaleComponent(
        NetClient netClient, 
        byte entityId, 
        HostClientPair<GameObject> gameObject,
        Rigidbody2D rigidbody
    ) : base(netClient, entityId, gameObject) {
        _rigidbody = rigidbody;
        _lastScale = rigidbody.gravityScale;
        
        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdate;
    }

    /// <summary>
    /// Callback for checking the gravity scale each update.
    /// </summary>
    private void OnUpdate() {
        if (IsControlled) {
            return;
        }

        if (GameObject.Host == null) {
            return;
        }

        if (_receivedGravityScale.HasValue && GameObject.Host.activeInHierarchy) {
            _rigidbody.gravityScale = _receivedGravityScale.Value;
            _receivedGravityScale = null;
        }

        var newGravityScale = _rigidbody.gravityScale;
        if (!newGravityScale.Equals(_lastScale)) {
            _lastScale = newGravityScale;
            
            var data = new EntityNetworkData {
                Type = EntityNetworkData.DataType.GravityScale
            };
            data.Packet.Write(newGravityScale);

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

        _receivedGravityScale = data.Packet.ReadFloat();
    }

    /// <inheritdoc />
    public override void Destroy() {
        MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdate;
    }
}