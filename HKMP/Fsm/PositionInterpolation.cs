using System.Collections;
using UnityEngine;

namespace HKMP.Fsm {
    public class PositionInterpolation : MonoBehaviour {

        private const float Duration = 1f / 60f;

        private Coroutine _lastCoroutine;

        public void SetNewPosition(Vector3 newPosition) {
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