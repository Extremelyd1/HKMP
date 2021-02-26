using UnityEngine;

namespace HKMP.Util {
    public class CoroutineUtil : MonoBehaviour {
        public static CoroutineUtil Instance;

        public void Awake() {
            if (Instance != null) {
                Destroy(this);
                return;
            }
            
            Instance = this;
        }
    }
}