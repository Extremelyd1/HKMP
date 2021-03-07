using System;
using HKMP.UI.Component;
using HKMP.UI.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace HKMP.UI {
    public class SettingsEntry<T> {
        private readonly IInputComponent _input;
        private readonly ICheckboxComponent _checkbox;

        private T _defaultValue;
        private bool _doubleLine;

        public SettingsEntry(GameObject parent, Vector2 position, string name, T defaultValue, bool doubleLine = false) {
            _defaultValue = defaultValue;
            _doubleLine = doubleLine;

            new TextComponent(
                parent,
                position + new Vector2(50, doubleLine ? -20 : 0),
                new Vector2(300, doubleLine ? 40 : 30),
                name,
                FontManager.GetFont(UIManager.TrajanProName),
                18,
                alignment: TextAnchor.LowerLeft
            );

            if (typeof(T) == typeof(int)) {
                _input = new InputComponent(
                    parent,
                    position - new Vector2(0, 35 + (doubleLine ? 25 : 0)),
                    new Vector2(200, 30),
                    defaultValue.ToString(),
                    "",
                    TextureManager.GetTexture("input_field_background"),
                    FontManager.GetFont(UIManager.TrajanProName),
                    18,
                    InputField.CharacterValidation.Integer
                );
                
                new TextComponent(
                    parent,
                    position - new Vector2(0, 60 + (doubleLine ? 25 : 0)),
                    new Vector2(200, 30),
                    "default value: " + defaultValue,
                    FontManager.GetFont(UIManager.TrajanProName),
                    alignment: TextAnchor.MiddleLeft
                );
            } else if (typeof(T) == typeof(bool)) {
                if (defaultValue is bool defaultChecked) {
                    _checkbox = new CheckboxComponent(
                        parent,
                        position - new Vector2(90, 30 + (doubleLine ? 25 : 0)),
                        new Vector2(20, 20),
                        defaultChecked,
                        TextureManager.GetTexture("toggle_background"),
                        TextureManager.GetTexture("checkmark")
                    );
                }

                new TextComponent(
                    parent,
                    position - new Vector2(-40, 30 + (doubleLine ? 25 : 0)),
                    new Vector2(200, 20),
                    "default value: " + defaultValue,
                    FontManager.GetFont(UIManager.TrajanProName),
                    alignment: TextAnchor.MiddleLeft
                );
            }
        }

        public T GetValue() {
            if (typeof(T) == typeof(int)) {
                if (!int.TryParse(_input.GetInput(), out var intValue)) {
                    return _defaultValue;
                }

                if (intValue is T value) {
                    return value;
                }
            } else if (typeof(T) == typeof(bool)) {
                var boolValue = _checkbox.IsToggled();
                if (boolValue is T value) {
                    return value;
                }
            }

            throw new Exception("Could not get value of SettingsEntry");
        }

        public bool IsDoubleLine() {
            return _doubleLine;
        }

    }
}