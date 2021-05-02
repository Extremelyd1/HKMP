using System;
using System.Linq;
using HutongGames.PlayMaker;

namespace HKMP.Util {
    public static class FsmUtilExt {
        public static FsmStateAction GetAction(this PlayMakerFSM fsm, string stateName, int index) {
            foreach (var state in fsm.FsmStates) {
                if (state.Name != stateName) {
                    continue;
                }
                
                var actions = state.Actions;

                Array.Resize(ref actions, actions.Length + 1);

                return actions[index];
            }

            return null;
        }

        public static T GetAction<T>(this PlayMakerFSM fsm, string stateName, int index) where T : FsmStateAction {
            return GetAction(fsm, stateName, index) as T;
        }
        
        public static FsmState GetState(this PlayMakerFSM fsm, string stateName) {
            return fsm.FsmStates.Where(t => t.Name == stateName)
                .Select(t => new {t, actions = t.Actions})
                .Select(t1 => t1.t)
                .FirstOrDefault();
        }
        
        public static void InsertAction(PlayMakerFSM fsm, string stateName, FsmStateAction action, int index) {
            foreach (var state in fsm.FsmStates) {
                if (state.Name != stateName) {
                    continue;
                }
                
                var actions = state.Actions.ToList();

                actions.Insert(index, action);

                state.Actions = actions.ToArray();
                
                action.Init(state);
            }
        }
        
        public static void InsertMethod(this PlayMakerFSM fsm, string stateName, int index, Action method) {
            InsertAction(fsm, stateName, new InvokeMethod(method), index);
        }
        
        public static void RemoveAction(this PlayMakerFSM fsm, string stateName, int index) {
            foreach (var state in fsm.FsmStates) {
                if (state.Name != stateName) {
                    continue;
                }
                
                var actions = state.Actions;

                var action = fsm.GetAction(stateName, index);
                actions = actions.Where(x => x != action).ToArray();

                state.Actions = actions;
            }
        }

        public static void RemoveAction(this PlayMakerFSM fsm, string stateName, Type type) {
            foreach (var state in fsm.FsmStates) {
                if (state.Name != stateName) {
                    continue;
                }
                
                var actions = state.Actions;

                actions = actions.Where(x => x.GetType() != type).ToArray();

                state.Actions = actions;
            }
        }
    }
    
    public class InvokeMethod : FsmStateAction {
        private readonly Action _action;

        public InvokeMethod(Action a) {
            _action = a;
        }
        
        public override void OnEnter() {
            _action?.Invoke();
            Finish();
        }
    }
}