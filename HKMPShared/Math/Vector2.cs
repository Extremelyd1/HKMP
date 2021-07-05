namespace Hkmp.Math {
    public class Vector2 {
        public static readonly Vector2 Zero = new Vector2(0, 0);

        public float X { get; set; }
        public float Y { get; set; }

        public Vector2(float x, float y) {
            X = x;
            Y = y;
        }
    }
}