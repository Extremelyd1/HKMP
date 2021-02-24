using UnityEngine;
using UnityEngine.UI;

namespace HKMP.UI.Component {
    public class CheckboxComponent : Component, ICheckboxComponent {
        private Toggle _toggleComponent;
        
        private OnToggle _onToggle;
        
        public CheckboxComponent(GameObject parent, Vector2 position, Vector2 size, Texture2D backgroundTexture, Texture2D checkTexture) :
            base(parent, position, size) {
            // Create the toggle component
            _toggleComponent = GameObject.AddComponent<Toggle>();
            _toggleComponent.transition = Selectable.Transition.ColorTint;
            
            // Create background object with image
            var backgroundObject = new GameObject();
            backgroundObject.AddComponent<RectTransform>().sizeDelta = size;
            backgroundObject.AddComponent<CanvasRenderer>();
            
            var backgroundImage = backgroundObject.AddComponent<Image>();
            backgroundImage.sprite = CreateSpriteFromTexture(backgroundTexture);
            backgroundImage.type = Image.Type.Simple;

            backgroundObject.transform.SetParent(GameObject.transform, false);
            _toggleComponent.targetGraphic = backgroundImage;
            // Dont destroy background object
            Object.DontDestroyOnLoad(backgroundObject);
            // TODO: check whether the background image needs its own GameObject
            
            // Create checkmark object with image
            var checkmarkObject = new GameObject();
            checkmarkObject.AddComponent<RectTransform>().sizeDelta = size;
            checkmarkObject.AddComponent<CanvasRenderer>();
            
            var checkmarkImage = checkmarkObject.AddComponent<Image>();
            checkmarkImage.sprite = CreateSpriteFromTexture(checkTexture);
            checkmarkImage.type = Image.Type.Simple;

            checkmarkObject.transform.SetParent(GameObject.transform, false);
            // Set the graphic of the Toggle component to the checkmark image
            // This will ensure that if the checkbox is checked it will display the image
            _toggleComponent.graphic = checkmarkImage;
            // Dont destroy background object
            Object.DontDestroyOnLoad(checkmarkObject);
            
            // Finally create the listener for when the checkbox is toggled
            _toggleComponent.onValueChanged.AddListener(newValue => {
                _onToggle?.Invoke(newValue);
            });
        }

        public void SetOnToggle(OnToggle onToggle) {
            _onToggle = onToggle;
        }

        public bool IsToggled() {
            return _toggleComponent.isOn;
        }
    }
}