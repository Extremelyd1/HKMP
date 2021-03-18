using System.Collections;
using UnityEngine;

namespace HKMP.Fsm {
    public class PositionInterpolation : MonoBehaviour {

        private const float Duration = 1f / 60f;

        private Coroutine _lastCoroutine;

        private bool _firstUpdate;

        public void Start() {
            _firstUpdate = true;
        }

        public void SetNewPosition(Vector3 newPosition) {
            if (_firstUpdate) {
                transform.position = newPosition;

                _firstUpdate = false;
                return;
            }
            
            if (_lastCoroutine != null) {
                StopCoroutine(_lastCoroutine);
            }
            
            _lastCoroutine = StartCoroutine(LerpPosition(newPosition, Duration));
        }
        
        private IEnumerator LerpPosition(Vector3 targetPosition, float duration) {
            var time = 0f;
            var startPosition = transform.position;

            while (time < duration) {
                transform.position = Vector3.Lerp(startPosition, targetPosition, time / duration);
                time += Time.deltaTime;
                yield return null;
            }
            transform.position = targetPosition;
        }

    }
}