using HKMP.UI.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace HKMP.UI.Component {
    public class RadioButtonBoxComponent : Component, IRadioButtonBoxComponent {
        private const int TextWidth = 200;

        private readonly ToggleGroup _toggleGroup;
        private readonly Toggle[] _toggles;

        private int _activeIndex;
        private OnValueChange _onValueChange;

        public RadioButtonBoxComponent(
            GameObject parent,
            Vector2 position,
            Vector2 size,
            string[] labels,
            int defaultValue
        ) : base (parent, position, size) {
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
            Logger.Info(this, $"Radio button clicked, index: {index}");

            SetActiveIndex(index, true);
        }

        public void SetOnChange(OnValueChange onValueChange) {
            _onValueChange = onValueChange;
        }

        public int GetActiveIndex() {
            return _activeIndex;
        }

        public void SetActiveIndex(int index, bool invokeCallback = false) {
            _toggleGroup.NotifyToggleOn(_toggles[index]);

            // If this was already the active radio button, we skip invoking the callback
            if (_activeIndex != index) {
                _activeIndex = index;

                if (invokeCallback) {
                    _onValueChange?.Invoke(index);
                }
            }
        }
    }
}