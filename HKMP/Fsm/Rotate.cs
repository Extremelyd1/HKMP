using UnityEngine;

namespace Hkmp.Fsm {
    public class Rotate : MonoBehaviour {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public void SetAngles(float x, float y, float z) {
            X = x;
            Y = y;
            Z = z;
        }

        public void Update() {
            var eulerAngles = new Vector3(X, Y, Z);

            transform.Rotate(eulerAngles * Time.deltaTime);
        }
    }
}