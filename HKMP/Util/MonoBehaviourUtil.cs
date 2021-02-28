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
    }
}