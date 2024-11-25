using System.Collections.Generic;
using Hkmp.Collection;

namespace Hkmp.Util;

/// <summary>
/// Static class to help with encoding/decoding values to/from bytes.
/// </summary>
public static class EncodeUtil {
    /// <summary>
    /// The file path of the embedded resource file for string data.
    /// </summary>
    private const string StringDataFilePath = "Hkmp.Resource.string-data.json";

    /// <summary>
    /// Bi-directional lookup that maps strings (for encoding) to their indices.
    /// </summary>
    private static readonly BiLookup<string, ushort> StringIndices;

    /// <summary>
    /// Static construct to load the scene indices.
    /// </summary>
    static EncodeUtil() {
        StringIndices = new BiLookup<string, ushort>();
        
        var strings = FileUtil.LoadObjectFromEmbeddedJson<List<string>>(StringDataFilePath);
        ushort index = 0;
        foreach (var str in strings) {
            StringIndices.Add(str, index++);
        }
    }
    
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

    /// <summary>
    /// Try to get the string index corresponding to the given string for encoding/decoding purposes.
    /// </summary>
    /// <param name="sceneName">The string.</param>
    /// <param name="index">The index of the string or default if the string could not be found.</param>
    /// <returns>true if there is a corresponding index for the given string, false otherwise.</returns>
    public static bool GetStringIndex(string sceneName, out ushort index) {
        return StringIndices.TryGetValue(sceneName, out index);
    }

    /// <summary>
    /// Try to get the string corresponding to the given string index for encoding/decoding purposes.
    /// </summary>
    /// <param name="index">The string.</param>
    /// <param name="sceneName">The string or default if the string index could not be found.</param>
    /// <returns>true if there is a corresponding string for the given index, false otherwise.</returns>
    public static bool GetStringName(ushort index, out string sceneName) {
        return StringIndices.TryGetValue(index, out sceneName);
    }
}
