using System;

namespace Hkmp.Menu;

/// <summary>
/// Attribute to define a name and description for entries in the mod menu.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ModMenuSettingAttribute : Attribute {
    /// <summary>
    /// The name that the entry should show as on the mod menu.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// The description that the entry should show as on the mod menu.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Constructs the attribute with the given name and no description.
    /// </summary>
    /// <param name="name">The name as a string.</param>
    public ModMenuSettingAttribute(string name) {
        Name = name;
        Description = null;
    }

    /// <summary>
    /// Constructs the attribute with the given name and description.
    /// </summary>
    /// <param name="name">The name as a string.</param>
    /// <param name="description">The description as a string.</param>
    public ModMenuSettingAttribute(string name, string description) {
        Name = name;
        Description = description;
    }
}
