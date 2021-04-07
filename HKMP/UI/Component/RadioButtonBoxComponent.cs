using HKMP.UI.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace HKMP.UI.Component {
    public class RadioButtonBoxComponent : Component, IRadioButtonBoxComponent {
        private const int TextWidth = 200;

        private readonly ToggleGroup _toggleGroup;
        private readonly Toggle[] _toggles;

        private readonly int _defaultValue;
        
        private int _activeIndex;
        private OnValueChange _onValueChange;

        public RadioButtonBoxComponent(
            GameObject parent,
            Vector2 position,
            Vector2 size,
            string[] labels,
            int defaultValue
        ) : base (parent, position, size) {
            _defaultValue = defaultValue;
            
            var tempGameObject = new GameObject();
            tempGameObject.transform.SetParent(parent.transform);
            tempGameObject.SetActive(true);
            
            _toggleGroup = tempGameObject.AddComponent<ToggleGroup>();
            _toggleGroup.allowSwitchOff = false;

            _toggles = new Toggle[labels.Length];

            _activeIndex = defaultValue;
            
            for (var i = 0; i < labels.Length; i++) {
                var label = labels[i];

                new TextComponent(
                    parent,
                    position,
                    new Vector2(TextWidth, 30),
                    label,
                    FontManager.UIFontRegular,
                    18,
                    alignment: TextAnchor.LowerLeft
                );

                var checkboxComponent = new CheckboxComponent(
                    parent,
                    position + new Vector2(90, 0),
                    new Vector2(20, 20),
                    i == defaultValue,
                    TextureManager.RadioBackground,
                    TextureManager.RadioFilled
                );
                var toggle = checkboxComponent.ToggleComponent;
                toggle.group = _toggleGroup;
                _toggleGroup.RegisterToggle(toggle);
                _toggles[i] = toggle;

                var index = i;
                
                checkboxComponent.SetOnToggle(value => {
                    if (value) {
                        OnClicked(index);
                    }
                });

                position -= new Vector2(0, 40);
            }
        }

        private void OnClicked(int index) {
            _activeIndex = index;
            _onValueChange?.Invoke(index);
        }

        public void SetOnChange(OnValueChange onValueChange) {
            _onValueChange = onValueChange;
        }

        public int GetActiveIndex() {
            return _activeIndex;
        }

        public void SetInteractable(bool interactable) {
            foreach (var toggle in _toggles) {
                toggle.interactable = interactable;
            }
        }

        public void Reset(bool invokeCallback = false) {
            // Save the callback in a local variable
            OnValueChange onValueChange = null;
            if (!invokeCallback) {
                onValueChange = _onValueChange;
                // Null the original callback so it doesn't get called
                _onValueChange = null;
            }

            // Reset the states of the toggles, since we can't use ToggleGroup for this
            // As soon as the Toggle is deactivated via Unity, it will null the ToggleGroup it belongs to :/
            for (var i = 0; i < _toggles.Length; i++) {
                _toggles[i].isOn = i == _defaultValue;
            }

            if (!invokeCallback) {
                // Reset the callback, so it can get called again
                _onValueChange = onValueChange;
            }
        }
    }
}