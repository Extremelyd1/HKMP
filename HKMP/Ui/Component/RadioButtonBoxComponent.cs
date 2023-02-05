using Hkmp.Ui.Resources;
using UnityEngine;

namespace Hkmp.Ui.Component;

/// <inheritdoc cref="IRadioButtonBoxComponent" />
internal class RadioButtonBoxComponent : Component, IRadioButtonBoxComponent {
    /// <summary>
    /// The default width of the entire box.
    /// </summary>
    private const float BoxWidth = 240f;

    /// <summary>
    /// The height of the header text.
    /// </summary>
    private const float HeaderHeight = 25f;

    /// <summary>
    /// The margin of the header with the buttons.
    /// </summary>
    private const float HeaderButtonMargin = 14f;

    /// <summary>
    /// The size of the buttons.
    /// </summary>
    private const float ButtonSize = 30f;

    /// <summary>
    /// The margin of the buttons with the text.
    /// </summary>
    private const float ButtonTextMargin = 10f;

    /// <summary>
    /// The index of the default box.
    /// </summary>
    private readonly int _defaultValue;

    /// <summary>
    /// The text component for the header text.
    /// </summary>
    private readonly TextComponent _headerTextComponent;

    /// <summary>
    /// An array of checkbox component that serve as radio buttons.
    /// </summary>
    private readonly CheckboxComponent[] _checkboxes;

    /// <summary>
    /// An array of text component as the text next to the radio buttons.
    /// </summary>
    private readonly TextComponent[] _textComponents;

    /// <summary>
    /// The index of the currently active radio button.
    /// </summary>
    private int _activeIndex;

    /// <summary>
    /// Delegate that is executed when the value changes.
    /// </summary>
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

    /// <summary>
    /// Callback method for when a radio button is clicked.
    /// </summary>
    /// <param name="index">The index of the clicked radio button.</param>
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

    /// <inheritdoc />
    public void SetOnChange(OnValueChange onValueChange) {
        _onValueChange = onValueChange;
    }

    /// <inheritdoc />
    public int GetActiveIndex() {
        return _activeIndex;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void Reset(bool invokeCallback = false) {
        for (var i = 0; i < _checkboxes.Length; i++) {
            _checkboxes[i].SetToggled(i == _defaultValue);
        }
    }
}
