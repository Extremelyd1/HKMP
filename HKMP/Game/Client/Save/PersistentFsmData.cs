using System;

namespace Hkmp.Game.Client.Save; 

/// <summary>
/// Data class for a persistent item in the scene with the corresponding FsmInt/FsmBool.
/// </summary>
internal class PersistentFsmData {
    /// <summary>
    /// The persistent item key with the ID and scene name.
    /// </summary>
    public PersistentItemKey PersistentItemKey { get; init; }
    
    /// <summary>
    /// Function to get the current integer value. Could be null if a boolean is used instead.
    /// </summary>
    public Func<int> GetCurrentInt { get; init; }
    /// <summary>
    /// Action to set the current integer value. Could be null if a boolean is used instead.
    /// </summary>
    public Action<int> SetCurrentInt { get; init; }
    /// <summary>
    /// Function to get the current boolean value. Could be null if an integer is used instead.
    /// </summary>
    public Func<bool> GetCurrentBool { get; init; }
    /// <summary>
    /// Action to set the current boolean value. Could be null if an integer is used instead.
    /// </summary>
    public Action<bool> SetCurrentBool { get; init; }
    
    /// <summary>
    /// The last value for the integer if used.
    /// </summary>
    public int LastIntValue { get; set; }
    /// <summary>
    /// The last value for the boolean if used.
    /// </summary>
    public bool LastBoolValue { get; set; }

    /// <summary>
    /// Whether an int is stored for this data.
    /// </summary>
    public bool IsInt => GetCurrentInt != null;
}
