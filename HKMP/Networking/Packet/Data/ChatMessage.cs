using System.Collections.Generic;

namespace Hkmp.Networking.Packet.Data {
    public class ChatMessage : IPacketData {
        public const ushort MaxMessageLength = ushort.MaxValue;
        public const string AllowedCharacterString =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()-=_+[]{}<>\\|;:'\"/?,.~` ";

        private static readonly Dictionary<char, byte> CharToByteDict;
        private static readonly Dictionary<byte, char> ByteToCharDict;
        
        static ChatMessage() {
            CharToByteDict = new Dictionary<char, byte>();
            ByteToCharDict = new Dictionary<byte, char>();

            for (byte i = 0; i < AllowedCharacterString.Length; i++) {
                CharToByteDict[AllowedCharacterString[i]] = i;
                ByteToCharDict[i] = AllowedCharacterString[i];
            }
        }

        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => false;
        
        public string Message { get; set; }

        public void WriteData(IPacket packet) {
            var length = (ushort) System.Math.Min(Message.Length, MaxMessageLength);

            packet.Write(length);

            for (var i = 0; i < length; i++) {
                packet.Write(CharToByteDict[Message[i]]);
            }
        }

        public void ReadData(IPacket packet) {
            var length = packet.ReadUShort();

            Message = "";

            for (var i = 0; i < length; i++) {
                Message += ByteToCharDict[packet.ReadByte()];
            }
        }
    }
}