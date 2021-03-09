using System;
using HKMP.UI.Component;
using HKMP.UI.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace HKMP.UI {
    public class SettingsUIEntry {
        public const int TextWidth = 300;
        private const int InputWidth = 200;
        private const int InputHeight = 30;
        
        private readonly IInputComponent _input;
        private readonly ICheckboxComponent _checkbox;

        private readonly Type _type;
        private readonly object _defaultValue;
        private readonly Action<object> _applySetting;
        private readonly bool _doubleLine;

        public SettingsUIEntry(GameObject parent, Vector2 position, string name, Type type, object defaultValue, Action<object> applySetting, bool doubleLine = false) {
            _type = type;
            _defaultValue = defaultValue;
            _applySetting = applySetting;
            _doubleLine = doubleLine;

            new TextComponent(
                parent,
                position + new Vector2(50, doubleLine ? -20 : 0),
                new Vector2(TextWidth, doubleLine ? 40 : 30),
                name,
                FontManager.UIFontRegular,
                18,
                alignment: TextAnchor.LowerLeft
            );

            if (type == typeof(int)) {
                _input = new InputComponent(
                    parent,
                    position - new Vector2(0, 35 + (doubleLine ? 25 : 0)),
                    new Vector2(InputWidth, InputHeight),
                    defaultValue.ToString(),
                    "",
                    TextureManager.InputFieldBackground,
                    FontManager.UIFontRegular,
                    18,
                    InputField.CharacterValidation.Integer
                );

                new TextComponent(
                    parent,
                    position - new Vector2(0, 60 + (doubleLine ? 25 : 0)),
                    new Vector2(InputWidth, 20),
                    "default value: " + defaultValue,
                    FontManager.UIFontRegular,
                    alignment: TextAnchor.MiddleLeft
                );
            } else if (type == typeof(bool)) {
                if (defaultValue is bool defaultChecked) {
                    _checkbox = new CheckboxComponent(
                        parent,
                        position - new Vector2(90, 30 + (doubleLine ? 25 : 0)),
                        new Vector2(20, 20),
                        defaultChecked,
                        TextureManager.ToggleBackground,
                        TextureManager.Checkmark
                    );
                }

                new TextComponent(
                    parent,
                    position - new Vector2(-40, 30 + (doubleLine ? 25 : 0)),
                    new Vector2(InputWidth, 20),
                    "default value: " + defaultValue,
                    FontManager.UIFontRegular,
                    alignment: TextAnchor.MiddleLeft
                );
            }
        }

        public void ApplySetting() {
            if (_type == typeof(int)) {
                if (!int.TryParse(_input.GetInput(), out var intValue)) {
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