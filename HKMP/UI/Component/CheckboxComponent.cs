using UnityEngine;
using UnityEngine.UI;

namespace HKMP.UI.Component {
    public class CheckboxComponent : Component, ICheckboxComponent {
        public Toggle ToggleComponent { get; }
        
        private OnToggle _onToggle;
        
        public CheckboxComponent(GameObject parent, Vector2 position, Vector2 size, bool defaultValue, Texture2D backgroundTexture, Texture2D checkTexture) :
            base(parent, position, size) {
            // Create the toggle component
            ToggleComponent = GameObject.AddComponent<Toggle>();
            ToggleComponent.transition = Selectable.Transition.ColorTint;
            ToggleComponent.isOn = defaultValue;
            
            // Create background object with image
            var backgroundObject = new GameObject();
            backgroundObject.AddComponent<RectTransform>().sizeDelta = size;
            backgroundObject.AddComponent<CanvasRenderer>();
            
            var backgroundImage = backgroundObject.AddComponent<Image>();
            backgroundImage.sprite = CreateSpriteFromTexture(backgroundTexture);
            backgroundImage.type = Image.Type.Simple;

            backgroundObject.transform.SetParent(GameObject.transform, false);
            ToggleComponent.targetGraphic = backgroundImage;
            // Dont destroy background object
            Object.DontDestroyOnLoad(backgroundObject);
            
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
            ToggleComponent.graphic = checkmarkImage;
            // Dont destroy background object
            Object.DontDestroyOnLoad(checkmarkObject);
            
            // Finally create the listener for when the checkbox is toggled
            ToggleComponent.onValueChanged.AddListener(newValue => {
                _onToggle?.Invoke(newValue);
            });
        }

        public void SetOnToggle(OnToggle onToggle) {
            _onToggle = onToggle;
        }

        public bool IsToggled() {
            return ToggleComponent.isOn;
        }

        public void SetToggled(bool newValue) {
            ToggleComponent.isOn = newValue;
        }
    }
}