using System.Collections.Generic;
using HutongGames.PlayMaker;

namespace Hkmp.Game.Client.Entity {
    public class TransitionStore {
        public Dictionary<string, FsmTransition[]> StateTransitions { get; }
        public FsmTransition[] GlobalTransitions { get; set; }

        public TransitionStore() {
            StateTransitions = new Dictionary<string, FsmTransition[]>();
        }
    }
}