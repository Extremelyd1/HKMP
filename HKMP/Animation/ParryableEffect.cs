using Hkmp.Util;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Animation; 

/// <summary>
/// Represents an animation effect that can be parried, such as nail slashes or nail arts.
/// </summary>
internal abstract class ParryableEffect : DamageAnimationEffect {
    /// <summary>
    /// The FSM for the nail parry effect.
    /// </summary>
    private readonly PlayMakerFSM NailClashTink;

    protected ParryableEffect() {
        var slySlash = HkmpMod.PreloadedObjects["GG_Sly"]["Battle Scene/Sly Boss/S1"];
        NailClashTink = slySlash.GetComponent<PlayMakerFSM>();
    }

    /// <summary>
    /// Adds the FSM responsible for parrying to the given game object.
    /// Does additional modification to the FSM to make it suitable for PvP.
    /// </summary>
    /// <param name="target">The GameObject that the FSM should be added to.</param>
    protected void AddParryFsm(GameObject target) {
        var fsm = target.AddComponent<PlayMakerFSM>();
        fsm.SetFsmTemplate(NailClashTink.FsmTemplate);

        var detectingState = fsm.GetState("Detecting");
        var blockedHitState = fsm.GetState("Blocked Hit");

        const string eventName = "TAKE DAMAGE 2";
        
        // Override the transitions to only transition on the "TAKE DAMAGE 2" event
        detectingState.Transitions = new[] {
            new FsmTransition {
                ToState = "Blocked Hit",
                ToFsmState = blockedHitState,
                FsmEvent = FsmEvent.GetFsmEvent(eventName)
            }
        };

        // Also change the sendEvent variable of the action that triggers the transition
        var triggerAction = fsm.GetFirstAction<Trigger2dEventLayer>("Detecting");
        triggerAction.sendEvent = FsmEvent.GetFsmEvent(eventName);
    }
}
