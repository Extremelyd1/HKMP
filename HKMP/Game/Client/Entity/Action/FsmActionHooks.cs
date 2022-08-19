using System;
using System.Collections.Generic;
using Hkmp.Logging;
using HutongGames.PlayMaker;
using MonoMod.RuntimeDetour;

namespace Hkmp.Game.Client.Entity.Action; 

internal static class FsmActionHooks {
    private static readonly Dictionary<Type, FsmActionHook> TypeEvents;

    static FsmActionHooks() {
        TypeEvents = new Dictionary<Type, FsmActionHook>();
    }

    public static void RegisterFsmStateActionType(Type type, Action<FsmStateAction> action) {
        if (!TypeEvents.TryGetValue(type, out var fsmActionHook)) {
            fsmActionHook = new FsmActionHook();

            var onEnterMethodInfo = type.GetMethod("OnEnter");

            // TODO: check if we need to keep track of hook
            // ReSharper disable once ObjectCreationAsStatement
            new Hook(
                onEnterMethodInfo,
                OnActionEntered
            );

            TypeEvents.Add(type, fsmActionHook);
        }

        fsmActionHook.HookEvent += action;
    }

    private static void OnActionEntered(Action<FsmStateAction> orig, FsmStateAction self) {
        orig(self);
        
        if (!TypeEvents.TryGetValue(self.GetType(), out var fsmActionHook)) {
            Logger.Warn("Hook was fired but no associated hook class was found");
            return;
        }

        fsmActionHook.InvokeEvent(self);
    }

    private class FsmActionHook {
        public event Action<FsmStateAction> HookEvent;

        public void InvokeEvent(FsmStateAction fsmStateAction) {
            HookEvent?.Invoke(fsmStateAction);
        }
    }
}