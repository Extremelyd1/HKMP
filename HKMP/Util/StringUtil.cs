using Hkmp.Collection;

namespace Hkmp.Util {
    public static class StringUtil {
        public const string AllowedChatCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()-=_+[]{}<>\\|;:'\"/?,.~` ";
        
        public static readonly BiLookup<char, byte> CharByteDict;
        
        static StringUtil() {
            CharByteDict = new BiLookup<char, byte>();

            for (byte i = 0; i < AllowedChatCharacters.Length; i++) {
                CharByteDict.Add(AllowedChatCharacters[i], i);
            }
        }
    }
}