using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity.Component;

internal class RotationComponent : EntityComponent {
    private readonly Climber _climber;

    private Vector3 _lastRotation;

    public RotationComponent(
        NetClient netClient,
        byte entityId,
        HostClientPair<GameObject> gameObject,
        Climber climber
    ) : base(netClient, entityId, gameObject) {
        _climber = climber;
        _climber.enabled = false;

        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdateRotation;
    }

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
                Type = EntityNetworkData.DataType.Rotation
            };
            data.Packet.Write(newRotation.z);

            SendData(data);
        }
    }

    public override void InitializeHost() {
        if (_climber != null) {
            _climber.enabled = true;
        }
    }

    public override void Update(EntityNetworkData data) {
        var rotation = data.Packet.ReadFloat();

        var transform = GameObject.Client.transform;
        var eulerAngles = transform.eulerAngles;
        transform.eulerAngles = new Vector3(
            eulerAngles.x,
            eulerAngles.y,
            rotation
        );
    }

    public override void Destroy() {
        MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdateRotation;
    }
}