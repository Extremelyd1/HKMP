using System;

namespace Hkmp.Menu;

/// <summary>
/// Attribute to define a name for entries in the mod menu.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class MenuNameAttribute : Attribute {
    /// <summary>
    /// The name that the entry should show as on the mod menu.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Constructs the attribute with the given name.
    /// </summary>
    /// <param name="name">The name as a string.</param>
    public MenuNameAttribute(string name) {
        Name = name;
    }
}
