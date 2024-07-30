using System;
using System.Reflection;
using Hkmp.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Hkmp.Game.Client;

// TODO: create method for de-registering the hooks
/// <summary>
/// Class that manages and exposes custom hooks that are not possible with On hooks or ModHooks. Uses IL modification
/// to embed event calls in certain methods.
/// </summary>
public class CustomHooks {
    /// <summary>
    /// The binding flags for obtaining certain types for hooking.
    /// </summary>
    private const BindingFlags BindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
    
    /// <summary>
    /// The instruction match set for matching the instructions below. This is the call to HeroInPosition.Invoke.
    /// </summary>
    // IL_01ae: ldloc.1      // V_1
    // IL_01af: ldfld        class HeroController/HeroInPosition HeroController::heroInPosition
    // IL_01b4: ldc.i4.0
    // IL_01b5: callvirt     instance void HeroController/HeroInPosition::Invoke(bool)
    private static readonly Func<Instruction, bool>[] HeroInPositionInstructions = [
        i => i.MatchLdfld(typeof(HeroController), "heroInPosition"),
        i => i.MatchLdcI4(out _),
        i => i.MatchCallvirt(typeof(HeroController.HeroInPosition), "Invoke")
    ];

    /// <summary>
    /// IL Hook instance for the HeroController EnterScene hook.
    /// </summary>
    private ILHook _heroControllerEnterSceneIlHook;
    /// <summary>
    /// IL Hook instance for the HeroController Respawn hook.
    /// </summary>
    private ILHook _heroControllerRespawnIlHook;

    /// <summary>
    /// Event for when the player object is done being transformed (changed position, scale) after entering a scene.
    /// </summary>
    public event Action AfterEnterSceneHeroTransformed;

    /// <summary>
    /// Initialize the class by registering the IL hooks.
    /// </summary>
    public void Initialize() {
        IL.HeroController.Start += HeroControllerOnStart;
        IL.HeroController.EnterSceneDreamGate += HeroControllerOnEnterSceneDreamGate;
        
        var type = typeof(HeroController).GetNestedType("<EnterScene>d__469", BindingFlags);
        _heroControllerEnterSceneIlHook = new ILHook(type.GetMethod("MoveNext", BindingFlags), HeroControllerOnEnterScene);
        
        type = typeof(HeroController).GetNestedType("<Respawn>d__473", BindingFlags);
        _heroControllerRespawnIlHook = new ILHook(type.GetMethod("MoveNext", BindingFlags), HeroControllerOnRespawn);
    }

    /// <summary>
    /// IL Hook for the HeroController Start method. Calls an event within the method.
    /// </summary>
    private void HeroControllerOnStart(ILContext il) {
        try {
            // Create a cursor for this context
            var c = new ILCursor(il);

            EmitAfterEnterSceneEventHeroInPosition(c);
        } catch (Exception e) {
            Logger.Error($"Could not change HeroControllerOnStart IL: \n{e}");
        }
    }

    /// <summary>
    /// IL Hook for the HeroController EnterSceneDreamGate method. Calls an event within the method.
    /// </summary>
    private void HeroControllerOnEnterSceneDreamGate(ILContext il) {
        try {
            // Create a cursor for this context
            var c = new ILCursor(il);

            EmitAfterEnterSceneEventHeroInPosition(c);
        } catch (Exception e) {
            Logger.Error($"Could not change HeroControllerOnEnterSceneDreamGate IL: \n{e}");
        }
    }

    /// <summary>
    /// IL Hook for the HeroController EnterScene method. Calls an event multiple times within the method.
    /// </summary>
    private void HeroControllerOnEnterScene(ILContext il) {
        try {
            // Create a cursor for this context
            var c = new ILCursor(il);

            for (var i = 0; i < 2; i++) {
                EmitAfterEnterSceneEventHeroInPosition(c);
            }

            // IL_0634: ldloc.1      // V_1
            // IL_0635: callvirt     instance void HeroController::FaceRight()
            Func<Instruction, bool>[] faceDirectionInstructions = [
                i => i.MatchLdloc(1),
                i => 
                    i.MatchCallvirt(typeof(HeroController), "FaceRight") || 
                    i.MatchCallvirt(typeof(HeroController), "FaceLeft")
            ];

            for (var i = 0; i < 2; i++) {
                c.GotoNext(
                    MoveType.After,
                    HeroInPositionInstructions
                );
                
                c.GotoNext(
                    MoveType.After,
                    faceDirectionInstructions
                );
                
                c.EmitDelegate(() => { AfterEnterSceneHeroTransformed?.Invoke(); });
            }
            
            EmitAfterEnterSceneEventHeroInPosition(c);
        } catch (Exception e) {
            Logger.Error($"Could not change HeroController#EnterScene IL: \n{e}");
        }
    }

    /// <summary>
    /// IL Hook for the HeroController Respawn method. Calls an event multiple times within the method.
    /// </summary>
    private void HeroControllerOnRespawn(ILContext il) {
        try {
            // Create a cursor for this context
            var c = new ILCursor(il);

            for (var i = 0; i < 2; i++) {
                EmitAfterEnterSceneEventHeroInPosition(c);
            }
        } catch (Exception e) {
            Logger.Error($"Could not change HeroControllerOnRespawn IL: \n{e}");
        }
    }

    /// <summary>
    /// Emit the delegate for calling the <see cref="AfterEnterSceneHeroTransformed"/> event after the
    /// 'HeroInPosition' instructions.
    /// </summary>
    /// <param name="c">The IL cursor on which to match the instructions and emit the delegate.</param>
    private void EmitAfterEnterSceneEventHeroInPosition(ILCursor c) {
        c.GotoNext(
            MoveType.After,
            HeroInPositionInstructions
        );

        c.EmitDelegate(() => { AfterEnterSceneHeroTransformed?.Invoke(); });
    }
}
