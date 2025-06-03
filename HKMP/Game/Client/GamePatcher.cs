using System;
using System.Reflection;
using GlobalEnums;
using Hkmp.Networking.Client;
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
    /// The NetClient instance to check if we are connected to a server.
    /// </summary>
    private readonly NetClient _netClient;

    /// <summary>
    /// The IL Hook for the bridge lever method.
    /// </summary>
    private ILHook _bridgeLeverIlHook;

    public GamePatcher(NetClient netClient) {
        _netClient = netClient;
    }
    
    /// <summary>
    /// Register the hooks.
    /// </summary>
    public void RegisterHooks() {
        // Register IL hook for changing the behaviour of tink effects
        IL.TinkEffect.OnTriggerEnter2D += TinkEffectOnTriggerEnter2D;
        
        IL.HealthManager.Invincible += HealthManagerOnInvincible;
        
        IL.HealthManager.TakeDamage += HealthManagerOnTakeDamage;

        On.BridgeLever.OnTriggerEnter2D += BridgeLeverOnTriggerEnter2D;

        var type = typeof(BridgeLever).GetNestedType("<OpenBridge>d__13", BindingFlags);
        _bridgeLeverIlHook = new ILHook(type.GetMethod("MoveNext", BindingFlags), BridgeLeverOnOpenBridge);
        
        On.HutongGames.PlayMaker.Actions.CallMethodProper.DoMethodCall += CallMethodProperOnDoMethodCall;
        
        IL.CameraLockArea.IsInApplicableGameState += CameraLockAreaOnIsInApplicableGameState;

        On.IgnoreHeroCollision.Ignore += IgnoreHeroCollisionOnIgnore;
    }

    /// <summary>
    /// De-register the hooks.
    /// </summary>
    public void DeregisterHooks() {
        IL.TinkEffect.OnTriggerEnter2D -= TinkEffectOnTriggerEnter2D;
        
        IL.HealthManager.Invincible -= HealthManagerOnInvincible;
        
        IL.HealthManager.TakeDamage -= HealthManagerOnTakeDamage;
        
        On.BridgeLever.OnTriggerEnter2D -= BridgeLeverOnTriggerEnter2D;
        
        _bridgeLeverIlHook?.Dispose();
        
        On.HutongGames.PlayMaker.Actions.CallMethodProper.DoMethodCall -= CallMethodProperOnDoMethodCall;
        
        IL.CameraLockArea.IsInApplicableGameState -= CameraLockAreaOnIsInApplicableGameState;
        
        On.IgnoreHeroCollision.Ignore -= IgnoreHeroCollisionOnIgnore;
    }
    
    /// <summary>
    /// IL hook to change the TinkEffect OnTriggerEnter2D to not trigger certain effects of it on remote players.
    /// This method will insert IL to check whether the player responsible for the attack is the local player and
    /// based on this, omit certain effects.
    /// </summary>
    private void TinkEffectOnTriggerEnter2D(ILContext il) {
        try {
            // Create a cursor for this context
            var c = new ILCursor(il);

            // Load the 'collision' argument onto the stack
            c.Emit(OpCodes.Ldarg_1);

            // Keep track of a whether the local player is responsible for the hit
            var isLocalPlayer = true;
            
            // Emit a delegate that pops the collision argument from the stack, checks whether the parent
            // of the collider is the knight and changes the above bool based on this
            c.EmitDelegate<Action<Collider2D>>(collider => {
                var parent = collider.transform.parent;
                if (parent == null) {
                    isLocalPlayer = true;
                    return;
                }

                parent = parent.parent;
                if (parent == null) {
                    isLocalPlayer = true;
                    return;
                }

                isLocalPlayer = parent.gameObject.name == "Knight";
            });

            // Define a label to branch to after the camera shake call
            var afterCameraShakeLabel = c.DefineLabel();

            // Goto before the camera shake call, which is after the 'if' instructions
            c.GotoNext(
                MoveType.After,
                i => i.MatchLdfld(typeof(TinkEffect), "gameCam"),
                i => i.MatchCall(typeof(UnityEngine.Object), "op_Implicit"),
                i => i.MatchBrfalse(out _)
            );

            // Emit the 'isLocalPlayer' bool to the stack
            c.EmitDelegate(() => isLocalPlayer);

            // Emit an instruction that branches to after the camera shake based on the bool
            c.Emit(OpCodes.Brfalse, afterCameraShakeLabel);

            // Goto after the camera shake call
            c.GotoNext(
                MoveType.After,
                i => i.MatchLdfld(typeof(GameCameras), "cameraShakeFSM"),
                i => i.MatchLdstr("EnemyKillShake"),
                i => i.MatchCallvirt(typeof(PlayMakerFSM), "SendEvent")
            );

            // Mark the label for branching here
            c.MarkLabel(afterCameraShakeLabel);
            
            // Goto after setting the 'position2' local variable
            c.GotoNext(
                MoveType.After,
                i => i.MatchCallvirt(typeof(Component), "get_transform"),
                i => i.MatchCallvirt(typeof(Transform), "get_position"),
                i => i.MatchStloc(3)
            );

            // Define a label for branching
            var afterRemotePositionLabel = c.DefineLabel();

            // Emit the 'isLocalPlayer' bool to the stack
            c.EmitDelegate(() => isLocalPlayer);

            // Emit the instruction for branching to behind the setting of the remote player position (below)
            c.Emit(OpCodes.Brtrue, afterRemotePositionLabel);

            // Load the 'collision' argument onto the stack
            c.Emit(OpCodes.Ldarg_1);
            // Emit a delegate that pops the 'collision' argument from the stack and pushes the position of the
            // remote player to the stack
            c.EmitDelegate<Func<Collider2D, Vector3>>(collider => collider.transform.parent.parent.position);

            // Emit the instruction for pushing the stack value into the 'position2' local variable
            c.Emit(OpCodes.Stloc, 3);

            // Mark the label for branching after setting the remote position, that we skip when the local player
            // is responsible for the hit
            c.MarkLabel(afterRemotePositionLabel);

            // Loop 3 times for the 3 'Recoil' method calls to HeroController
            for (var i = 0; i < 3; i++) {
                // Goto before the 'Recoil' method call
                c.GotoNext(
                    MoveType.Before, 
                    inst => inst.MatchCall(typeof(HeroController), "get_instance")
                );

                // Define a label for branching
                var afterRecoilLabel = c.DefineLabel();

                // Emit the 'isLocalPlayer' bool to the stack
                c.EmitDelegate(() => isLocalPlayer);
                // Emit the instruction for branching after the 'Recoil' call
                c.Emit(OpCodes.Brfalse, afterRecoilLabel);

                // Goto after the FSM 'SendEvent' call
                c.GotoNext(
                    MoveType.After,
                    inst => inst.MatchCallvirt(typeof(PlayMakerFSM), "SendEvent")
                );

                // Mark the label for branching here
                c.MarkLabel(afterRecoilLabel);
            }

            // Goto after the last if statement that checks for the 'sendFSMEvent' variable
            c.GotoNext(
                MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld(typeof(TinkEffect), "sendFSMEvent"),
                i => i.MatchBrfalse(out _)
            );

            // Define a label to branch to
            var afterSendEventLabel = c.DefineLabel();
            
            // Emit the 'isLocalPlayer' bool to the stack
            c.EmitDelegate(() => isLocalPlayer);
            // Emit the instruction for branching after the 'SendEvent' call in case of a remote player hit
            c.Emit(OpCodes.Brfalse, afterSendEventLabel);
            
            // Goto after the 'SendEvent' call to mark the label
            c.GotoNext(
                MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld(typeof(TinkEffect), "FSMEvent"),
                i => i.MatchCallvirt(typeof(PlayMakerFSM), "SendEvent")
            );

            // Mark the label to branch to here
            c.MarkLabel(afterSendEventLabel);
        } catch (Exception e) {
            Logger.Error($"Could not change TinkEffect#OnTriggerEnter2D IL:\n{e}");
        }
    }
    
    private void HealthManagerOnInvincible(ILContext il) {
        try {
            // Create a cursor for this context
            var c = new ILCursor(il);

            // Load the 'hitInstance' argument onto the stack
            c.Emit(OpCodes.Ldarg_1);

            // Keep track of a whether the local player is responsible for the hit
            var isLocalPlayer = true;
            
            // Emit a delegate that pops the collision argument from the stack, checks whether the parent
            // of the collider is the knight and changes the above bool based on this
            c.EmitDelegate<Action<HitInstance>>(hitInstance => {
                if (hitInstance.Source == null) {
                    isLocalPlayer = true;
                    return;
                }
                
                var parent = hitInstance.Source.transform.parent;
                if (parent == null) {
                    isLocalPlayer = true;
                    return;
                }

                parent = parent.parent;
                if (parent == null) {
                    isLocalPlayer = true;
                    return;
                }

                isLocalPlayer = parent.gameObject.name == "Knight";
            });
            
            c.GotoNext(
                MoveType.Before,
                i => i.MatchLdarg(1),
                i => i.MatchLdfld(typeof(HitInstance), "AttackType"),
                i => i.MatchBrtrue(out _)
            );
            
            var afterRecoilFreezeShakeLabel = c.DefineLabel();

            c.EmitDelegate(() => isLocalPlayer);
            c.Emit(OpCodes.Brfalse, afterRecoilFreezeShakeLabel);

            c.GotoNext(
                MoveType.After,
                i => i.MatchLdfld(typeof(GameCameras), "cameraShakeFSM"),
                i => i.MatchLdstr("EnemyKillShake"),
                i => i.MatchCallvirt(typeof(PlayMakerFSM), "SendEvent")
            );

            c.MarkLabel(afterRecoilFreezeShakeLabel);
        } catch (Exception e) {
            Logger.Error($"Could not change HealthManager#OnInvincible IL:\n{e}");
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
                i => i.MatchCallvirt(typeof(Component), "get_gameObject"),
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
    
    /// <summary>
    /// Hook for the 'DoMethodCall' method in the 'CallMethodProper' FSM action. This is used for the Crystal Shot
    /// game object to ensure that knockback is not applied to the local player if a remote player hits the crystal.
    /// </summary>
    private void CallMethodProperOnDoMethodCall(
        On.HutongGames.PlayMaker.Actions.CallMethodProper.orig_DoMethodCall orig, 
        HutongGames.PlayMaker.Actions.CallMethodProper self
    ) {
        // If the 'behaviour' and 'methodName' strings do not match, we execute the original method and return
        if (self.behaviour.Value != "HeroController" ||
            self.methodName.Value != "RecoilLeft" &&
            self.methodName.Value != "RecoilRight" &&
            self.methodName.Value != "RecoilDown" &&
            self.methodName.Value != "Bounce"
        ) {
            orig(self);
            return;
        }

        // If the state does not match, we return as well
        if (!self.State.Name.StartsWith("No Box ") && !self.State.Name.StartsWith("Blocked ")) {
            orig(self);
            return;
        }

        Transform attacks;

        // Find either the 'Damager' or the 'Collider' game object from the FSM variables, and check up the hierarchy.
        // If it matches the local player's object, then we know it was the local player's slash and not a remote
        // player's slash
        var damager = self.Fsm.Variables.FindFsmGameObject("Damager");
        var collider = self.Fsm.Variables.FindFsmGameObject("Collider");
        if (damager != null && damager.Value != null) {
            attacks = damager.Value.transform.parent;
        } else if (collider != null && collider.Value != null && collider.Value.transform.parent != null) {
            attacks = collider.Value.transform.parent.parent;
        } else {
            orig(self);
            return;
        }

        if (attacks == null) {
            orig(self);
            return;
        }

        var knight = attacks.parent;
        if (knight == null) {
            orig(self);
            return;
        }

        if (knight.name.Equals("Knight")) {
            orig(self);
        }
    }
    
    /// <summary>
    /// IL Hook for the 'IsInApplicableGameState' method in 'CameraLockArea'. This is used to add a check
    /// for being in the pause menu and connected to a server. Otherwise, the camera will sometimes not lock while
    /// in the pause menu during host transfers.
    /// </summary>
    private void CameraLockAreaOnIsInApplicableGameState(ILContext il) {
        try {
            // Create a cursor for this context
            var c = new ILCursor(il);

            // Goto after the first 'gameState' check in the method
            // IL_0011: ldloc.0      // unsafeInstance
            // IL_0012: ldfld        valuetype GlobalEnums.GameState GameManager::gameState
            // IL_0017: ldc.i4.4
            // IL_0018: beq.s        IL_0024
            c.GotoNext(
                MoveType.After,
                i => i.MatchLdloc(0),
                i => i.MatchLdfld(typeof(global::GameManager), "gameState"),
                i => i.MatchLdcI4(4),
                i => i.MatchBeq(out _)
            );

            // Define a label for branching to if the conditions fail
            var afterChecksLabel = c.DefineLabel();

            // Load GameManager 'unsafeInstance' onto evaluation stack
            c.Emit(OpCodes.Ldloc, 0);
            // Check if the game state is paused and push a boolean onto the stack based on the result
            c.EmitDelegate<Func<global::GameManager, bool>>(gm => gm.gameState == GameState.PAUSED);
            // If the game state is not paused, we branch to the last check in the method
            c.Emit(OpCodes.Brfalse, afterChecksLabel);

            // Check if the NetClient is connected and push a boolean onto the stack based on the result
            c.EmitDelegate(() => _netClient.IsConnected);
            // If we are not connected, we branch to the last check in the method
            c.Emit(OpCodes.Brfalse, afterChecksLabel);

            var returnTrueLabel = c.DefineLabel();

            // If the previous two checks succeeded, we branch to the return true label
            c.Emit(OpCodes.Br, returnTrueLabel);

            // Mark the label after our new checks but before the last check of the method
            c.MarkLabel(afterChecksLabel);
            
            // Goto before the 'return true' IL so we can branch here if our checks succeed 
            // IL_0024: ldc.i4.1
            // IL_0025: ret
            c.GotoNext(
                MoveType.Before,
                i => i.MatchLdcI4(1),
                i => i.MatchRet()
            );

            // Mark the label for return true here
            c.MarkLabel(returnTrueLabel);
        } catch (Exception e) {
            Logger.Error($"Could not change CameraLockArea#IsInApplicableGameState IL: \n{e}");
        }
    }
    
    // TODO: this is a temporary solution and requires further investigation on why this happens
    /// <summary>
    /// Hook for the 'Ignore' method in the 'IgnoreHeroCollision' MonoBehaviour. This is simply a hook that doesn't do
    /// anything, but prevents NRE's happening in the method do to it now being redirected by MonoMod.
    /// </summary>
    private void IgnoreHeroCollisionOnIgnore(On.IgnoreHeroCollision.orig_Ignore orig, IgnoreHeroCollision self) {
        orig(self);
    }
}
