using Hkmp.Ui.Resources;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hkmp.Ui.Component;

/// <inheritdoc cref="ICheckboxComponent" />
internal class CheckboxComponent : Component, ICheckboxComponent {
    /// <summary>
    /// The GameObject for the checkmark.
    /// </summary>
    private readonly GameObject _checkmarkObject;

    /// <summary>
    /// The Unity Image component for the background image.
    /// </summary>
    private readonly Image _bgImage;

    /// <summary>
    /// The Unity Image component for the checkmark image.
    /// </summary>
    private readonly Image _checkmarkImage;

    /// <summary>
    /// The background sprites.
    /// </summary>
    private readonly MultiStateSprite _bgSprite;

    /// <summary>
    /// Whether this checkbox can be toggled off.
    /// </summary>
    private readonly bool _canToggleOff;

    /// <summary>
    /// The delegate that is executed when the checkbox is toggled. 
    /// </summary>
    private OnToggle _onToggle;

    /// <summary>
    /// Whether this checkbox is interactable.
    /// </summary>
    private bool _interactable;

    /// <summary>
    /// Whether the checkbox is toggled on.
    /// </summary>
    private bool _isToggled;

    /// <inheritdoc />
    public bool IsToggled {
        get => _isToggled;
        set {
            _isToggled = value;

            _checkmarkObject.SetActive(value);
        }
    }

    public CheckboxComponent(
        ComponentGroup componentGroup,
        Vector2 position,
        Vector2 size,
        bool defaultValue,
        MultiStateSprite bgSprite,
        Sprite checkSprite,
        bool canToggleOff = true
    ) : base(componentGroup, position, size) {
        _bgSprite = bgSprite;
        _canToggleOff = canToggleOff;

        _interactable = true;
        _isToggled = defaultValue;

        // Create background object with image
        var backgroundObject = new GameObject();
        backgroundObject.AddComponent<RectTransform>().sizeDelta = size;
        backgroundObject.AddComponent<CanvasRenderer>();

        _bgImage = backgroundObject.AddComponent<Image>();
        _bgImage.sprite = bgSprite.Neutral;
        _bgImage.type = Image.Type.Sliced;

        backgroundObject.transform.SetParent(GameObject.transform, false);

        // Create checkmark object with image
        _checkmarkObject = new GameObject();
        _checkmarkObject.AddComponent<RectTransform>().sizeDelta = size;

        _checkmarkImage = _checkmarkObject.AddComponent<Image>();
        _checkmarkImage.sprite = checkSprite;
        _checkmarkImage.type = Image.Type.Sliced;

        _checkmarkObject.transform.SetParent(GameObject.transform, false);
        _checkmarkObject.SetActive(defaultValue);

        var eventTrigger = GameObject.AddComponent<EventTrigger>();
        var isMouseDown = false;
        var isHover = false;

        AddEventTrigger(eventTrigger, EventTriggerType.PointerEnter, data => {
            isHover = true;

            if (_interactable) {
                _bgImage.sprite = bgSprite.Hover;
            }
        });
        AddEventTrigger(eventTrigger, EventTriggerType.PointerExit, data => {
            isHover = false;
            if (_interactable && !isMouseDown) {
                _bgImage.sprite = bgSprite.Neutral;
            }
        });
        AddEventTrigger(eventTrigger, EventTriggerType.PointerDown, data => {
            isMouseDown = true;

            if (_interactable) {
                _bgImage.sprite = bgSprite.Active;
            }
        });
        AddEventTrigger(eventTrigger, EventTriggerType.PointerUp, data => {
            isMouseDown = false;

            if (_interactable) {
                if (isHover) {
                    _bgImage.sprite = bgSprite.Hover;
                    OnToggle();
                } else {
                    _bgImage.sprite = bgSprite.Neutral;
                }
            }
        });
    }

    /// <summary>
    /// Callback method for when the user clicks the checkbox.
    /// </summary>
    private void OnToggle() {
        if (_isToggled) {
            if (_canToggleOff) {
                IsToggled = false;

                _onToggle?.Invoke(false);
            }
        } else {
            IsToggled = true;

            _onToggle?.Invoke(true);
        }
    }

    /// <inheritdoc />
    public void SetOnToggle(OnToggle onToggle) {
        _onToggle = onToggle;
    }

    /// <inheritdoc />
    public void SetToggled(bool newValue) {
        IsToggled = newValue;
    }

    /// <inheritdoc />
    public void SetInteractable(bool interactable) {
        _interactable = interactable;

        var color = _checkmarkImage.color;
        if (interactable) {
            _bgImage.sprite = _bgSprite.Neutral;

            color.a = 1f;
        } else {
            _bgImage.sprite = _bgSprite.Disabled;

            color.a = NotInteractableOpacity;
        }

        _checkmarkImage.color = color;
    }
}
