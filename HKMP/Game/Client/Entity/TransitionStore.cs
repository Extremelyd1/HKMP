using System.Collections.Generic;
using HutongGames.PlayMaker;

namespace Hkmp.Game.Client.Entity {
    /**
     * A class storing the transitions of a PlayMakerFSM instance. Stores the transitions per state and separately
     * the global transition array.
     */
    public class TransitionStore {
        // Dictionary containing the transitions per state
        public Dictionary<string, FsmTransition[]> StateTransitions { get; }
        // The array of global transitions
        public FsmTransition[] GlobalTransitions { get; set; }

        public TransitionStore() {
            StateTransitions = new Dictionary<string, FsmTransition[]>();
        }
    }
}