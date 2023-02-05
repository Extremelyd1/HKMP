using System;
using System.Collections;
using Hkmp.Ui.Component;
using Hkmp.Ui.Resources;
using Hkmp.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Hkmp.Ui;

/// <summary>
/// Class for a settings entry in the UI.
/// </summary>
internal class SettingsEntryInterface {
    /// <summary>
    /// The width of the entire entry.
    /// </summary>
    private const float EntryWidth = 240f;

    /// <summary>
    /// The width of input components.
    /// </summary>
    private const float InputWidth = 45f;

    /// <summary>
    /// The height of input components.
    /// </summary>
    private const float InputHeight = 38f;

    /// <summary>
    /// The size of checkboxes.
    /// </summary>
    public const float CheckboxSize = 32f;

    /// <summary>
    /// The text component for the name of the entry.
    /// </summary>
    private readonly TextComponent _text;

    /// <summary>
    /// The input component if it is an input entry.
    /// </summary>
    private readonly IInputComponent _input;

    /// <summary>
    /// The checkbox component if it is an checkbox entry.
    /// </summary>
    private readonly ICheckboxComponent _checkbox;

    /// <summary>
    /// The type of the settings entry.
    /// </summary>
    private readonly Type _type;

    /// <summary>
    /// The default value of the entry.
    /// </summary>
    private readonly object _defaultValue;

    /// <summary>
    /// The action that is executed when the setting is applied.
    /// </summary>
    private readonly Action<object> _applySetting;

    /// <summary>
    /// The coroutine that delays applying the setting if the entry is volatile.
    /// </summary>
    private Coroutine _currentInputWaitApplyRoutine;

    public SettingsEntryInterface(
        ComponentGroup componentGroup,
        Vector2 position,
        string name,
        Type type,
        object defaultValue,
        object currentValue,
        Action<object> applySetting,
        bool autoApply = false
    ) {
        _type = type;
        _defaultValue = defaultValue;
        _applySetting = applySetting;

        float height;
        if (type == typeof(byte)) {
            height = 38f;
        } else if (type == typeof(bool)) {
            height = 32f;
        } else {
            throw new ArgumentException("Type of object is not supported");
        }

        _text = new TextComponent(
            componentGroup,
            position,
            new Vector2(EntryWidth, height),
            name,
            UiManager.NormalFontSize,
            alignment: TextAnchor.MiddleLeft
        );

        if (type == typeof(byte)) {
            _input = new InputComponent(
                componentGroup,
                position + new Vector2(EntryWidth / 2f - InputWidth / 2f, 0f),
                new Vector2(InputWidth, InputHeight),
                currentValue.ToString(),
                "",
                TextureManager.InputFieldBg,
                FontManager.UIFontRegular,
                UiManager.NormalFontSize,
                0,
                InputField.CharacterValidation.Integer
            );

            if (autoApply) {
                _input.SetOnChange(_ => {
                    if (_currentInputWaitApplyRoutine != null) {
                        MonoBehaviourUtil.Instance.StopCoroutine(_currentInputWaitApplyRoutine);
                    }

                    _currentInputWaitApplyRoutine = MonoBehaviourUtil.Instance.StartCoroutine(InputWaitApply());
                });
            }
            // TODO: make the constructor parameter "autoApply" work with integer input
        } else if (type == typeof(bool)) {
            if (currentValue is bool currentChecked) {
                _checkbox = new CheckboxComponent(
                    componentGroup,
                    position + new Vector2(EntryWidth / 2f - CheckboxSize / 2f, 0f),
                    new Vector2(CheckboxSize, CheckboxSize),
                    currentChecked,
                    TextureManager.InputFieldBg,
                    TextureManager.CheckBoxToggle
                );

                if (autoApply) {
                    _checkbox.SetOnToggle(_ => { ApplySetting(); });
                }
            }
        }
    }

    /// <summary>
    /// Coroutine for waiting before apply the setting.
    /// </summary>
    /// <returns>The enumerator for this coroutine.</returns>
    private IEnumerator InputWaitApply() {
        yield return new WaitForSeconds(2f);

        ApplySetting();
    }

    /// <summary>
    /// Apply the setting and execute the callback.
    /// </summary>
    /// <exception cref="Exception">Thrown if the value of the entry could not be retrieved.</exception>
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
            _applySetting(_checkbox.IsToggled);
            return;
        }

        throw new Exception("Could not get value of SettingsEntry");
    }

    /// <summary>
    /// Set whether this settings entry is interactable.
    /// </summary>
    /// <param name="interactable">Whether the entry is interactable.</param>
    public void SetInteractable(bool interactable) {
        if (_type == typeof(byte)) {
            _input.SetInteractable(interactable);
        } else if (_type == typeof(bool)) {
            _checkbox.SetInteractable(interactable);
        }

        var color = _text.GetColor();
        if (interactable) {
            color.a = 1f;
        } else {
            color.a = 0.5f;
        }

        _text.SetColor(color);
    }
}
