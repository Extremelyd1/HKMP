using System;
using System.Collections.Generic;
using UnityEngine;

namespace HKMP.Util {
    public class ThreadUtil : MonoBehaviour {
        private static readonly object Lock = new object();
        
        private static readonly List<Action> ActionsToRun = new List<Action>();

        public static void Instantiate() {
            var threadUtilObject = new GameObject();
            threadUtilObject.AddComponent<ThreadUtil>();
            DontDestroyOnLoad(threadUtilObject);
        }
        
        public static void RunActionOnMainThread(Action action) {
            lock (Lock) {
                ActionsToRun.Add(action);
            }
        }

        public void Update() {
            lock (Lock) {
                foreach (var action in ActionsToRun) {
                    action.Invoke();
                }
                
                ActionsToRun.Clear();
            }
        }

    }
}