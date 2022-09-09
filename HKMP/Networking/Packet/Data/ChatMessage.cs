using Hkmp.Util;

namespace Hkmp.Networking.Packet.Data {
    /// <summary>
    /// Packet data for a chat message.
    /// </summary>
    internal class ChatMessage : IPacketData {
        /// <summary>
        /// The maximum length of a chat message.
        /// </summary>
        public const byte MaxMessageLength = byte.MaxValue;

        /// <inheritdoc />
        public bool IsReliable => true;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => false;

        /// <summary>
        /// The message string.
        /// </summary>
        public string Message { get; set; }

        /// <inheritdoc />
        public void WriteData(IPacket packet) {
            var length = (byte)System.Math.Min(Message.Length, MaxMessageLength);

            packet.Write(length);

            for (var i = 0; i < length; i++) {
                packet.Write(StringUtil.CharByteDict[Message[i]]);
            }
        }

        /// <inheritdoc />
        public void ReadData(IPacket packet) {
            var length = packet.ReadByte();

            Message = "";
            for (var i = 0; i < length; i++) {
                Message += StringUtil.CharByteDict[packet.ReadByte()];
            }
        }
    }
}
