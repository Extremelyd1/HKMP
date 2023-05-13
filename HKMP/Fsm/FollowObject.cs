using UnityEngine;

namespace Hkmp.Fsm;

/// <summary>
/// MonoBehaviour that adjusts the position of a GameObject to follow a target GameObject with a certain offset.
/// </summary>
internal class FollowObject : MonoBehaviour {
    /// <summary>
    /// The target GameObject to follow.
    /// </summary>
    public GameObject Target { get; set; }

    /// <summary>
    /// The offset from the target the GameObject should have.
    /// </summary>
    public Vector3 Offset { get; set; }

    public void Update() {
        transform.position = Target.transform.position + Offset;
    }
}
