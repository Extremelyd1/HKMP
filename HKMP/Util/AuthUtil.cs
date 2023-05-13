using System;
using System.Security.Cryptography;
using Hkmp.Collection;

namespace Hkmp.Util;

/// <summary>
/// Utility class for authentication related methods. 
/// </summary>
internal static class AuthUtil {
    /// <summary>
    /// The length of the authentication key.
    /// </summary>
    public const int AuthKeyLength = 56;

    /// <summary>
    /// Cryptographically secure random number generator for generating authentication keys.
    /// </summary>
    private static readonly RandomNumberGenerator RandomNumberGenerator = RandomNumberGenerator.Create();

    /// <summary>
    /// Lookup for authentication key characters to their byte value.
    /// </summary>
    private static readonly BiLookup<char, byte> AuthKeyLookup;

    /// <summary>
    /// Static constructor that initializes the bi-directional lookup.
    /// </summary>
    static AuthUtil() {
        // A string containing all possible characters for an authentication key
        const string authKeyCharacter =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";

        AuthKeyLookup = new BiLookup<char, byte>();

        for (byte i = 0; i < authKeyCharacter.Length; i++) {
            AuthKeyLookup.Add(authKeyCharacter[i], i);
        }
    }

    /// <summary>
    /// Checks whether a given authentication key is valid or not.
    /// </summary>
    /// <param name="authKey">The authentication key in string form to check.</param>
    /// <returns>True if the given authentication key is valid, false otherwise.</returns>
    public static bool IsValidAuthKey(string authKey) {
        if (authKey == null) {
            return false;
        }

        if (authKey.Length != AuthKeyLength) {
            return false;
        }

        foreach (var authKeyChar in authKey.ToCharArray()) {
            if (!AuthKeyLookup.ContainsFirst(authKeyChar)) {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Generates a new authentication key.
    /// </summary>
    /// <returns>The authentication key as string.</returns>
    public static string GenerateAuthKey() {
        var authKey = "";

        for (var i = 0; i < AuthKeyLength; i++) {
            var randomIndex = (byte) GetRandomInt(0, AuthKeyLookup.Count);

            authKey += AuthKeyLookup[randomIndex];
        }

        return authKey;
    }

    /// <summary>
    /// Get a random integer between <paramref name="minValue"/> (inclusive) and <paramref name="maxValue"/>
    /// (exclusive).
    /// </summary>
    /// <param name="minValue">The minimum value of the integer (inclusive).</param>
    /// <param name="maxValue">The maximum value of the integer (exclusive).</param>
    /// <returns>A random signed integer value.</returns>
    private static int GetRandomInt(int minValue, int maxValue) {
        var diff = (long) maxValue - minValue;
        var upperBound = uint.MaxValue / diff * diff;

        uint ui;
        do {
            ui = GetRandomUInt();
        } while (ui >= upperBound);

        return (int) (minValue + ui % diff);
    }

    /// <summary>
    /// Get a random unsigned integer.
    /// </summary>
    /// <returns>A random unsigned integer.</returns>
    private static uint GetRandomUInt() {
        var randomBytes = GenerateRandomBytes(sizeof(uint));

        return BitConverter.ToUInt32(randomBytes, 0);
    }

    /// <summary>
    /// Generate an array of random bytes with the given length.
    /// </summary>
    /// <param name="numBytes">The number of bytes to generate.</param>
    /// <returns>A byte array of length <paramref name="numBytes"/></returns>
    private static byte[] GenerateRandomBytes(int numBytes) {
        var buffer = new byte[numBytes];

        RandomNumberGenerator.GetBytes(buffer);

        return buffer;
    }
}
