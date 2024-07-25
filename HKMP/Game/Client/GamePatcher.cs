using System;
using System.Linq;
using System.Reflection;
using Modding;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client;

/// <summary>
/// Class that manager patches such as IL and On hooks that are standalone patches for the multiplayer to function
/// correctly.
/// </summary>
internal class GamePatcher {
    /// <summary>
    /// The binding flags for obtaining certain types for hooking.
    /// </summary>
    private const BindingFlags BindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

    /// <summary>
    /// The IL Hook for the bridge lever method.
    /// </summary>
    private ILHook _bridgeLeverIlHook;
    
    /// <summary>
    /// Register the hooks.
    /// </summary>
    public void RegisterHooks() {
        // Register IL hook for changing the behaviour of tink effects
        IL.TinkEffect.OnTriggerEnter2D += TinkEffectOnTriggerEnter2D;
        
        IL.HealthManager.TakeDamage += HealthManagerOnTakeDamage;

        On.BridgeLever.OnTriggerEnter2D += BridgeLeverOnTriggerEnter2D;

        var type = typeof(BridgeLever).GetNestedType("<OpenBridge>d__13", BindingFlags);
        _bridgeLeverIlHook = new ILHook(type.GetMethod("MoveNext", BindingFlags), BridgeLeverOnOpenBridge);
    }

    /// <summary>
    /// De-register the hooks.
    /// </summary>
    public void DeregisterHooks() {
        IL.HealthManager.TakeDamage -= HealthManagerOnTakeDamage;
        
        On.BridgeLever.OnTriggerEnter2D -= BridgeLeverOnTriggerEnter2D;
        
        _bridgeLeverIlHook?.Dispose();
    }
    
    /// <summary>
    /// IL hook to change the TinkEffect OnTriggerEnter2D to not trigger on remote players.
    /// This method will insert IL to check whether the player responsible for the attack is the local player.
    /// </summary>
    private void TinkEffectOnTriggerEnter2D(ILContext il) {
        try {
            // Create a cursor for this context
            var c = new ILCursor(il);
            
            // Find the first return instruction in the method to branch to later
            var retInstr = il.Instrs.First(i => i.MatchRet());

            // Load the 'collision' argument onto the stack
            c.Emit(OpCodes.Ldarg_1);
            
            // Emit a delegate that pops the TinkEffect from the stack, checks whether the parent
            // of the effect is the knight and pushes a bool on the stack based on this
            c.EmitDelegate<Func<Collider2D, bool>>(collider => {
                var parent = collider.transform.parent;
                if (parent == null) {
                    return true;
                }
                
                parent = parent.parent;
                if (parent == null) {
                    return true;
                }
                
                return parent.gameObject.name != "Knight";
            });
            
            // Based on the bool we pushed to the stack earlier, we conditionally branch to the return instruction
            c.Emit(OpCodes.Brtrue, retInstr);
        } catch (Exception e) {
            Logger.Error($"Could not change TinkEffect#OnTriggerEnter2D IL:\n{e}");
        }
    }
    
    /// <summary>
    /// IL Hook to modify the behaviour of the TakeDamage method in HealthManager. This modification adds a
    /// conditional branch in case the nail swing from the HitInstance was from a remote player to ensure that
    /// soul is not gained for remote hits.
    /// </summary>
    private void HealthManagerOnTakeDamage(ILContext il) {
        try {
            // Create a cursor for this context
            var c = new ILCursor(il);
            
            // Goto the next virtual call to HeroController.SoulGain()
            c.GotoNext(i => i.MatchCallvirt(typeof(HeroController), "SoulGain"));

            // Move the cursor to before the call and call virtual instructions
            c.Index -= 1;

            // Emit the instruction to load the first parameter (hitInstance) onto the stack
            c.Emit(OpCodes.Ldarg_1);

            // Emit a delegate that takes the hitInstance parameter from the stack and pushes a boolean on the stack
            // that indicates whether the hitInstance was from a remote player's nail swing
            c.EmitDelegate<Func<HitInstance, bool>>(hitInstance => {
                if (hitInstance.Source == null || hitInstance.Source.transform == null) {
                    return false;
                }

                // Find the top-level parent of the hit instance
                var transform = hitInstance.Source.transform;
                while (transform.parent != null) {
                    transform = transform.parent;
                }

                var go = transform.gameObject;

                return go.tag != "Player";
            });

            // Define a label for the branch instruction
            var afterLabel = c.DefineLabel();

            // Emit the branch (on true) instruction with the label
            c.Emit(OpCodes.Brtrue, afterLabel);

            // Move the cursor after the SoulGain method call
            c.Index += 2;

            // Mark the label here, so we branch after the SoulGain method call on true
            c.MarkLabel(afterLabel);
        } catch (Exception e) {
            Logger.Error($"Could not change HealthManager#TakeDamage IL:\n{e}");
        }
    }
    
