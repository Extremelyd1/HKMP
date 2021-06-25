using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hkmp.Ui.Component {
    public class HiddenButtonLeaveHandler : MonoBehaviour, IPointerExitHandler {
        public Action Action { private get; set; }

        public void OnPointerExit(PointerEventData eventData) {
            Action.Invoke();
        }
    }
}