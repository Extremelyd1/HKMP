using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Fsm;

/// <summary>
/// Class for patching functionality of PlayMaker FSMs.
/// </summary>
internal class FsmPatcher {
    /// <summary>
    /// Registers the hooks necessary to patch.
    /// </summary>
    public void RegisterHooks() {
        On.PlayMakerFSM.OnEnable += OnFsmEnable;
    }

    /// <summary>
    /// Callback method for the PlayMakerFSM#OnEnable hook.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The PlayMakerFSM instance that the hooked method was called on.</param>
    private void OnFsmEnable(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self) {
        orig(self);

        // Check if it is a FSM for picking up shiny items
        if (self.name.Equals("Inspect Region") && self.Fsm.Name.Equals("inspect")) {
            // Find the action that checks whether the player enters the pickup area
            var triggerAction = self.GetFirstAction<Trigger2dEvent>("Out Of Range");
            if (triggerAction == null) {
                Logger.Warn("Could not patch inspect FSM, because trigger action does not exist");
                return;
            }

            // Add a collide tag to the action to ensure it only triggers on the local player
            triggerAction.collideTag.Value = "Player";
            triggerAction.collideTag.UseVariable = false;
        }
    }
}
