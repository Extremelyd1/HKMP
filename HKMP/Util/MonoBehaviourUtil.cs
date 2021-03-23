using System;
using UnityEngine;

namespace HKMP.Util {
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
        
        /**
         * Destroys all children of the given game object
         */
        public static void DestroyAllChildren(GameObject gameObject) {
            for (var i = 0; i < gameObject.transform.childCount; i++) {
                var child = gameObject.transform.GetChild(i);
                Destroy(child.gameObject);
            }
        }
    }
}