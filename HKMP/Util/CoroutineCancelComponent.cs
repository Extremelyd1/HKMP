using System.Collections.Generic;
using UnityEngine;

namespace Hkmp.Util;

/// <summary>
/// Component that track active coroutine on a GameObject so they can be cancelled on demand.
/// </summary>
internal class CoroutineCancelComponent : MonoBehaviour {
    /// <summary>
    /// Dictionary mapping string IDs to coroutines.
    /// </summary>
    private Dictionary<string, Coroutine> _activeCoroutines;

    public void Awake() {
        _activeCoroutines = new Dictionary<string, Coroutine>();
    }

    /// <summary>
    /// Add a coroutine with the given ID.
    /// </summary>
    /// <param name="id">The ID of the coroutine.</param>
    /// <param name="coroutine">The coroutine instance.</param>
    public void AddCoroutine(string id, Coroutine coroutine) {
        if (_activeCoroutines.ContainsKey(id)) {
            CancelCoroutine(id);
        }

        _activeCoroutines.Add(id, coroutine);
    }

    /// <summary>
    /// Cancel the coroutine with the given ID.
    /// </summary>
    /// <param name="id">The ID of the coroutine to cancel.</param>
    public void CancelCoroutine(string id) {
        if (!_activeCoroutines.ContainsKey(id)) {
            return;
        }

        StopCoroutine(_activeCoroutines[id]);
        _activeCoroutines.Remove(id);
    }
}
