using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hkmp.Game.Client.Entity.Action;
using Hkmp.Util;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Entity; 

/// <summary>
/// Class that manages initializing client-side entities to ensure they have correct references within FSM actions
/// to game objects (such as child objects).
/// </summary>
internal static class EntityInitializer {
    /// <summary>
    /// Array of state names that indicates that it is a initializing state.
    /// </summary>
    private static readonly string[] InitStateNames = {
        "init",
        "initiate",
        "initialise",
        "initialize",
        "dormant",
        "pause",
        "init pause",
        "deparents",
        "opened" // For battle gates
    };

    /// <summary>
    /// Array of types that should be removed from client-side enemies so it doesn't interfere with remote behaviour.
    /// </summary>
    private static readonly Type[] ToRemoveTypes = {
        typeof(Walker),
        typeof(Rigidbody2D),
        typeof(BigCentipede)
    };

    /// <summary>
    /// Array of types of actions that should be skipped during initialization. 
    /// </summary>
    private static readonly Type[] ToSkipTypes = {
        typeof(Tk2dPlayAnimation),
        typeof(ActivateAllChildren)
    };

    /// <summary>
    /// Initialize the FSM of a client entity by finding initialize states and executing the actions in those states.
    /// </summary>
    /// <param name="fsm">The FSM to initialize.</param>
    public static void InitializeFsm(PlayMakerFSM fsm) {
        // Create a list of states to initialize later
        var statesToInit = new List<FsmState>();
        // Keep track of the indices where the individual initialization states begin in our final list
        var indices = new int[InitStateNames.Length];

        // Go over each state in the FSM
        foreach (var state in fsm.FsmStates) {
            var stateName = state.Name.ToLower();
            var index = Array.IndexOf(InitStateNames, stateName);
            // Check if it is a "init" state
            if (index == -1) {
                continue;
            }

            // Then insert it at the correct index according to our tracked indices
            statesToInit.Insert(indices[index], state);
            // Increase all indices that come after, since we inserted something before
            for (var i = index; i < indices.Length; i++) {
                indices[i]++;
            }
        }

        // Now we can loop over the states in the same order as our "InitStateNames" array
        foreach (var state in statesToInit) {
            Logger.Debug($"Found initialization state: {state.Name}, executing actions");

            // The index of the state in the FSM, NOT in the list of states to initialize
            var stateIndex = Array.IndexOf(fsm.FsmStates, state);
            
            // Go over each action and try to execute it by applying empty data to it
            for (var actionIndex = 0; actionIndex < state.Actions.Length; actionIndex++) {
                var action = state.Actions[actionIndex];
                
                if (!action.Enabled) {
                    continue;
                }

                if (ToSkipTypes.Contains(action.GetType())) {
                    continue;
                }

                if (EntityFsmActions.SupportedActionTypes.Contains(action.GetType())) {
                    if (action.Fsm == null) {
                        Logger.Debug("Initializing FSM and action.Fsm is null, starting coroutine");

                        var finalActionIndex = actionIndex;
                        var sceneName = fsm.gameObject.scene.name;
                        var actionFunc = () => fsm.FsmStates[stateIndex].Actions[finalActionIndex];
                        var checkFunc = () => actionFunc.Invoke().Fsm == null;

                        MonoBehaviourUtil.Instance.StartCoroutine(WaitForActionInitialization());
                        IEnumerator WaitForActionInitialization() {
                            while (checkFunc.Invoke()) {
                                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != sceneName) {
                                    yield break;
                                }

                                yield return new WaitForSeconds(0.1f);
                            }
                            
                            Logger.Debug($"Initializing FSM action completed, executing action: {actionFunc.Invoke().GetType()}");
                            
                            EntityFsmActions.ApplyNetworkDataFromAction(null, actionFunc.Invoke());                            
                        }

                        continue;
                    }
                    Logger.Debug($"  Executing action {action.GetType()} for initialization");

                    EntityFsmActions.ApplyNetworkDataFromAction(null, action);
                }
            }
        }
    }

    /// <summary>
    /// Remove all types that should be removed from a client-side entity object.
    /// </summary>
    /// <param name="gameObject">The game object on which to remove the types.</param>
    public static void RemoveClientTypes(GameObject gameObject) {
        foreach (var type in ToRemoveTypes) {
            var component = gameObject.GetComponent(type);
            if (component != null) {
                UnityEngine.Object.Destroy(component);
            }
        }
    }
}
