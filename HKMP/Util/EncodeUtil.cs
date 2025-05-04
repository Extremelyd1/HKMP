using System;
using System.Collections.Generic;
using System.Linq;
using Hkmp.Collection;
using Hkmp.Game.Client.Save;
using Hkmp.Math;
using Hkmp.Serialization;
using Logger = Hkmp.Logging.Logger;

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
    public static bool TryGetStringIndex(string sceneName, out ushort index) {
        return StringIndices.TryGetValue(sceneName, out index);
    }

    /// <summary>
    /// Try to get the string corresponding to the given string index for encoding/decoding purposes.
    /// </summary>
    /// <param name="index">The string.</param>
    /// <param name="sceneName">The string or default if the string index could not be found.</param>
    /// <returns>true if there is a corresponding string for the given index, false otherwise.</returns>
    public static bool TryGetStringName(ushort index, out string sceneName) {
        return StringIndices.TryGetValue(index, out sceneName);
    }
    
    /// <summary>
    /// Encode a given value into a byte array in the context of save data.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>A byte array containing the encoded value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the given value is out of range to be encoded.
    /// </exception>
    /// <exception cref="NotImplementedException">Thrown when the given value has a type that cannot be encoded due to
    /// missing implementation.</exception>
    public static byte[] EncodeSaveDataValue(object value) {
        if (value is bool bValue) {
            return [(byte) (bValue ? 1 : 0)];
        }

        if (value is float fValue) {
            return BitConverter.GetBytes(fValue);
        }

        if (value is int iValue) {
            return BitConverter.GetBytes(iValue);
        }

        if (value is string sValue) {
            return EncodeString(sValue);
        }

        if (value is Vector3 vecValue) {
            return EncodeVector3(vecValue);
        }

        if (value is List<string> listValue) {
            if (listValue.Count > ushort.MaxValue) {
                throw new ArgumentOutOfRangeException($"Could not encode string list length: {listValue.Count}");
            }

            var length = (ushort) listValue.Count;

            IEnumerable<byte> byteArray = BitConverter.GetBytes(length);

            for (var i = 0; i < length; i++) {
                var encoded = EncodeString(listValue[i]);

                byteArray = byteArray.Concat(encoded);
            }

            return byteArray.ToArray();
        }

        if (value is BossSequenceDoorCompletion bsdCompValue) {
            // For now we only encode the bools of completion struct
            var firstBools = new[] {
                bsdCompValue.CanUnlock, bsdCompValue.Unlocked, bsdCompValue.Completed, bsdCompValue.AllBindings,
                bsdCompValue.NoHits, bsdCompValue.BoundNail, bsdCompValue.BoundShell, bsdCompValue.BoundCharms
            };

            var byte1 = GetByte(firstBools);

            var byte2 = (byte) (bsdCompValue.BoundSoul ? 1 : 0);

            return [byte1, byte2];
        }

        if (value is BossStatueCompletion bsCompValue) {
            var bools = new[] {
                bsCompValue.HasBeenSeen, bsCompValue.IsUnlocked, bsCompValue.CompletedTier1, bsCompValue.CompletedTier2,
                bsCompValue.CompletedTier3, bsCompValue.SeenTier3Unlock, bsCompValue.UsingAltVersion
            };

            return [GetByte(bools)];
        }

        if (value is List<Vector3> vecListValue) {
            if (vecListValue.Count > ushort.MaxValue) {
                throw new ArgumentOutOfRangeException($"Could not encode vector list length: {vecListValue.Count}");
            }

            var length = (ushort) vecListValue.Count;

            IEnumerable<byte> byteArray = BitConverter.GetBytes(length);

            for (var i = 0; i < length; i++) {
                var encoded = EncodeVector3(vecListValue[i]);

                byteArray = byteArray.Concat(encoded);
            }

            return byteArray.ToArray();
        }

        if (value is MapZone mapZone) {
            return [(byte) mapZone];
        }

        if (value is List<int> intListValue) {
            if (intListValue.Count > byte.MaxValue) {
                throw new ArgumentOutOfRangeException($"Could not encode int list length: {intListValue.Count}");
            }

            var length = (byte) intListValue.Count;

            // Create a byte array for the encoded result that has the size of the length of the int list plus one
            // for the length itself
            var byteArray = new byte[length + 1];
            byteArray[0] = length;
            
            for (var i = 0; i < length; i++) {
                byteArray[i + 1] = (byte) intListValue[i];
            }

            return byteArray;
        }

        throw new ArgumentException($"No encoding implementation for type: {value.GetType()}");

        // To preserve network bandwidth, we encode known strings into indices, since there is a limited number of
        // strings in the save data
        byte[] EncodeString(string stringValue) {
            if (!TryGetStringIndex(stringValue, out var index)) {
                Logger.Info($"Could not encode string value: {stringValue}");
                throw new Exception($"Could not encode string value: {stringValue}");
            }

            return BitConverter.GetBytes(index);
        }

        byte[] EncodeVector3(Vector3 vec3Value) {
            return BitConverter.GetBytes(vec3Value.X)
                .Concat(BitConverter.GetBytes(vec3Value.Y))
                .Concat(BitConverter.GetBytes(vec3Value.Z))
                .ToArray();
        }
    }

    /// <summary>
    /// Decode a given save data value from its name and encoded byte array. This only supports values from PlayerData.
    /// </summary>
    /// <param name="name">The variable name from PlayerData.</param>
    /// <param name="encodedValue">The encoded value as a byte array.</param>
    /// <returns>The decoded object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the given name does not correspond with a PlayerData
    /// value that can be decoded, because its variable properties do not exist.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the length of the given byte array does not match
    /// the value that should be decoded from it.</exception>
    /// <exception cref="ArgumentException">Thrown when the value can not be decoded for another reason.</exception>
    public static object DecodeSaveDataValue(string name, byte[] encodedValue) {
        if (!SaveDataMapping.Instance.PlayerDataVarProperties.TryGetValue(name, out var varProps)) {
            throw new InvalidOperationException($"Could not decode save data value with name: \"{name}\", missing variable properties");
        }

        var type = varProps.VarType;
        if (type == "System.Boolean") {
            if (encodedValue.Length != 1) {
                throw new ArgumentOutOfRangeException($"Encoded value has incorrect value length for bool: {encodedValue.Length}");
            }

            return encodedValue[0] == 1;
        }
        
        if (type == "System.Single") {
            if (encodedValue.Length != 4) {
                throw new ArgumentOutOfRangeException($"Encoded value has incorrect value length for float: {encodedValue.Length}");
            }

            return BitConverter.ToSingle(encodedValue, 0);
        }

        if (type == "System.Int32") {
            if (encodedValue.Length != 4) {
                throw new ArgumentOutOfRangeException($"Encoded value has incorrect value length for int: {encodedValue.Length}");
            }

            return BitConverter.ToInt32(encodedValue, 0);
        }

        if (type == "System.String") {
            return DecodeString(encodedValue, 0);
        }

        if (type == "Hkmp.Math.Vector3") {
            if (encodedValue.Length != 12) {
                throw new ArgumentOutOfRangeException($"Encoded value has incorrect value length for Vector3: {encodedValue.Length}");
            }
            
            return new Vector3(
                BitConverter.ToSingle(encodedValue, 0),
                BitConverter.ToSingle(encodedValue, 4),
                BitConverter.ToSingle(encodedValue, 8)
            );
        }

        if (type == "System.Collections.Generic.List`1[System.String]") {
            var length = BitConverter.ToUInt16(encodedValue, 0);

            var list = new List<string>();
            for (var i = 0; i < length; i++) {
                var sceneIndex = BitConverter.ToUInt16(encodedValue, 2 + i * 2);

                if (!TryGetStringName(sceneIndex, out var sceneName)) {
                    throw new ArgumentException($"Could not decode string in list from save update: {sceneIndex}");
                }

                list.Add(sceneName);
            }

            return list;
        }

        if (type == "Hkmp.Serialization.BossSequenceDoorCompletion") {
            var byte1 = encodedValue[0];
            var byte2 = encodedValue[1];

            var bools = GetBoolsFromByte(byte1);

            return new BossSequenceDoorCompletion {
                CanUnlock = bools[0],
                Unlocked = bools[1],
                Completed = bools[2],
                AllBindings = bools[3],
                NoHits = bools[4],
                BoundNail = bools[5],
                BoundShell = bools[6],
                BoundCharms = bools[7],
                BoundSoul = byte2 == 1
            };
        }

        if (type == "Hkmp.Serialization.BossStatueCompletion") {
            var bools = GetBoolsFromByte(encodedValue[0]);

            return new BossStatueCompletion {
                HasBeenSeen = bools[0],
                IsUnlocked = bools[1],
                CompletedTier1 = bools[2],
                CompletedTier2 = bools[3],
                CompletedTier3 = bools[4],
                SeenTier3Unlock = bools[5],
                UsingAltVersion = bools[6]
            };
        }

        if (type == "System.Collections.Generic.List`1[Hkmp.Math.Vector3]") {
            var length = BitConverter.ToUInt16(encodedValue, 0);

            var list = new List<Vector3>();
            for (var i = 0; i < length; i++) {
                // Decode the floats of the vector with offset indices 2, 6, and 10 because we already read 2
                // bytes as the length. The index is multiplied by 12 as this is the length of a single float
                var value = new Vector3(
                    BitConverter.ToSingle(encodedValue, 2 + i * 12),
                    BitConverter.ToSingle(encodedValue, 6 + i * 12),
                    BitConverter.ToSingle(encodedValue, 10 + i * 12)
                );

                list.Add(value);
            }

            return list;
        }

        if (type == "Hkmp.Serialization.MapZone") {
            if (encodedValue.Length != 1) {
                throw new ArgumentOutOfRangeException($"Encoded value has incorrect value length for MapZone: {encodedValue.Length}");
            }
            
            return (MapZone) encodedValue[0];
        }

        if (type == "System.Collections.Generic.List`1[System.Int32]") {
            var length = encodedValue[0];

            var list = new List<int>();
            for (var i = 0; i < length; i++) {
                list.Add(encodedValue[i + 1]);
            }

            return list;
        }

        throw new ArgumentException($"Could not decode type: {type}");
        
        // Decode a string from the given byte array and start index in that array
        string DecodeString(byte[] encoded, int startIndex) {
            var sceneIndex = BitConverter.ToUInt16(encoded, startIndex);

            if (!TryGetStringName(sceneIndex, out var value)) {
                throw new ArgumentException($"Could not decode string from save update: {encodedValue}");
            }

            return value;
        }
    }
}
