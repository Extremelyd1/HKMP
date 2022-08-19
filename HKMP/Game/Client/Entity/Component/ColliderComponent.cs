using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Entity.Component; 

internal class ColliderComponent : EntityComponent {
    private readonly HostClientPair<BoxCollider2D> _collider;

    private bool? _lastEnabled;

    public ColliderComponent(
        NetClient netClient, 
        byte entityId, 
        HostClientPair<GameObject> gameObject,
        HostClientPair<BoxCollider2D> collider
    ) : base(netClient, entityId, gameObject) {
        _collider = collider;

        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdateCollider;
    }

    private void OnUpdateCollider() {
        if (IsControlled) {
            return;
        }

        if (_collider.Host == null) {
            return;
        }

        var newEnabled = _collider.Host.enabled;
        if (!_lastEnabled.HasValue || newEnabled != _lastEnabled.Value) {
            Logger.Info($"Collider of {GameObject.Host.name} enabled changed to: {newEnabled}");
            _lastEnabled = newEnabled;

            var data = new EntityNetworkData {
                Type = EntityNetworkData.DataType.Collider
            };
            data.Packet.Write(newEnabled);

            SendData(data);
        }
    }

    public override void InitializeHost() {
    }

    public override void Update(EntityNetworkData data) {
        Logger.Info($"Received collider update for {GameObject.Client.name}");
        
        if (!IsControlled) {
            Logger.Info("  Entity was not controlled");
            return;
        }
        
        var enabled = data.Packet.ReadBool();
        _collider.Client.enabled = enabled;
        
        Logger.Info($"  Enabled: {enabled}");
    }

    public override void Destroy() {
        MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdateCollider;
    }
}