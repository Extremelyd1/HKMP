using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Entity.Component;

/// <inheritdoc />
/// This component manages the hazard respawn changes within certain bossfights.
internal class HazardRespawnComponent : EntityComponent {
    private readonly PlayMakerFSM _hostControlFsm;
    
    public HazardRespawnComponent(
        NetClient netClient,
        ushort entityId,
        HostClientPair<GameObject> gameObject
    ) : base(netClient, entityId, gameObject) {
        var host = gameObject.Host;
        _hostControlFsm = host.LocateMyFSM("Control");
        if (!_hostControlFsm) {
            Logger.Error("Could not find 'Control' FSM on Radiance host object");
            return;
        }
        
        _hostControlFsm.InsertMethod("Climb Plats1", 7, () => {
            Logger.Debug("Climb Plats1 state reached, sending hazard respawn data");
            
            var data = new EntityNetworkData {
                Type = EntityComponentType.HazardRespawn
            };
            
            data.Packet.Write((byte) 0);

            SendData(data);
        });
    }

    /// <inheritdoc />
    public override void InitializeHost() {
    }

    /// <inheritdoc />
    public override void Update(EntityNetworkData data, bool alreadyInSceneUpdate) {
        var numRespawn = data.Packet.ReadByte();

        if (numRespawn == 0) {
            var p2AHazardVar = _hostControlFsm.FsmVariables.GetFsmGameObject("P2A Hazard");
            if (p2AHazardVar == null) {
                Logger.Error("Could not find P2A Hazard variable in host FSM");
                return;
            }

            var p2AHazard = p2AHazardVar.Value;
            if (!p2AHazard) {
                Logger.Error("P2A Hazard variable value is null in host FSM");
                return;
            }
            
            Logger.Debug("Setting hazard respawn to plats hazard respawn");
            HeroController.instance.SetHazardRespawn(p2AHazard.transform.position, true);
        }
    }

    /// <inheritdoc />
    public override void Destroy() {
        
    }
}
