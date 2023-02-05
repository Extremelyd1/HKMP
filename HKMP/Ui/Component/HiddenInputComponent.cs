using Hkmp.Ui.Resources;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hkmp.Ui.Component;

/// <summary>
/// An input component that is hidden until the user clicks on it.
/// </summary>
internal class HiddenInputComponent : InputComponent {
    /// <summary>
    /// The text that appears when the input is hidden.
    /// </summary>
    private const string HiddenText = "Hidden";

    /// <summary>
    /// String that stores the current input if it is not displayed.
    /// </summary>
    private string _currentInput;

    /// <summary>
    /// Whether the input is hidden.
    /// </summary>
    private bool _isHidden;

    protected HiddenInputComponent(
        ComponentGroup componentGroup,
        Vector2 position,
        string defaultValue,
        string placeholderText,
        int fontSize,
        InputField.CharacterValidation characterValidation = InputField.CharacterValidation.None
    ) : this(
        componentGroup,
        position,
        new Vector2(DefaultWidth, DefaultHeight),
        defaultValue,
        placeholderText,
        TextureManager.InputFieldBg,
        FontManager.UIFontRegular,
        fontSize,
        characterValidation
    ) {
    }

    private HiddenInputComponent(
        ComponentGroup componentGroup,
        Vector2 position,
        Vector2 size,
        string defaultValue,
        string placeholderText,
        MultiStateSprite bgSprite,
        Font font,
        int fontSize,
        InputField.CharacterValidation characterValidation = InputField.CharacterValidation.None
    ) : base(
        componentGroup,
        position,
        size,
        defaultValue,
        placeholderText,
        bgSprite,
        font,
        fontSize,
        0,
        characterValidation
    ) {
        _isHidden = true;

        _currentInput = defaultValue;

        InputField.text = HiddenText;
        SetTextAlpha(NotInteractableOpacity);

        var eventTrigger = GameObject.GetComponent<EventTrigger>();
        eventTrigger.triggers.Clear();

        AddEventTrigger(eventTrigger, EventTriggerType.PointerEnter, data => {
            if (Interactable) {
                Image.sprite = bgSprite.Hover;
            }
        });
        AddEventTrigger(eventTrigger, EventTriggerType.PointerExit, data => {
            if (Interactable) {
                Image.sprite = bgSprite.Neutral;

                if (!_isHidden) {
                    _currentInput = InputField.text;
                    InputField.text = HiddenText;
                    SetTextAlpha(NotInteractableOpacity);
                }

                _isHidden = true;
            }
        });
        AddEventTrigger(eventTrigger, EventTriggerType.PointerDown, data => {
            if (Interactable) {
                Image.sprite = bgSprite.Active;
                InputField.text = _currentInput;
                SetTextAlpha(1f);

                _isHidden = false;
            }
        });
    }

    /// <inheritdoc />
    public override void SetInput(string input) {
        if (_isHidden) {
            _currentInput = input;
        } else {
            InputField.text = input;
        }
    }

    /// <inheritdoc />
    public override string GetInput() {
        return _isHidden ? _currentInput : InputField.text;
    }
}
