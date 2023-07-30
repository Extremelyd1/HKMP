using System;
using System.Collections.Generic;
using Hkmp.Logging;
using HutongGames.PlayMaker;
using MonoMod.RuntimeDetour;
using UnityEngine.SceneManagement;

namespace Hkmp.Game.Client.Entity.Action; 

/// <summary>
/// Static class for registering callbacks on the "OnEnter" method of an <see cref="FsmStateAction"/> class.
/// </summary>
internal static class FsmActionHooks {
    /// <summary>
    /// Dictionary mapping types (subtypes of <see cref="FsmStateAction"/>) to an hook class.
    /// </summary>
    private static readonly Dictionary<Type, FsmActionHook> TypeEvents;

    /// <summary>
    /// List of all registered hooks. Used to loop over and remove all.
    /// </summary>
    // ReSharper disable once CollectionNeverQueried.Local
    private static readonly List<Hook> Hooks;

    static FsmActionHooks() {
        TypeEvents = new Dictionary<Type, FsmActionHook>();
        Hooks = new List<Hook>();
    }

    /// <summary>
    /// Initialize this class by registering the scene changed event.
    /// </summary>
    public static void Initialize() {
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
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

            Hooks.Add(new Hook(
                onEnterMethodInfo,
                OnActionEntered
            ));

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
    /// Callback method for when the scene changes, used to reset all hooks.
    /// </summary>
    /// <param name="oldScene">The old scene.</param>
    /// <param name="newScene">The new scene.</param>
    private static void OnSceneChanged(Scene oldScene, Scene newScene) {
        foreach (var actionHook in TypeEvents.Values) {
            actionHook.Clear();
        }
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

        /// <summary>
        /// Clear all subscriptions to the hook event.
        /// </summary>
        public void Clear() {
            HookEvent = null;
        }
    }
}
