using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;

namespace Hkmp.Util;

/// <summary>
/// Class for FSM extensions.
/// </summary>
public static class FsmUtilExt {
    /// <summary>
    /// Get a FSM action by state name and index.
    /// </summary>
    /// <param name="fsm">The FSM instance.</param>
    /// <param name="stateName">The name of the state.</param>
    /// <param name="index">The index of the action within that state.</param>
    /// <returns>The FsmStateAction from the FSM or null if the action could not be found.</returns>
    public static FsmStateAction GetAction(this PlayMakerFSM fsm, string stateName, int index) {
        foreach (var t in fsm.FsmStates) {
            if (t.Name != stateName) {
                continue;
            }

            var actions = t.Actions;

            Array.Resize(ref actions, actions.Length + 1);

            return actions[index];
        }

        return null;
    }

    /// <summary>
    /// Get a FSM action by state name and index.
    /// </summary>
    /// <param name="fsm">The FSM instance.</param>
    /// <param name="stateName">The name of the state.</param>
    /// <param name="index">The index of the action within that state.</param>
    /// <typeparam name="T">The type of the action that extends FsmStateAction.</typeparam>
    /// <returns>The action from the FSM or null if the action could not be found.</returns>
    public static T GetAction<T>(this PlayMakerFSM fsm, string stateName, int index) where T : FsmStateAction {
        return GetAction(fsm, stateName, index) as T;
    }

    /// <summary>
    /// Get the first FSM action by state name and type.
    /// </summary>
    /// <param name="fsm">The FSM instance.</param>
    /// <param name="stateName">The name of the state.</param>
    /// <typeparam name="T">The type of the action that extends FsmStateAction.</typeparam>
    /// <returns>The action from the FSM or null if the action could not be found.</returns>
    public static T GetFirstAction<T>(this PlayMakerFSM fsm, string stateName) where T : FsmStateAction {
        return fsm.GetState(stateName)?.Actions.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Get a FSM state by its name.
    /// </summary>
    /// <param name="fsm">The FSM instance.</param>
    /// <param name="stateName">The name of the state.</param>
    /// <returns>The state from the FSM or null, if no such state exists.</returns>
    public static FsmState GetState(this PlayMakerFSM fsm, string stateName) {
        return fsm.FsmStates.Where(t => t.Name == stateName)
            .Select(t => new { t, actions = t.Actions })
            .Select(t1 => t1.t)
            .FirstOrDefault();
    }

    /// <summary>
    /// Insert a FSM action in a state at a specific index.
    /// </summary>
    /// <param name="fsm">The FSM instance.</param>
    /// <param name="stateName">The name of the state.</param>
    /// <param name="action">The FSM action to insert.</param>
    /// <param name="index">The index at which to insert the action.</param>
    public static void InsertAction(PlayMakerFSM fsm, string stateName, FsmStateAction action, int index) {
        foreach (FsmState t in fsm.FsmStates) {
            if (t.Name != stateName) continue;
            List<FsmStateAction> actions = t.Actions.ToList();

            actions.Insert(index, action);

            t.Actions = actions.ToArray();
            action.Init(t);
        }
    }

    /// <summary>
    /// Insert a method in a state at a specific index.
    /// </summary>
    /// <param name="fsm">The FSM instance.</param>
    /// <param name="stateName">The name of the state.</param>
    /// <param name="index">The index at which to insert the method.</param>
    /// <param name="method">The method to insert.</param>
    public static void InsertMethod(this PlayMakerFSM fsm, string stateName, int index, Action method) {
        InsertAction(fsm, stateName, new InvokeMethod(method), index);
    }
    
    /// <summary>
    /// Removes an action from a specific state in a FSM.
    /// </summary>
    /// <param name="fsm">The FSM.</param>
    /// <param name="stateName">The name of the state with the action to remove.</param>
    /// <param name="index">The index of the action within the state.</param>
    public static void RemoveAction(this PlayMakerFSM fsm, string stateName, int index) {
        var state = fsm.GetState(stateName);
        
        var origActions = state.Actions;
        var actions = new FsmStateAction[origActions.Length - 1];
        for (var i = 0; i < index; i++) {
            actions[i] = origActions[i];
        }

        for (var i = index; i < actions.Length; i++) {
            actions[i] = origActions[i + 1];
        }

        state.Actions = actions;
    }
}

/// <summary>
/// FSM action that simply invokes a method.
/// </summary>
internal class InvokeMethod : FsmStateAction {
    /// <summary>
    /// The action to execute.
    /// </summary>
    private readonly Action _action;

    /// <summary>
    /// Construct the FSM action with the given action.
    /// </summary>
    /// <param name="a"></param>
    public InvokeMethod(Action a) {
        _action = a;
    }

    /// <inheritdoc />
    public override void OnEnter() {
        _action?.Invoke();
        Finish();
    }
}
