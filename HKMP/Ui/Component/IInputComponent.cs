using System;

namespace Hkmp.Ui.Component;

/// <summary>
/// An component that handles user text input.
/// </summary>
internal interface IInputComponent : IComponent {
    /// <summary>
    /// Set the input of the component.
    /// </summary>
    /// <param name="input">The string input.</param>
    void SetInput(string input);

    /// <summary>
    /// Get the currently input text.
    /// </summary>
    /// <returns>The string input.</returns>
    string GetInput();

    /// <summary>
    /// Set whether this component is interactable.
    /// </summary>
    /// <param name="interactable">Whether the component is interactable.</param>
    void SetInteractable(bool interactable);

    /// <summary>
    /// Set an action that is executed when the input field changes.
    /// </summary>
    /// <param name="onChange">The action to execute.</param>
    void SetOnChange(Action<string> onChange);
}
