using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;

namespace Hkmp.Util {
    public static class FsmUtilExt {
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
            foreach (FsmState t in fsm.FsmStates) {
                if (t.Name != stateName) continue;
                List<FsmStateAction> actions = t.Actions.ToList();

                actions.Insert(index, action);

                t.Actions = actions.ToArray();
            }
        }

        public static void InsertMethod(this PlayMakerFSM fsm, string stateName, int index, Action method) {
            InsertAction(fsm, stateName, new InvokeMethod(method), index);
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