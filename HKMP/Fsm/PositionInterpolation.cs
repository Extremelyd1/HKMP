#define no_interpolation

#if !no_interpolation
using System.Collections;
#endif
using UnityEngine;

namespace Hkmp.Fsm;

/// <summary>
/// MonoBehaviour for interpolating position between position updates.
/// </summary>
internal class PositionInterpolation : MonoBehaviour {
#if !no_interpolation
        /// <summary>
        /// The duration between interpolation of positions.
        /// </summary>
        private const float Duration = 1f / 60f;

        /// <summary>
        /// The last coroutine for interpolation.
        /// </summary>
        private Coroutine _lastCoroutine;

        /// <summary>
        /// Whether this is the first update.
        /// </summary>
        private bool _firstUpdate;

        public void Start() {
            _firstUpdate = true;
        }
#endif

    /// <summary>
    /// Set the new position to interpolate to.
    /// </summary>
    /// <param name="newPosition">The new position as Vector3.</param>
    public void SetNewPosition(Vector3 newPosition) {
#if no_interpolation
        transform.localPosition = newPosition;
#else
            if (_firstUpdate) {
                transform.localPosition = newPosition;

                _firstUpdate = false;
                return;
            }

            if (_lastCoroutine != null) {
                StopCoroutine(_lastCoroutine);
            }

            _lastCoroutine = StartCoroutine(LerpPosition(newPosition, Duration));
        }

        /// <summary>
        /// Lerp the position of this instance to the target position over the given duration.
        /// </summary>
        /// <param name="targetPosition">The target position as Vector3.</param>
        /// <param name="duration">The duration as float.</param>
        /// <returns>An enumerator for this coroutine.</returns>
        private IEnumerator LerpPosition(Vector3 targetPosition, float duration) {
            var time = 0f;
            var startPosition = transform.localPosition;

            while (time < duration) {
                transform.localPosition = Vector3.Lerp(startPosition, targetPosition, time / duration);
                time += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = targetPosition;
#endif
    }
}
