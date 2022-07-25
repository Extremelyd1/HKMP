using System;
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
        GameObject hostObject,
        GameObject clientObject,
        Climber climber
    ) : base(netClient, entityId, hostObject, clientObject) {
        _climber = climber;
        _climber.enabled = false;
        
        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdateRotation;
    }

    private void OnUpdateRotation() {
        if (IsControlled) {
            return;
        }

        if (HostObject == null) {
            return;
        }

        var transform = HostObject.transform;

        var newRotation = transform.rotation.eulerAngles;
        if (newRotation != _lastRotation) {
            _lastRotation = newRotation;

            var data = new EntityNetworkData {
                Type = EntityNetworkData.DataType.Rotation
            };
            data.Data.AddRange(BitConverter.GetBytes(newRotation.z));

            SendData(data);
        }
    }
    
    public override void InitializeHost() {
        if (_climber != null) {
            _climber.enabled = true;
        }
    }

    public override void Update(EntityNetworkData data) {
        var rotation = BitConverter.ToSingle(data.Data.ToArray(), 0);
                    
        var transform = ClientObject.transform;
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