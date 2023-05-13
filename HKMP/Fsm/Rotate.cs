using UnityEngine;

namespace Hkmp.Fsm;

/// <summary>
/// MonoBehaviour for rotating a GameObject.
/// </summary>
internal class Rotate : MonoBehaviour {
    /// <summary>
    /// The X coordinate in the euler rotation.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// The Y coordinate in the euler rotation.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// The Z coordinate in the euler rotation.
    /// </summary>
    public float Z { get; set; }

    /// <summary>
    /// Sets the angles or the euler rotation.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    public void SetAngles(float x, float y, float z) {
        X = x;
        Y = y;
        Z = z;
    }

    public void Update() {
        var eulerAngles = new Vector3(X, Y, Z);

        transform.Rotate(eulerAngles * Time.deltaTime);
    }
}
