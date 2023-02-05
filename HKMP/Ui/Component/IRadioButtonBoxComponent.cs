namespace Hkmp.Ui.Component;

/// <summary>
/// Delegate for when the value of the radio buttons changes.
/// </summary>
internal delegate void OnValueChange(int newIndex);

/// <summary>
/// A radio button box component.
/// </summary>
internal interface IRadioButtonBoxComponent {
    /// <summary>
    /// Set a callback method for when the active button changes.
    /// </summary>
    /// <param name="onValueChange">The callback method.</param>
    void SetOnChange(OnValueChange onValueChange);

    /// <summary>
    /// Get the index of the currently active radio button.
    /// </summary>
    /// <returns></returns>
    int GetActiveIndex();

    /// <summary>
    /// Set whether this component is interactable.
    /// </summary>
    /// <param name="interactable">Whether the component is interactable.</param>
    void SetInteractable(bool interactable);

    /// <summary>
    /// Resets the radio box to be the default value.
    /// </summary>
    /// <param name="invokeCallback">Whether to invoke the callback that the value changed.</param>
    void Reset(bool invokeCallback = false);
}
