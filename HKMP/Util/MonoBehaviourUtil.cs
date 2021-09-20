using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hkmp.Util {
    public class MonoBehaviourUtil : MonoBehaviour {
        public static MonoBehaviourUtil Instance;

        public event Action OnUpdateEvent;

        public void Awake() {
            if (Instance != null) {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        public void Update() {
            OnUpdateEvent?.Invoke();
        }

        public static void DestroyAllChildren(GameObject gameObject) {
            DestroyAllChildren(gameObject, new List<string>());
        }
        
        /**
         * Destroys all children of the given game object
         */
        public static void DestroyAllChildren(
            GameObject gameObject,
            List<string> exclude) {
            for (var i = 0; i < gameObject.transform.childCount; i++) {
                var child = gameObject.transform.GetChild(i);

                if (exclude.Contains(child.name)) {
                    continue;
                }
                
                Destroy(child.gameObject);
            }
        }
    }
}