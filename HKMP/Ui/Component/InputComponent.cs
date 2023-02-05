using System;
using Hkmp.Ui.Resources;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hkmp.Ui.Component;

/// <inheritdoc cref="IInputComponent" />
internal class InputComponent : Component, IInputComponent {
    /// <summary>
    /// The default width of the component.
    /// </summary>
    protected const float DefaultWidth = 240f;

    /// <summary>
    /// The default height of the component.
    /// </summary>
    public const float DefaultHeight = 38f;

    /// <summary>
    /// The margin of the text with the borders of the component.
    /// </summary>
    private const float TextMargin = 5f;

    /// <summary>
    /// The background sprites.
    /// </summary>
    private readonly MultiStateSprite _bgSprite;

    /// <summary>
    /// The Unity InputField component. 
    /// </summary>
    protected readonly InputField InputField;

    /// <summary>
    /// The Unity Text component.
    /// </summary>
    protected readonly Text Text;

    /// <summary>
    /// The Unity Image component.
    /// </summary>
    protected readonly Image Image;

    /// <summary>
    /// Whether this input field is interactable.
    /// </summary>
    protected bool Interactable;

    /// <summary>
    /// The action to execute when the input changes.
    /// </summary>
    private Action<string> _onChange;

    public InputComponent(
        ComponentGroup componentGroup,
        Vector2 position,
        string defaultValue,
        string placeholderText,
        int characterLimit = 0,
        InputField.CharacterValidation characterValidation = InputField.CharacterValidation.None,
        InputField.OnValidateInput onValidateInput = null
    ) : this(
        componentGroup,
        position,
        new Vector2(DefaultWidth, DefaultHeight),
        defaultValue,
        placeholderText,
        TextureManager.InputFieldBg,
        FontManager.UIFontRegular,
        UiManager.NormalFontSize,
        characterLimit,
        characterValidation,
        onValidateInput
    ) {
    }

    public InputComponent(
        ComponentGroup componentGroup,
        Vector2 position,
        Vector2 size,
        string defaultValue,
        string placeholderText,
        MultiStateSprite bgSprite,
        Font font,
        int fontSize,
        int characterLimit = 0,
        InputField.CharacterValidation characterValidation = InputField.CharacterValidation.None,
        InputField.OnValidateInput onValidateInput = null
    ) : base(componentGroup, position, size) {
        _bgSprite = bgSprite;

        Interactable = true;

        // Create background image
        Image = GameObject.AddComponent<Image>();
        Image.sprite = bgSprite.Neutral;
        Image.type = Image.Type.Sliced;

        var placeholder = new GameObject();
        placeholder.AddComponent<RectTransform>().sizeDelta = size;
        var placeholderTextComponent = placeholder.AddComponent<Text>();
        placeholderTextComponent.text = placeholderText;
        placeholderTextComponent.font = font;
        placeholderTextComponent.fontSize = fontSize;
        placeholderTextComponent.alignment = TextAnchor.MiddleCenter;
        // Make the color white with opacity so it is clearly different from inputted text
        placeholderTextComponent.color = new Color(1f, 1f, 1f, 0.5f);

        // Set the transform parent to the InputComponent gameObject
        placeholder.transform.SetParent(GameObject.transform, false);
        Object.DontDestroyOnLoad(placeholder);

        var textObject = new GameObject();
        textObject.AddComponent<RectTransform>().sizeDelta = size - new Vector2(TextMargin * 2f, 0f);
        Text = textObject.AddComponent<Text>();
        Text.text = defaultValue;
        Text.font = font;
        Text.fontSize = fontSize;
        Text.alignment = TextAnchor.MiddleCenter;
        Text.color = Color.white;

        // Set the transform parent to the InputComponent gameObject
        textObject.transform.SetParent(GameObject.transform, false);

        Object.DontDestroyOnLoad(textObject);

        // Create the actual inputField component
        InputField = GameObject.AddComponent<InputField>();
        InputField.targetGraphic = Image;
        InputField.placeholder = placeholderTextComponent;
        InputField.textComponent = Text;
        InputField.text = defaultValue;
        InputField.characterValidation = characterValidation;
        InputField.characterLimit = characterLimit;

        if (onValidateInput != null) {
            InputField.onValidateInput += onValidateInput;
        }

        InputField.shouldActivateOnSelect = false;
        InputField.onValueChanged.AddListener(value => { _onChange?.Invoke(value); });

        var eventTrigger = GameObject.AddComponent<EventTrigger>();

        AddEventTrigger(eventTrigger, EventTriggerType.PointerEnter, data => {
            if (Interactable) {
                Image.sprite = bgSprite.Hover;
            }
        });
        AddEventTrigger(eventTrigger, EventTriggerType.PointerExit, data => {
            if (Interactable) {
                Image.sprite = bgSprite.Neutral;
            }
        });
        AddEventTrigger(eventTrigger, EventTriggerType.PointerDown, data => {
            if (Interactable) {
                Image.sprite = bgSprite.Active;
            }
        });
    }

    /// <summary>
    /// Sets the alpha value of the text.
    /// </summary>
    /// <param name="alpha">The float alpha.</param>
    protected void SetTextAlpha(float alpha) {
        var color = Text.color;
        color.a = alpha;
        Text.color = color;
    }

    /// <inheritdoc />
    public void SetInteractable(bool interactable) {
        Interactable = interactable;

        InputField.interactable = interactable;

        if (interactable) {
            Image.sprite = _bgSprite.Neutral;
            SetTextAlpha(1f);
        } else {
            Image.sprite = _bgSprite.Disabled;
            SetTextAlpha(NotInteractableOpacity);
        }
    }

    /// <inheritdoc />
    public virtual void SetInput(string input) {
        InputField.text = input;
    }

    /// <inheritdoc />
    public virtual string GetInput() {
        return InputField.text;
    }

    /// <inheritdoc />
    public void SetOnChange(Action<string> onChange) {
        _onChange = onChange;
    }
}
