using System;
using System.Collections;
using System.Linq;
using Hkmp.Game.Client.Entity.Action;
using Hkmp.Util;
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
        "init pause"
    };

    /// <summary>
    /// Array of types of actions that should be skipped during initialization. 
    /// </summary>
    private static readonly Type[] ToSkipTypes = {
        typeof(Tk2dPlayAnimation)
    };

    /// <summary>
    /// Initialize the FSM of a client entity by finding initialize states and executing the actions in those states.
    /// </summary>
    /// <param name="fsm">The FSM to initialize.</param>
    public static void InitializeFsm(PlayMakerFSM fsm) {
        // Check for all states whether they are initialize states
        foreach (var state in fsm.FsmStates) {
            if (!InitStateNames.Contains(state.Name.ToLower())) {
                continue;
            }

            // Go over each action and try to execute it by applying empty data to it
            foreach (var action in state.Actions) {
                if (!action.Enabled) {
                    continue;
                }

                if (ToSkipTypes.Contains(action.GetType())) {
                    continue;
                }
                
                if (EntityFsmActions.SupportedActionTypes.Contains(action.GetType())) {
                    if (action.Fsm == null) {
                        Logger.Debug("Initializing FSM and action.Fsm is null, starting coroutine");

                        MonoBehaviourUtil.Instance.StartCoroutine(WaitForActionInitialization());
                        IEnumerator WaitForActionInitialization() {
                            while (action.Fsm == null) {
                                yield return new WaitForSeconds(0.1f);
                            }
                            
                            Logger.Debug("Initializing FSM action completed");
                            
                            EntityFsmActions.ApplyNetworkDataFromAction(null, action);                            
                        }

                        continue;
                    }
                    
                    EntityFsmActions.ApplyNetworkDataFromAction(null, action);
                }
            }
        }
    }
    
}
