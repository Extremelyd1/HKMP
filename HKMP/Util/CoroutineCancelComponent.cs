using System.Collections.Generic;
using UnityEngine;

namespace HKMP.Util {
    public class CoroutineCancelComponent : MonoBehaviour {

        private Dictionary<string, Coroutine> _activeCoroutines;
        
        public void Awake() {
            _activeCoroutines = new Dictionary<string, Coroutine>();
        }

        public void AddCoroutine(string id, Coroutine coroutine) {
            if (_activeCoroutines.ContainsKey(id)) {
                CancelCoroutine(id);
            }
            
            _activeCoroutines.Add(id, coroutine);
        }

        public void CancelCoroutine(string id) {
            if (!_activeCoroutines.ContainsKey(id)) {
                return;
            }
            
            StopCoroutine(_activeCoroutines[id]);
            _activeCoroutines.Remove(id);
        }

    }
}