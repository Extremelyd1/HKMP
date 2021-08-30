namespace Hkmp.Math {
    public class Vector2 {
        public static readonly Vector2 Zero = new Vector2(0, 0);

        public float X { get; set; }
        public float Y { get; set; }

        public Vector2(float x, float y) {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj) {
            if (!(obj is Vector2 vector2)) {
                return false;
            }
            
            return Equals(vector2);
        }

        private bool Equals(Vector2 other) {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override int GetHashCode() {
            unchecked {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }
        
        public static bool operator ==(Vector2 lhs, Vector2 rhs) {
            if ((object) lhs == null) {
                return (object) rhs == null;
            }
            
            return lhs.Equals(rhs);
        }
        
        public static bool operator !=(Vector2 lhs, Vector2 rhs) {
            return !(lhs == rhs);
        }
    }
}