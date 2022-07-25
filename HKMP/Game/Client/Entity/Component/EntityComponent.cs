using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using UnityEngine;

namespace Hkmp.Game.Client.Entity.Component;

internal abstract class EntityComponent {
    private readonly NetClient _netClient;
    private readonly byte _entityId;

    protected readonly GameObject HostObject;
    protected readonly GameObject ClientObject;

    public bool IsControlled { get; set; }

    protected EntityComponent(
        NetClient netClient,
        byte entityId,
        GameObject hostObject,
        GameObject clientObject
    ) {
        _netClient = netClient;
        _entityId = entityId;

        HostObject = hostObject;
        ClientObject = clientObject;
    }

    protected void SendData(EntityNetworkData data) {
        _netClient.UpdateManager.AddEntityData(_entityId, data);
    }

    public abstract void InitializeHost();
    public abstract void Update(EntityNetworkData data);
    public abstract void Destroy();
}