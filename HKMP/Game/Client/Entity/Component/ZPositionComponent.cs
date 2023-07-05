using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Entity.Component; 

/// <inheritdoc />
/// This component manages the Z-position of an entity.
internal class ZPositionComponent : EntityComponent {
    /// <summary>
    /// The last value of the Z position.
    /// </summary>
    private float _lastZ;
    
    public ZPositionComponent(
        NetClient netClient, 
        ushort entityId, 
        HostClientPair<GameObject> gameObject
    ) : base(netClient, entityId, gameObject) {
        _lastZ = gameObject.Host.transform.position.z;
        
        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdate;
    }

    /// <summary>
    /// Callback for checking the Z-position each update.
    /// </summary>
    private void OnUpdate() {
        if (IsControlled) {
            return;
        }

        if (GameObject.Host == null) {
            return;
        }

        var newZ = GameObject.Host.transform.position.z;
        if (!_lastZ.Equals(newZ)) {
            _lastZ = newZ;

            var data = new EntityNetworkData {
                Type = EntityComponentType.ZPosition
            };
            data.Packet.Write(newZ);

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
        
        var newZ = data.Packet.ReadFloat();
        
        SetZ(GameObject.Host);
        SetZ(GameObject.Client);

        void SetZ(GameObject gameObject) {
            var position = gameObject.transform.position;
            gameObject.transform.position = new Vector3(
                position.x,
                position.y,
                newZ
            );
        }
    }

    /// <inheritdoc />
    public override void Destroy() {
        MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdate;
    }
}