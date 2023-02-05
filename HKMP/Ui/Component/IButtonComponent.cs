using System;

namespace Hkmp.Ui.Component;

/// <summary>
/// A UI component for a button.
/// </summary>
internal interface IButtonComponent : IComponent {
    /// <summary>
    /// Set the text on the button.
    /// </summary>
    /// <param name="text">The string text.</param>
    void SetText(string text);

    /// <summary>
    /// Set the action that is executed when pressed.
    /// </summary>
    /// <param name="action">The action.</param>
    void SetOnPress(Action action);

    /// <summary>
    /// Set whether the button is interactable.
    /// </summary>
    /// <param name="interactable">Whether the button is interactable.</param>
    void SetInteractable(bool interactable);
}
