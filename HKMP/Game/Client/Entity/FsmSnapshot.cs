using System.Collections.Generic;
using UnityEngine;

namespace Hkmp.Game.Client.Entity; 

/// <summary>
/// Snapshot of FSM that includes current state and current values for all FSM variables.
/// Used to check for any changes in state/variables to network with the server.
/// </summary>
internal class FsmSnapshot {
    /// <summary>
    /// The name of the current (or last) state of the FSM.
    /// </summary>
    public string CurrentState { get; set; }
    
    /// <summary>
    /// Dictionary of names of float variables and corresponding (current/last) value.
    /// </summary>
    public Dictionary<string, float> Floats { get; }
    /// <summary>
    /// Dictionary of names of int variables and corresponding (current/last) value.
    /// </summary>
    public Dictionary<string, int> Ints { get; }
    /// <summary>
    /// Dictionary of names of bool variables and corresponding (current/last) value.
    /// </summary>
    public Dictionary<string, bool> Bools { get; }
    /// <summary>
    /// Dictionary of names of string variables and corresponding (current/last) value.
    /// </summary>
    public Dictionary<string, string> Strings { get; }
    /// <summary>
    /// Dictionary of names of vector2 variables and corresponding (current/last) value.
    /// </summary>
    public Dictionary<string, Vector2> Vector2s { get; }
    /// <summary>
    /// Dictionary of names of vector3 variables and corresponding (current/last) value.
    /// </summary>
    public Dictionary<string, Vector3> Vector3s { get; }

    /// <summary>
    /// Construct the snapshot by initializing all dictionaries.
    /// </summary>
    public FsmSnapshot() {
        Floats = new Dictionary<string, float>();
        Ints = new Dictionary<string, int>();
        Bools = new Dictionary<string, bool>();
        Strings = new Dictionary<string, string>();
        Vector2s = new Dictionary<string, Vector2>();
        Vector3s = new Dictionary<string, Vector3>();
    }
}
