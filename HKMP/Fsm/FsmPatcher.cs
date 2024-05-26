using Hkmp.Util;
using Hkmp.Logging;
using HutongGames.PlayMaker.Actions;

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

        // Specific patch for the Battle Control FSM in Fungus2_05 where the Shroomal Ogres are with the Charm Notch
        if (self.name.Equals("Battle Scene v2") && 
            self.Fsm.Name.Equals("Battle Control") && 
            self.gameObject.scene.name.Equals("Fungus2_05")) {
            var findBrawler1 = self.GetAction<FindGameObject>("Init", 6);
            var findBrawler2 = self.GetAction<FindGameObject>("Init", 8);

            // With the way the entity system works, the Mushroom Brawlers might not be found with the existing actions
            // We complement these actions by checking if the Brawlers were found and if not, find them another way
            self.InsertMethod("Init", 7, () => {
                if (findBrawler1.store.Value == null) {
                    var brawler1 = GameObjectUtil.FindInactiveGameObject("Mushroom Brawler 1");
                    findBrawler1.store.Value = brawler1;
                }
            });
            self.InsertMethod("Init", 10, () => {
                if (findBrawler2.store.Value == null) {
                    var brawler2 = GameObjectUtil.FindInactiveGameObject("Mushroom Brawler 2");
                    findBrawler2.store.Value = brawler2;
                }
            });
        }
        
        // Patch the break floor FSM to make sure the Hero Range is not checked so remote players can break the floor
        if (self.Fsm.Name.Equals("break_floor")) {
            var boolTestAction = self.GetAction<BoolTest>("Check If Nail", 0);
            if (boolTestAction != null) {
                self.RemoveAction("Check If Nail", 0);
            }
        }
    }
}
