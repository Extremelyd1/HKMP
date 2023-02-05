using UnityEngine;

namespace Hkmp.Ui.Component;

/// <summary>
/// A component displaying text.
/// </summary>
internal interface ITextComponent : IComponent {
    /// <summary>
    /// Set the displayed text.
    /// </summary>
    /// <param name="text">The string text.</param>
    void SetText(string text);

    /// <summary>
    /// Set the color of the text.
    /// </summary>
    /// <param name="color">The color.</param>
    void SetColor(Color color);
}