    /// <summary>
    /// Whether the local player hit the bridge lever.
    /// </summary>
    private bool _localPlayerBridgeLever;
    
    /// <summary>
    /// On Hook that stores a boolean depending on whether the local player hit the bridge lever or not. Used in the
    /// IL Hook below.
    /// </summary>
    private void BridgeLeverOnTriggerEnter2D(On.BridgeLever.orig_OnTriggerEnter2D orig, BridgeLever self, Collider2D collision) {
        var activated = ReflectionHelper.GetField<BridgeLever, bool>(self, "activated");
        
        if (!activated && collision.tag == "Nail Attack") {
            _localPlayerBridgeLever = collision.transform.parent?.parent?.tag == "Player";
        }
        
        orig(self, collision);
    }
    
    /// <summary>
    /// IL Hook to modify the OpenBridge method of BridgeLever to exclude locking players in place that did not hit
    /// the lever.
    /// </summary>
    private void BridgeLeverOnOpenBridge(ILContext il) {
        try {
            // Create a cursor for this context
            var c = new ILCursor(il);

            // Define the collection of instructions that matches the FreezeMoment call
            Func<Instruction, bool>[] freezeMomentInstructions = [
                i => i.MatchCall(typeof(global::GameManager), "get_instance"),
                i => i.MatchLdcI4(1),
                i => i.MatchCallvirt(typeof(global::GameManager), "FreezeMoment")
            ];

            // Goto after the FreezeMoment call
            c.GotoNext(MoveType.Before, freezeMomentInstructions);
            
            // Emit a delegate that puts the boolean on the stack
            c.EmitDelegate(() => _localPlayerBridgeLever);

            // Define the label to branch to
            var afterFreezeLabel = c.DefineLabel();
            
            // Then emit an instruction that branches to after the freeze if the boolean is false
            c.Emit(OpCodes.Brfalse, afterFreezeLabel);

            // Goto after the FreezeMoment call
            c.GotoNext(MoveType.After, freezeMomentInstructions);
            
            // Mark the label after the FreezeMoment call so we branch here
            c.MarkLabel(afterFreezeLabel);
            
            // Goto after the rumble call
            c.GotoNext(
                MoveType.After,
                i => i.MatchCall(typeof(GameCameras), "get_instance"),
                i => i.MatchLdfld(typeof(GameCameras), "cameraShakeFSM"),
                i => i.MatchLdstr("RumblingMed"),
                i => i.MatchLdcI4(1),
                i => i.MatchCall(typeof(FSMUtility), "SetBool")
            );
            
            // Emit a delegate that puts the boolean on the stack
            c.EmitDelegate(() => _localPlayerBridgeLever);
            
            // Define the label to branch to
            var afterRoarEnterLabel = c.DefineLabel();
            
            // Emit another instruction that branches over the roar enter FSM calls
            c.Emit(OpCodes.Brfalse, afterRoarEnterLabel);
            
            // Goto after the roar enter call
            c.GotoNext(
                MoveType.After, 
                i => i.MatchLdstr("ROAR ENTER"),
                i => i.MatchLdcI4(0),
                i => i.MatchCall(typeof(FSMUtility), "SendEventToGameObject")
            );
            
            // Mark the label after the Roar Enter call so we branch here
            c.MarkLabel(afterRoarEnterLabel);
            
            // Define the collection of instructions that matches the roar exit FSM call
            Func<Instruction, bool>[] roarExitInstructions = [
                i => i.MatchCall(typeof(HeroController), "get_instance"),
                i => i.MatchCallvirt(typeof(UnityEngine.Component), "get_gameObject"),
                i => i.MatchLdstr("ROAR EXIT"),
                i => i.MatchLdcI4(0),
                i => i.MatchCall(typeof(FSMUtility), "SendEventToGameObject")
            ];
            
            // Goto before the roar exit FSM call 
            c.GotoNext(MoveType.Before, roarExitInstructions);
            
            // Emit a delegate that puts the boolean on the stack
            c.EmitDelegate(() => _localPlayerBridgeLever);
            
            // Define the label to branch to
            var afterRoarExitLabel = c.DefineLabel();
            
            // Emit the last instruction to branch over the roar exit call
            c.Emit(OpCodes.Brfalse, afterRoarExitLabel);
            
            // Goto after the roar exit FSM call
            c.GotoNext(MoveType.After, roarExitInstructions);
            
            // Mark the label so we branch here
            c.MarkLabel(afterRoarExitLabel);
        } catch (Exception e) {
            Logger.Error($"Could not change BridgeLever#OnOpenBridge IL: \n{e}");
        }
    }
}
