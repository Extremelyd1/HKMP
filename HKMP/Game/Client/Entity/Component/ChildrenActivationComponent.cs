using System.Collections.Generic;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity.Component; 

/// <inheritdoc />
/// This component manages the activation of the children of an entity.
internal class ChildrenActivationComponent : EntityComponent {
    private readonly List<GameObject> _hostChildren;
    private readonly List<GameObject> _clientChildren;

    private bool _lastActive;
    
    public ChildrenActivationComponent(
        NetClient netClient, 
        ushort entityId, 
        HostClientPair<GameObject> gameObject
    ) : base(netClient, entityId, gameObject) {
        _hostChildren = gameObject.Host.GetChildren();
        if (_hostChildren.Count == 0) {
            return;
        }

        _lastActive = _hostChildren[0].activeSelf;

        _clientChildren = gameObject.Client.GetChildren();

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

        var newActive = _hostChildren[0].activeSelf;
        if (newActive != _lastActive) {
            _lastActive = newActive;

            var data = new EntityNetworkData {
                Type = EntityComponentType.ChildrenActivation
            };
            data.Packet.Write(newActive);

            SendData(data);
        }
    }

    /// <inheritdoc />
    public override void InitializeHost() {
    }

    /// <inheritdoc />
    public override void Update(EntityNetworkData data, bool alreadyInSceneUpdate) {
        if (!IsControlled) {
            return;
        }

        var newActive = data.Packet.ReadBool();

        foreach (var child in _hostChildren) {
            child.SetActive(newActive);
        }
        foreach (var child in _clientChildren) {
            child.SetActive(newActive);
        }
    }

    /// <inheritdoc />
    public override void Destroy() {
        MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdate;
    }
}
