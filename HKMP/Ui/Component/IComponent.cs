using UnityEngine;

namespace Hkmp.Ui.Component;

/// <summary>
/// Base class for UI components.
/// </summary>
public interface IComponent {
    /// <summary>
    /// Set whether the group of this component is active.
    /// </summary>
    /// <param name="groupActive">Whether the group is active.</param>
    void SetGroupActive(bool groupActive);

    /// <summary>
    /// Set whether this component is active.
    /// </summary>
    /// <param name="active">Whether this component is active.</param>
    void SetActive(bool active);

    /// <summary>
    /// Get the position of the component.
    /// </summary>
    /// <returns>A Vector2 representing the position.</returns>
    Vector2 GetPosition();

    /// <summary>
    /// Set the position of this component.
    /// </summary>
    /// <param name="position">Vector2 representing the position.</param>
    void SetPosition(Vector2 position);

    /// <summary>
    /// Get the size of this component.
    /// </summary>
    /// <returns>Vector2 representing the size.</returns>
    Vector2 GetSize();
}
