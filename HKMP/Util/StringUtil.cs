using Hkmp.Collection;

namespace Hkmp.Util {
    /// <summary>
    /// Class for utilities regarding strings.
    /// </summary>
    internal static class StringUtil {
        /// <summary>
        /// A string containing the allowed characters to be typed in the chat.
        /// </summary>
        public const string AllowedChatCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()-=_+[]{}<>\\|;:'\"/?,.~` ";

        /// <summary>
        /// A bi-directional lookup of the allowed characters to their byte indices.
        /// </summary>
        public static readonly BiLookup<char, byte> CharByteDict;

        /// <summary>
        /// Static constructor that initializes the lookup table.
        /// </summary>
        static StringUtil() {
            CharByteDict = new BiLookup<char, byte>();

            for (byte i = 0; i < AllowedChatCharacters.Length; i++) {
                CharByteDict.Add(AllowedChatCharacters[i], i);
            }
        }
    }
}