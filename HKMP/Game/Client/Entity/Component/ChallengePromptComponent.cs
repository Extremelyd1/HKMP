using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity.Component;

/// <inheritdoc />
/// This component manages the challenge prompt that appears for Mantis Lords.
internal class ChallengePromptComponent : EntityComponent {
    /// <summary>
    /// The game object that handles the challenge prompt pop-up.
    /// </summary>
    private readonly GameObject _promptObj;
    
    public ChallengePromptComponent(
        NetClient netClient,
        ushort entityId,
        HostClientPair<GameObject> gameObject
    ) : base(netClient, entityId, gameObject) {
        var hostObj = gameObject.Host;
        var parent = hostObj.transform.parent;
        _promptObj = parent.Find("Challenge Prompt").gameObject;

        var promptFsm = _promptObj.LocateMyFSM("Challenge Start");
        promptFsm.InsertMethod("Take Control", 6, () => {
            var data = new EntityNetworkData {
                Type = EntityComponentType.ChallengePrompt
            };
            data.Packet.Write(true);

            SendData(data);
        });
    }

    /// <inheritdoc />
    public override void InitializeHost() {
    }

    /// <inheritdoc />
    public override void Update(EntityNetworkData data) {
        var destroyPrompt = data.Packet.ReadBool();
        if (!destroyPrompt) {
            return;
        }

        if (_promptObj != null) {
            Object.Destroy(_promptObj);
        }
    }

    /// <inheritdoc />
    public override void Destroy() {
    }
}
