using System;
using HutongGames.PlayMaker;

namespace Hkmp.Game.Client.Save; 

/// <summary>
/// Data class for a persistent item in the scene with the corresponding FsmInt/FsmBool.
/// </summary>
internal class PersistentFsmData {
    /// <summary>
    /// The persistent item data with the ID and scene name.
    /// </summary>
    public PersistentItemData PersistentItemData { get; set; }
    
    /// <summary>
    /// The function to get the current integer value. Could be null if a boolean is used instead.
    /// </summary>
    public Func<int> CurrentInt { get; set; }
    /// <summary>
    /// The function to get the current boolean value. Could be null if an integer is used instead.
    /// </summary>
    public Func<bool> CurrentBool { get; set; }
    
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
    public bool IsInt => CurrentInt != null;
}
