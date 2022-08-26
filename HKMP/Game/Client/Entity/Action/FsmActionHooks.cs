using System;
using System.Collections.Generic;
using Hkmp.Logging;
using HutongGames.PlayMaker;
using MonoMod.RuntimeDetour;

namespace Hkmp.Game.Client.Entity.Action; 

/// <summary>
/// Static class for registering callbacks on the "OnEnter" method of an <see cref="FsmStateAction"/> class.
/// </summary>
internal static class FsmActionHooks {
    /// <summary>
    /// Dictionary mapping types (subtypes of <see cref="FsmStateAction"/>) to an hook class.
    /// </summary>
    private static readonly Dictionary<Type, FsmActionHook> TypeEvents;

    static FsmActionHooks() {
        TypeEvents = new Dictionary<Type, FsmActionHook>();
    }

    /// <summary>
    /// Register an action as callback on the "OnEnter" method of an <see cref="FsmStateAction"/> class.
    /// </summary>
    /// <param name="type">The subtype of <see cref="FsmStateAction"/> to register the callback for.</param>
    /// <param name="action">The action that will be called when the "OnEnter" method executes with the instance
    /// as the parameter to the action.</param>
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

    /// <summary>
    /// Callback method on the "OnEnter" method for a specific <see cref="FsmStateAction"/> class.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The instance on which it was called.</param>
    private static void OnActionEntered(Action<FsmStateAction> orig, FsmStateAction self) {
        orig(self);
        
        if (!TypeEvents.TryGetValue(self.GetType(), out var fsmActionHook)) {
            Logger.Warn("Hook was fired but no associated hook class was found");
            return;
        }

        fsmActionHook.InvokeEvent(self);
    }

    /// <summary>
    /// A wrapper class containing an event for all callbacks of a hook.
    /// </summary>
    private class FsmActionHook {
        /// <summary>
        /// Event for the callbacks on the hook.
        /// </summary>
        public event Action<FsmStateAction> HookEvent;

        /// <summary>
        /// Invokes all (if any) callbacks to this hook.
        /// </summary>
        /// <param name="fsmStateAction">The instance on which the hook triggered.</param>
        public void InvokeEvent(FsmStateAction fsmStateAction) {
            HookEvent?.Invoke(fsmStateAction);
        }
    }
}