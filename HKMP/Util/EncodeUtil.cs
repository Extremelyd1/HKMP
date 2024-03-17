using System.Collections.Generic;
using Hkmp.Collection;

namespace Hkmp.Util;

/// <summary>
/// Static class to help with encoding/decoding values to/from bytes.
/// </summary>
public static class EncodeUtil {
    /// <summary>
    /// The file path of the embedded resource file for scene data.
    /// </summary>
    private const string SceneDataFilePath = "Hkmp.Resource.scene-data.json";

    /// <summary>
    /// Bi-directional lookup that maps scene names to their indices.
    /// </summary>
    private static readonly BiLookup<string, ushort> SceneIndices;

    /// <summary>
    /// Static construct to load the scene indices.
    /// </summary>
    static EncodeUtil() {
        SceneIndices = new BiLookup<string, ushort>();
        
        var sceneNames = FileUtil.LoadObjectFromEmbeddedJson<List<string>>(SceneDataFilePath);
        ushort index = 0;
        foreach (var sceneName in sceneNames) {
            SceneIndices.Add(sceneName, index++);
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
    /// Try to get the scene index corresponding to the given scene name for encoding/decoding purposes.
    /// </summary>
    /// <param name="sceneName">The name of the scene.</param>
    /// <param name="index">The index of the scene or default if the scene name could not be found.</param>
    /// <returns>true if there is a corresponding index for the given scene name, false otherwise.</returns>
    public static bool GetSceneIndex(string sceneName, out ushort index) {
        return SceneIndices.TryGetValue(sceneName, out index);
    }

    /// <summary>
    /// Try to get the scene name corresponding to the given scene index for encoding/decoding purposes.
    /// </summary>
    /// <param name="index">The index of the scene.</param>
    /// <param name="sceneName">The name of the scene or default if the scene index could not be found.</param>
    /// <returns>true if there is a corresponding name for the given scene index, false otherwise.</returns>
    public static bool GetSceneName(ushort index, out string sceneName) {
        return SceneIndices.TryGetValue(index, out sceneName);
    }
}
