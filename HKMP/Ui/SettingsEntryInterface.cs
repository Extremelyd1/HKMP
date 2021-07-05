using System;
using Hkmp.Ui.Component;
using Hkmp.Ui.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace Hkmp.Ui {
    public class SettingsEntryInterface {
        public const int TextWidth = 300;
        private const int InputWidth = 200;
        private const int InputHeight = 30;

        private readonly IInputComponent _input;
        private readonly ICheckboxComponent _checkbox;

        private readonly Type _type;
        private readonly object _defaultValue;
        private readonly Action<object> _applySetting;
        private readonly bool _doubleLine;

        public SettingsEntryInterface(
            ComponentGroup componentGroup,
            Vector2 position,
            string name,
            Type type,
            object defaultValue,
            object currentValue,
            Action<object> applySetting,
            bool doubleLine = false,
            bool autoApply = false
        ) {
            _type = type;
            _defaultValue = defaultValue;
            _applySetting = applySetting;
            _doubleLine = doubleLine;

            new TextComponent(
                componentGroup,
                position + new Vector2(50, doubleLine ? -20 : 0),
                new Vector2(TextWidth, doubleLine ? 40 : 30),
                name,
                FontManager.UIFontRegular,
                18,
                alignment: TextAnchor.LowerLeft
            );

            if (type == typeof(byte)) {
                _input = new InputComponent(
                    componentGroup,
                    position - new Vector2(0, 35 + (doubleLine ? 25 : 0)),
                    new Vector2(InputWidth, InputHeight),
                    currentValue.ToString(),
                    "",
                    TextureManager.InputFieldBackground,
                    FontManager.UIFontRegular,
                    18,
                    InputField.CharacterValidation.Integer
                );
                // TODO: make the constructor parameter "autoApply" work with integer input

                new TextComponent(
                    componentGroup,
                    position - new Vector2(0, 60 + (doubleLine ? 25 : 0)),
                    new Vector2(InputWidth, 20),
                    "default value: " + defaultValue,
                    FontManager.UIFontRegular,
                    alignment: TextAnchor.MiddleLeft
                );
            } else if (type == typeof(bool)) {
                if (currentValue is bool currentChecked) {
                    _checkbox = new CheckboxComponent(
                        componentGroup,
                        position - new Vector2(90, 30 + (doubleLine ? 25 : 0)),
                        new Vector2(20, 20),
                        currentChecked,
                        TextureManager.ToggleBackground,
                        TextureManager.Checkmark
                    );

                    if (autoApply) {
                        _checkbox.SetOnToggle(_ => { ApplySetting(); });
                    }
                }

                new TextComponent(
                    componentGroup,
                    position - new Vector2(-40, 30 + (doubleLine ? 25 : 0)),
                    new Vector2(InputWidth, 20),
                    "default value: " + defaultValue,
                    FontManager.UIFontRegular,
                    alignment: TextAnchor.MiddleLeft
                );
            } else {
                throw new ArgumentException("Type of object is not supported");
            }
        }

        public void ApplySetting() {
            if (_type == typeof(byte)) {
                if (!byte.TryParse(_input.GetInput(), out var intValue)) {
                    _applySetting(_defaultValue);
                    return;
                }

                _applySetting(intValue);
                return;
            }

            if (_type == typeof(bool)) {
                _applySetting(_checkbox.IsToggled());
                return;
            }

            throw new Exception("Could not get value of SettingsEntry");
        }

        public bool IsDoubleLine() {
            return _doubleLine;
        }
    }
}