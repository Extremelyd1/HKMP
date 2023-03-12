namespace Hkmp.Math;

/// <summary>
/// Class for three dimensional vectors.
/// </summary>
public class Vector3 {
    /// <summary>
    /// The X coordinate of this vector.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// The Y coordinate of this vector.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// The Z coordinate of this vector.
    /// </summary>
    public float Z { get; set; }

    /// <summary>
    /// Construct a vector with the given X, Y and Z values.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    public Vector3(float x, float y, float z) {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Explicit conversion from a UnityEngine.Vector3 to a Hkmp.Math.Vector3.
    /// </summary>
    /// <param name="vector3">The UnityEngine.Vector3 to convert.</param>
    /// <returns>The converted Hkmp.Math.Vector3.</returns>
    public static explicit operator Vector3(UnityEngine.Vector3 vector3) {
        return new Vector3(vector3.x, vector3.y, vector3.z);
    }

    /// <summary>
    /// Explicit conversion from a Hkmp.Math.Vector3 to a UnityEngine.Vector3.
    /// </summary>
    /// <param name="vector3">The Hkmp.Math.Vector3 to convert.</param>
    /// <returns>The converted UnityEngine.Vector3.</returns>
    public static explicit operator UnityEngine.Vector3(Vector3 vector3) {
        return new UnityEngine.Vector3(vector3.X, vector3.Y, vector3.Z);
    }
}
