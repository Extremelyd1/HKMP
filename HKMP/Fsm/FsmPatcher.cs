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
        
        // Insert an action that specifically re-scales the fireball to be in line with the player's scale
        // For some reason players dying in acid might mess up the future scale of fireballs
        // This is the case for the "top" and "spiral" objects of the fireball

        // TODO: figure out the root cause of the issue that this band-aid solution fixes. It has something to do
        // with the HazardDeath animation effect playing that messes up future fireballs in the same scene.
        // Although the issue already exists as soon as I try to find the FSM for the acid death object.
        // Something with FSM initialization gets messed up at that point, but I'm confused why this doesn't happen
        // with spike deaths or anywhere else for that matter.
        if ((self.name.Contains("Fireball Top") || self.name.Contains("Fireball2 Top")) && self.Fsm.Name.Equals("Fireball Cast")) {
            self.InsertMethod("L or R", 3, () => {
                var hero = HeroController.instance.gameObject;
                if (hero == null) {
                    return;
                }
                
                var heroScaleX = hero.transform.localScale.x;

                var scaleFsmVar = self.Fsm.GetFsmFloat("Hero Scale");
                if (scaleFsmVar == null) {
                    return;
                }

                scaleFsmVar.Value = heroScaleX;
            });
        }

        if (self.name.Contains("Fireball2 Spiral") && self.Fsm.Name.Equals("Fireball Control")) {
            self.InsertMethod("Init", 3, () => {
                var hero = HeroController.instance.gameObject;
                if (hero == null) {
                    return;
                }
                
                var heroScaleX = hero.transform.localScale.x;

                var scaleFsmVar = self.Fsm.GetFsmFloat("X Scale");
                if (scaleFsmVar == null) {
                    return;
                }

                scaleFsmVar.Value = heroScaleX;
            });
        }
    }
}
