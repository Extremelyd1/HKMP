using UnityEngine;
using UnityEngine.EventSystems;

namespace HKMP.UI.Component {
    public class HiddenButtonLeaveHandler : MonoBehaviour, IPointerExitHandler {
        
        public GameObject DeactivateObject { get; set; }
        public GameObject ActivateObject { get; set; }
        
        public void OnPointerExit(PointerEventData eventData) {
            DeactivateObject.SetActive(false);
            ActivateObject.SetActive(true);
        }
    }
}