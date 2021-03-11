using UnityEngine;

namespace HKMP.Fsm {
    public class FollowObject : MonoBehaviour {
        
        public GameObject GameObject { get; set; }
        public Vector3 Offset { get; set; }

        public void Update() {
            transform.position = GameObject.transform.position + Offset;
        }
        
    }
}