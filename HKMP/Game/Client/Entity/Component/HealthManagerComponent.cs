using System;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using UnityEngine;

namespace Hkmp.Game.Client.Entity.Component; 

// TODO: periodically (or on hit) sync the health of the entity so on scene host transfer we can reset health
internal class HealthManagerComponent : EntityComponent {
    private readonly HostClientPair<HealthManager> _healthManager;

    public HealthManagerComponent(
        NetClient netClient, 
        byte entityId, 
        HostClientPair<GameObject> gameObject,
        HostClientPair<HealthManager> healthManager
    ) : base(netClient, entityId, gameObject) {
        _healthManager = healthManager;
        
        On.HealthManager.Die += HealthManagerOnDie;
    }

    private void HealthManagerOnDie(
        On.HealthManager.orig_Die orig, 
        HealthManager self, 
        float? attackDirection, 
        AttackTypes attackType, 
        bool ignoreEvasion
    ) {
        if (IsControlled) {
            Logger.Get().Info(this, "Entity's health manager tried dying, cancelled because client");
            return;
        }

        orig(self, attackDirection, attackType, ignoreEvasion);

        var data = new EntityNetworkData {
            Type = EntityNetworkData.DataType.HealthManager
        };

        if (attackDirection.HasValue) {
            data.Packet.Write(true);
            data.Packet.Write(attackDirection.Value);
        } else {
            data.Packet.Write(false);
        }

        data.Packet.Write((byte) attackType);

        data.Packet.Write(ignoreEvasion);
    }

    public override void InitializeHost() {
    }

    public override void Update(EntityNetworkData data) {
        throw new System.NotImplementedException();
    }

    public override void Destroy() {
        throw new System.NotImplementedException();
    }
}