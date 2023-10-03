using Hkmp.Util;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Animation.Effects; 

/// <summary>
/// Represents an animation effect that can be parried, such as nail slashes or nail arts.
/// </summary>
internal abstract class ParryableEffect : DamageAnimationEffect {
    /// <summary>
    /// The FSM for the nail parry effect.
    /// </summary>
    private readonly PlayMakerFSM NailClashTink;

    protected ParryableEffect() {
        var hiveKnightSlash = HkmpMod.PreloadedObjects["Hive_05"]["Battle Scene/Hive Knight/Slash 1"];
        NailClashTink = hiveKnightSlash.GetComponent<PlayMakerFSM>();
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
        detectingState.GetTransition(0).FsmEvent = new FsmEvent("TAKE DAMAGE 2");

        var triggerAction = fsm.GetFirstAction<Trigger2dEventLayer>("Detecting");
        triggerAction.sendEvent = new FsmEvent("TAKE DAMAGE 2");
    }
}
