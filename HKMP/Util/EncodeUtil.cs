namespace Hkmp.Util;

/// <summary>
/// Static class to help with encoding/decoding values to/from bytes.
/// </summary>
public static class EncodeUtil {
    /// <summary>
    /// Get a single byte for the given array of booleans where each bit represents a boolean from the array.
    /// </summary>
    /// <param name="bits">An array of booleans of at most length 8.</param>
    /// <returns>A byte representing the booleans.</returns>
    public static byte GetByte(bool[] bits) {
        byte result = 0;
        for (var i = 0; i < bits.Length; i++) {
            if (bits[i]) {
                result |= (byte) (1 << i);
            }
        }

        return result;
    }

    /// <summary>
    /// Get a boolean array representing the given byte where each boolean is a bit from the byte.
    /// </summary>
    /// <param name="b">A byte that contains boolean for each bit.</param>
    /// <returns>An boolean array of length 8.</returns>
    public static bool[] GetBoolsFromByte(byte b) {
        var result = new bool[8];
        for (var i = 0; i < result.Length; i++) {
            result[i] = (b & (1 << i)) > 0;
        }

        return result;
    }
}
