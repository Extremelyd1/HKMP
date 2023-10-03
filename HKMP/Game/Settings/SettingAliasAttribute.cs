using System;

namespace Hkmp.Game.Settings;

/// <summary>
/// Attribute to define aliases to settings properties/fields.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class SettingAliasAttribute : Attribute {
    /// <summary>
    /// The string array containing the aliases.
    /// </summary>
    public string[] Aliases { get; private set; }

    /// <summary>
    /// Constructs the attribute with the given alias strings.
    /// </summary>
    /// <param name="aliases">One or more strings containing aliases.</param>
    public SettingAliasAttribute(params string[] aliases) {
        Aliases = aliases;
    }
}