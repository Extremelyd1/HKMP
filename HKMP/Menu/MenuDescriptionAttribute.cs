using System;

namespace Hkmp.Menu;

/// <summary>
/// Attribute to define a description for entries in the mod menu.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class MenuDescriptionAttribute : Attribute {
    /// <summary>
    /// The description that the entry should show as on the mod menu.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Constructs the attribute with the given description.
    /// </summary>
    /// <param name="description">The description as a string.</param>
    public MenuDescriptionAttribute(string description) {
        Description = description;
    }
}
