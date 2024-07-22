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
        
        // Patch the break floor FSM to make sure the Hero Range is not checked so remote players can break the floor
        if (self.Fsm.Name.Equals("break_floor")) {
            var boolTestAction = self.GetAction<BoolTest>("Check If Nail", 0);
            if (boolTestAction != null) {
                self.RemoveAction("Check If Nail", 0);
            }
        }

        // Patch Switch Control FSMs to ignore the range requirements to allow remote players from hitting them
        if (self.Fsm.Name.Equals("Switch Control")) {
            if (self.GetState("Range") != null) {
                self.RemoveFirstAction<BoolTest>("Range");
            }

            if (self.GetState("Check If Nail") != null) {
                self.RemoveFirstAction<BoolTest>("Check If Nail");
            }
        }

        // Patch the Mantis Throne Main to not rely on animation events from the local player in case another
        // player challenges the boss
        if (self.name.Equals("Mantis Lord Throne 2") && self.Fsm.Name.Equals("Mantis Throne Main")) {
            // Get the animation action for the animation clip that is played
            var animationAction = self.GetFirstAction<Tk2dPlayAnimation>("End Challenge");

            // Get the game object for the animation and check if it is not null
            var go = self.Fsm.GetOwnerDefaultTarget(animationAction.gameObject);
            if (go == null) {
                return;
            }

            // Get the animator from the game object, the clip from the action and its length
            var animator = go.GetComponent<tk2dSpriteAnimator>();
            var clip = animator.GetClipByName(animationAction.clipName.Value);
            var length = clip.Duration;

            // Get the original watch animation action for the FSM event it sends
            var watchAnimationAction = self.GetFirstAction<Tk2dWatchAnimationEvents>("End Challenge");
            
            // Insert a wait action that takes exactly the duration of the animation and sends the original event
            // when it finishes
            self.InsertAction("End Challenge", new Wait {
                time = length,
                finishEvent = watchAnimationAction.animationCompleteEvent
            }, 2);
            
            // Remove the original watch animation action
            self.RemoveFirstAction<Tk2dWatchAnimationEvents>("End Challenge");
        }
        
        // Code for modifying the collision check on collapsing floors to include remote players (not working)
        // if (self.name.Equals("Collapser Small") && self.Fsm.Name.Equals("collapse small")) {
        //     self.InsertAction("Idle", new Collision2dEventLayer {
        //         Enabled = true,
        //         collideLayer = 9,
        //         collideTag = new FsmString(),
        //         sendEvent = FsmEvent.GetFsmEvent("BREAK"),
        //         storeCollider = new FsmGameObject(),
        //         storeForce = new FsmFloat()
        //     }, 7);
        //     self.RemoveFirstAction<Collision2dEvent>("Idle");
        //
        //     var rigidbody = self.gameObject.AddComponent<Rigidbody2D>();
        //     rigidbody.isKinematic = true;
        // }
    }
}
