namespace Hkmp.Math;

/// <summary>
/// Class for two dimensional vectors.
/// </summary>
public class Vector2 {
    /// <summary>
    /// The zero (0, 0) vector.
    /// </summary>
    public static readonly Vector2 Zero = new Vector2(0, 0);

    /// <summary>
    /// The X coordinate of this vector.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// The Y coordinate of this vector.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Construct a vector with the given X and Y values.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public Vector2(float x, float y) {
        X = x;
        Y = y;
    }

    /// <inheritdoc />
    public override bool Equals(object obj) {
        if (!(obj is Vector2 vector2)) {
            return false;
        }

        return Equals(vector2);
    }

    /// <summary>
    /// Determines whether the given vector is equal to the current instance.
    /// </summary>
    /// <param name="other">The vector to compare with the current vector.</param>
    /// <returns>true if the given vector is equal to the current object; otherwise, false.</returns>
    private bool Equals(Vector2 other) {
        if (other == null) {
            return false;
        }

        return X.Equals(other.X) && Y.Equals(other.Y);
    }

    /// <inheritdoc />
    public override int GetHashCode() {
        unchecked {
            return (X.GetHashCode() * 397) ^ Y.GetHashCode();
        }
    }

    /// <summary>
    /// Determines whether the given vectors are equal.
    /// </summary>
    /// <param name="lhs">The left-hand side to compare.</param>
    /// <param name="rhs">The right-hand side to compare.</param>
    /// <returns>true if the given vectors are equal; otherwise, false.</returns>
    public static bool operator ==(Vector2 lhs, Vector2 rhs) {
        if ((object) lhs == null) {
            return (object) rhs == null;
        }

        return lhs.Equals(rhs);
    }

    /// <summary>
    /// Determines whether the given vector are not equal.
    /// </summary>
    /// <param name="lhs">The left-hand side to compare.</param>
    /// <param name="rhs">The right-hand side to compare.</param>
    /// <returns>true if the given vectors are not equal; otherwise, false.</returns>
    public static bool operator !=(Vector2 lhs, Vector2 rhs) {
        return !(lhs == rhs);
    }

    /// <summary>
    /// Explicit conversion from a UnityEngine.Vector2 to a Hkmp.Math.Vector2.
    /// </summary>
    /// <param name="vector2">The UnityEngine.Vector2 to convert.</param>
    /// <returns>The converted Hkmp.Math.Vector2.</returns>
    public static explicit operator Vector2(UnityEngine.Vector2 vector2) {
        return new Vector2(vector2.x, vector2.y);
    }

    /// <summary>
    /// Explicit conversion from a Hkmp.Math.Vector2 to a UnityEngine.Vector2.
    /// </summary>
    /// <param name="vector2">The Hkmp.Math.Vector2 to convert.</param>
    /// <returns>The converted UnityEngine.Vector2.</returns>
    public static explicit operator UnityEngine.Vector2(Vector2 vector2) {
        return new UnityEngine.Vector2(vector2.X, vector2.Y);
    }
}
