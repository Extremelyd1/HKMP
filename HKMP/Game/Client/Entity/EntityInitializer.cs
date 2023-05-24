using System.Linq;
using Hkmp.Game.Client.Entity.Action;
using Hkmp.Networking.Packet.Data;

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
        "dormant"
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
                if (EntityFsmActions.SupportedActionTypes.Contains(action.GetType())) {
                    var data = new EntityNetworkData();
                    EntityFsmActions.ApplyNetworkDataFromAction(data, action);
                }
            }
        }
    }
    
}
