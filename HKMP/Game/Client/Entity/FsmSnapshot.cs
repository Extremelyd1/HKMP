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
    public float[] Floats { set; get; }
    /// <summary>
    /// Dictionary of names of int variables and corresponding (current/last) value.
    /// </summary>
    public int[] Ints { set; get; }
    /// <summary>
    /// Dictionary of names of bool variables and corresponding (current/last) value.
    /// </summary>
    public bool[] Bools { set; get; }
    /// <summary>
    /// Dictionary of names of string variables and corresponding (current/last) value.
    /// </summary>
    public string[] Strings { set; get; }
    /// <summary>
    /// Dictionary of names of vector2 variables and corresponding (current/last) value.
    /// </summary>
    public Vector2[] Vector2s { set; get; }
    /// <summary>
    /// Dictionary of names of vector3 variables and corresponding (current/last) value.
    /// </summary>
    public Vector3[] Vector3s { set; get; }
}
