using System;
using System.Reflection;
using Hkmp.Logging;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using UnityEngine.Audio;

namespace Hkmp.Game.Client;

// TODO: create method for de-registering the hooks
/// <summary>
/// Static class that manages and exposes custom hooks that are not possible with On hooks or ModHooks. Uses IL modification
/// to embed event calls in certain methods.
/// </summary>
public static class CustomHooks {
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
    private static ILHook _heroControllerEnterSceneIlHook;
    /// <summary>
    /// IL Hook instance for the HeroController Respawn hook.
    /// </summary>
    private static ILHook _heroControllerRespawnIlHook;

    /// <summary>
    /// Event for when the player object is done being transformed (changed position, scale) after entering a scene.
    /// </summary>
    public static event Action AfterEnterSceneHeroTransformed;

    /// <summary>
    /// Event for when the AudioManager.ApplyMusicCue method is called from the ApplyMusicCue FSM action.
    /// </summary>
    public static event Action<ApplyMusicCue> ApplyMusicCueFromFsmAction;

    /// <summary>
    /// Event for when the AudioMixerSnapshot.TransitionTo method is called from the TransitionToAudioSnapshot FSM
    /// action.
    /// </summary>
    public static event Action<TransitionToAudioSnapshot> TransitionToAudioSnapshotFromFsmAction;

    /// <summary>
    /// Internal event for <see cref="HeroControllerStartAction"/>.
    /// </summary>
    private static event Action HeroControllerStartActionInternal;
    
    /// <summary>
    /// Event that executes when the HeroController starts or executes its subscriber immediately if the HeroContoller
    /// is already active.
    /// </summary>
    public static event Action HeroControllerStartAction {
        add {
            if (HeroController.UnsafeInstance) {
                value.Invoke();
            }
            
            HeroControllerStartActionInternal += value;
        }

        remove => HeroControllerStartActionInternal -= value;
    }

    /// <summary>
    /// Initialize the class by registering the IL/On hooks.
    /// </summary>
    public static void Initialize() {
        IL.HeroController.Start += HeroControllerOnStart;
        IL.HeroController.EnterSceneDreamGate += HeroControllerOnEnterSceneDreamGate;
        
        var type = typeof(HeroController).GetNestedType("<EnterScene>d__469", BindingFlags);
        _heroControllerEnterSceneIlHook = new ILHook(type.GetMethod("MoveNext", BindingFlags), HeroControllerOnEnterScene);
        
        type = typeof(HeroController).GetNestedType("<Respawn>d__473", BindingFlags);
        _heroControllerRespawnIlHook = new ILHook(type.GetMethod("MoveNext", BindingFlags), HeroControllerOnRespawn);
        
        IL.HutongGames.PlayMaker.Actions.ApplyMusicCue.OnEnter += ApplyMusicCueOnEnter;
        IL.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.OnEnter += TransitionToAudioSnapshotOnEnter;

        On.HeroController.Start += HeroControllerOnStart;
    }

    /// <summary>
    /// IL Hook for the HeroController Start method. Calls an event within the method.
    /// </summary>
    private static void HeroControllerOnStart(ILContext il) {
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
    private static void HeroControllerOnEnterSceneDreamGate(ILContext il) {
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
    private static void HeroControllerOnEnterScene(ILContext il) {
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
    private static void HeroControllerOnRespawn(ILContext il) {
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
    private static void EmitAfterEnterSceneEventHeroInPosition(ILCursor c) {
        c.GotoNext(
            MoveType.After,
            HeroInPositionInstructions
        );

        c.EmitDelegate(() => { AfterEnterSceneHeroTransformed?.Invoke(); });
    }
    
    /// <summary>
    /// IL Hook for the ApplyMusicCue OnEnter method. Calls an event in the method after the ApplyMusicCue call is
    /// made.
    /// </summary>
    private static void ApplyMusicCueOnEnter(ILContext il) {
        try {
            // Create a cursor for this context
            var c = new ILCursor(il);

            // IL_005d: ldc.i4.0
            // IL_005e: callvirt     instance void AudioManager::ApplyMusicCue(class MusicCue, float32, float32, bool)
            c.GotoNext(
                MoveType.After,
                i => i.MatchLdcI4(0),
                i => i.MatchCallvirt(typeof(AudioManager), "ApplyMusicCue")
            );

            // Put the instance of the ApplyMusicCue class onto the stack
            c.Emit(OpCodes.Ldarg_0);

            // Emit a delegate for firing the event with the ApplyMusicCue instance
            c.EmitDelegate<Action<ApplyMusicCue>>(action => { ApplyMusicCueFromFsmAction?.Invoke(action); });
        } catch (Exception e) {
            Logger.Error($"Could not change ApplyMusicCueOnEnter IL: \n{e}");
        }
    }
    
    /// <summary>
    /// IL Hook for the TransitionToAudioSnapshot OnEnter method. Calls an event in the method after the TransitionTo
    /// call is made.
    /// </summary>
    private static void TransitionToAudioSnapshotOnEnter(ILContext il) {
        try {
            // Create a cursor for this context
            var c = new ILCursor(il);

            // IL_0021: callvirt     instance float32 [PlayMaker]HutongGames.PlayMaker.FsmFloat::get_Value()
            // IL_0026: callvirt     instance void [UnityEngine.AudioModule]UnityEngine.Audio.AudioMixerSnapshot::TransitionTo(float32)
            c.GotoNext(
                MoveType.After,
                i => i.MatchCallvirt(typeof(FsmFloat), "get_Value"),
                i => i.MatchCallvirt(typeof(AudioMixerSnapshot), "TransitionTo")
            );

            // Put the instance of the TransitionToAudioSnapshot class onto the stack
            c.Emit(OpCodes.Ldarg_0);

            // Emit a delegate for firing the event with the TransitionToAudioSnapshot instance
            c.EmitDelegate<Action<TransitionToAudioSnapshot>>(action => { TransitionToAudioSnapshotFromFsmAction?.Invoke(action); });
        } catch (Exception e) {
            Logger.Error($"Could not change TransitionToAudioSnapshotOnEnter IL: \n{e}");
        }
    }

    /// <summary>
    /// On hook for when the HeroController starts, so we can invoke our custom event.
    /// </summary>
    private static void HeroControllerOnStart(On.HeroController.orig_Start orig, HeroController self) {
        orig(self);
        HeroControllerStartActionInternal?.Invoke();
    }
}
