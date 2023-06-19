using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity.Component;

/// <inheritdoc />
/// This component manages the rotation of the entity.
internal class RotationComponent : EntityComponent {
    /// <summary>
    /// The last rotation of the entity.
    /// </summary>
    private Vector3 _lastRotation;

    public RotationComponent(
        NetClient netClient,
        byte entityId,
        HostClientPair<GameObject> gameObject
    ) : base(netClient, entityId, gameObject) {
        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdateRotation;
    }

    /// <summary>
    /// Callback method to check for rotation updates.
    /// </summary>
    private void OnUpdateRotation() {
        if (IsControlled) {
            return;
        }

        if (GameObject.Host == null) {
            return;
        }

        var transform = GameObject.Host.transform;

        var newRotation = transform.rotation.eulerAngles;
        if (newRotation != _lastRotation) {
            _lastRotation = newRotation;

            var data = new EntityNetworkData {
                Type = EntityComponentType.Rotation
            };
            data.Packet.Write(newRotation.z);

            SendData(data);
        }
    }

    /// <inheritdoc />
    public override void InitializeHost() {
    }

    /// <inheritdoc />
    public override void Update(EntityNetworkData data) {
        var rotation = data.Packet.ReadFloat();

        SetRotation(GameObject.Host);
        SetRotation(GameObject.Client);
        
        void SetRotation(GameObject obj) {
            var transform = obj.transform;
            var eulerAngles = transform.eulerAngles;
            transform.eulerAngles = new Vector3(
                eulerAngles.x,
                eulerAngles.y,
                rotation
            );
        }
    }

    /// <inheritdoc />
    public override void Destroy() {
        MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdateRotation;
    }
}