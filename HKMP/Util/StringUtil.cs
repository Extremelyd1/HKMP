using System.Collections.Generic;
using System.Text;
using Hkmp.Collection;
using UnityEngine.UI;

namespace Hkmp.Util;

/// <summary>
/// Class for utilities regarding strings.
/// </summary>
internal static class StringUtil {
    /// <summary>
    /// The path to the resource of the JSON file that contains valid characters.
    /// </summary>
    private const string CharacterResourcePath = "Hkmp.Ui.Resources.chars.json";

    /// <summary>
    /// Hashset containing the allowed characters.
    /// </summary>
    private static readonly HashSet<char> AllowedCharacters;

    /// <summary>
    /// A string containing all allowed characters for pre-caching.
    /// </summary>
    public static readonly string AllowedCharactersString;

    /// <summary>
    /// A bi-directional lookup of the allowed characters to their byte indices.
    /// </summary>
    public static readonly BiLookup<char, ushort> CharByteDict;

    /// <summary>
    /// OnValidateInput delegate to validate characters that are allowed.
    /// </summary>
    public static InputField.OnValidateInput ValidateAllowedCharacters => (_, _, addedChar) =>
        AllowedCharacters.Contains(addedChar) ? addedChar : '\0';

    /// <summary>
    /// Static constructor that initializes and fills the hashset and lookup table.
    /// </summary>
    static StringUtil() {
        // Initialize the hashset and lookup table
        AllowedCharacters = new HashSet<char>();
        CharByteDict = new BiLookup<char, ushort>();

        var stringBuilder = new StringBuilder();

        // Load the JSON containing valid characters from the embedded resource
        var characters = FileUtil.LoadObjectFromResourcePath<Dictionary<int, char>>(CharacterResourcePath);

        // Loop over all the characters and add them to the hashset, lookup table and string builder
        ushort index = 0;
        foreach (var character in characters.Values) {
            AllowedCharacters.Add(character);

            CharByteDict.Add(character, index++);

            stringBuilder.Append(character);
        }

        AllowedCharactersString = stringBuilder.ToString();
    }
}
