namespace Hkmp.Math {
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
    }
}