using Hkmp.Ui.Resources;
using UnityEngine;

namespace Hkmp.Ui.Component {
    public class RadioButtonBoxComponent : Component, IRadioButtonBoxComponent {
        private const float BoxWidth = 240f;
        private const float HeaderHeight = 25f;
        private const float HeaderButtonMargin = 14f;
        private const float ButtonSize = 30f;
        private const float ButtonTextMargin = 10f;

        private readonly int _defaultValue;
        private readonly TextComponent _headerTextComponent;
        private readonly CheckboxComponent[] _checkboxes;
        private readonly TextComponent[] _textComponents;

        private int _activeIndex;
        private OnValueChange _onValueChange;

        public RadioButtonBoxComponent(
            ComponentGroup componentGroup,
            Vector2 position,
            string headerLabel,
            string[] labels,
            int defaultValue
        ) : base(componentGroup, position, Vector2.zero) {
            _defaultValue = defaultValue;
            _activeIndex = defaultValue;

            _headerTextComponent = new TextComponent(
                componentGroup,
                position,
                new Vector2(BoxWidth, HeaderHeight),
                headerLabel,
                UiManager.NormalFontSize,
                alignment: TextAnchor.MiddleLeft
            );

            position -= new Vector2(0, HeaderHeight + HeaderButtonMargin);

            _checkboxes = new CheckboxComponent[labels.Length];
            _textComponents = new TextComponent[labels.Length];

            for (var i = 0; i < labels.Length; i++) {
                var label = labels[i];

                var checkboxComponent = _checkboxes[i] = new CheckboxComponent(
                    componentGroup,
                    position + new Vector2(-BoxWidth / 2f + ButtonSize / 2f, 0f),
                    new Vector2(ButtonSize, ButtonSize),
                    i == defaultValue,
                    TextureManager.RadioButtonBg,
                    TextureManager.RadioButtonToggle,
                    false
                );

                var index = i;
                checkboxComponent.SetOnToggle(value => {
                    if (value) {
                        OnClicked(index);
                    }
                });
                
                _textComponents[i] = new TextComponent(
                    componentGroup,
                    position + new Vector2((ButtonSize + ButtonTextMargin) / 2f, 0f),
                    new Vector2(BoxWidth - ButtonSize - ButtonTextMargin, ButtonSize),
                    label,
                    UiManager.NormalFontSize,
                    alignment: TextAnchor.MiddleLeft
                );

                position -= new Vector2(0, ButtonSize + 8f);
            }
        }

        private void OnClicked(int index) {
            for (var i = 0; i < _checkboxes.Length; i++) {
                if (i == index) {
                    continue;
                }
                
                _checkboxes[i].SetToggled(false);
            }
            
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
            var color = _headerTextComponent.GetColor();
            color.a = interactable ? 1f : NotInteractableOpacity;
            _headerTextComponent.SetColor(color);
            
            foreach (var checkbox in _checkboxes) {
                checkbox.SetInteractable(interactable);
            }

            foreach (var textComponent in _textComponents) {
                color = textComponent.GetColor();
                color.a = interactable ? 1f : NotInteractableOpacity;
                textComponent.SetColor(color);
            }
        }

        public void Reset(bool invokeCallback = false) {
            for (var i = 0; i < _checkboxes.Length; i++) {
                _checkboxes[i].SetToggled(i == _defaultValue);
            }
        }
    }
}