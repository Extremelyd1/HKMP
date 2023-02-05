namespace Hkmp.Ui.Component;

/// <summary>
/// Delegate for when a checkbox is toggled.
/// </summary>
internal delegate void OnToggle(bool newValue);

/// <summary>
/// A checkbox component that can be toggled on and off.
/// </summary>
internal interface ICheckboxComponent : IComponent {
    /// <summary>
    /// Whether the checkbox is toggled on or off.
    /// </summary>
    bool IsToggled { get; }

    /// <summary>
    /// Set the action that is executed when toggled.
    /// </summary>
    /// <param name="onToggle">The action to execute.</param>
    void SetOnToggle(OnToggle onToggle);

    /// <summary>
    /// Set whether this checkbox is toggled.
    /// </summary>
    /// <param name="newValue">The toggle value.</param>
    void SetToggled(bool newValue);

    /// <summary>
    /// Set whether the checkbox is interactable.
    /// </summary>
    /// <param name="interactable">Whether the checkbox is interactable.</param>
    void SetInteractable(bool interactable);
}
