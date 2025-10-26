using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Entity.Component;

/// <inheritdoc />
/// This component manages the challenge prompt that appears for Mantis Lords.
internal class ChallengePromptComponent : EntityComponent {
    /// <summary>
    /// The game object that handles the challenge prompt pop-up.
    /// </summary>
    private readonly GameObject _promptObj;

    /// <summary>
    /// The FSM corresponding to the challenge prompt object.
    /// </summary>
    private readonly PlayMakerFSM _promptFsm;
    
    public ChallengePromptComponent(
        NetClient netClient,
        ushort entityId,
        HostClientPair<GameObject> gameObject
    ) : base(netClient, entityId, gameObject) {
        var hostObj = gameObject.Host;
        var parent = hostObj.transform.parent;
        var parentTransform = parent.Find("Challenge Prompt");
        if (!parentTransform) {
            Logger.Debug("Could not find Challenge Prompt object");
            return;
        }

        _promptObj = parent.Find("Challenge Prompt").gameObject;
        _promptFsm = _promptObj.LocateMyFSM("Challenge Start");

        _promptFsm.InsertMethod("Take Control", 6, () => {
            var data = new EntityNetworkData {
                Type = EntityComponentType.ChallengePrompt
            };
            data.Packet.Write(0);

            SendData(data);
        });
    }

    /// <inheritdoc />
    public override void InitializeHost() {
    }

    /// <inheritdoc />
    public override void Update(EntityNetworkData data, bool alreadyInSceneUpdate) {
        var type = data.Packet.ReadByte();

        // If the player is a scene client we destroy the prompt, otherwise we start the fight by progressing the FSM
        if (IsControlled) {
            if (_promptObj != null) {
                Object.Destroy(_promptObj);
            }
        } else {
            // Remove actions that rely on the local player
            _promptFsm.RemoveFirstAction<Tk2dPlayAnimation>("Challenge");
            _promptFsm.RemoveFirstAction<Tk2dWatchAnimationEvents>("Challenge");

            // Get some actions that we want to re-use in another state
            var activateObjAction = _promptFsm.GetFirstAction<ActivateGameObject>("Take Control");
            var sendEventAction = _promptFsm.GetFirstAction<SendEventByName>("Take Control");

            // Put these actions in the Challenge state for execution
            _promptFsm.InsertAction("Challenge", activateObjAction, 0);
            _promptFsm.InsertAction("Challenge", sendEventAction, 1);

            // Get the watch animation events action so we can get the FsmEvent is sends
            var watchAnimationEvent = _promptFsm.GetFirstAction<Tk2dWatchAnimationEvents>("Challenge Audio");
            
            // Insert a method that sends the event to go to the next stage instead of waiting for the animation to finish
            _promptFsm.InsertMethod("Challenge Audio", 1, () => {
                _promptFsm.Fsm.Event(watchAnimationEvent.animationCompleteEvent);
            });
            // Remove the original action
            _promptFsm.RemoveFirstAction<Tk2dWatchAnimationEvents>("Challenge Audio");
            
            // Start the FSM from the state 'Challenge'
            _promptFsm.SetState("Challenge");
        }
    }

    /// <inheritdoc />
    public override void Destroy() {
    }
}
