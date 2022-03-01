using Hkmp.Util;

namespace Hkmp.Networking.Packet.Data {
    public class ChatMessage : IPacketData {
        public const byte MaxMessageLength = byte.MaxValue;

        public bool IsReliable => true;
        public bool DropReliableDataIfNewerExists => false;
        
        public string Message { get; set; }

        public void WriteData(IPacket packet) {
            var length = (byte) System.Math.Min(Message.Length, MaxMessageLength);

            packet.Write(length);

            for (var i = 0; i < length; i++) {
                packet.Write(StringUtil.CharByteDict[Message[i]]);
            }
        }

        public void ReadData(IPacket packet) {
            var length = packet.ReadByte();

            Message = "";
            for (var i = 0; i < length; i++) {
                Message += StringUtil.CharByteDict[packet.ReadByte()];
            }
        }
    }
}