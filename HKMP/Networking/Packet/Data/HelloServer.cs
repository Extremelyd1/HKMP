using Hkmp.Math;

namespace Hkmp.Networking.Packet.Data {
    public class HelloServer : IPacketData {
        public bool IsReliable => true;

        public bool DropReliableDataIfNewerExists => true;
        
        public string Username { get; set; }
        public string SceneName { get; set; }

        public Vector2 Position { get; set; }
        public bool Scale { get; set; }

        public ushort AnimationClipId { get; set; }

        public void WriteData(IPacket packet) {
            packet.Write(Username);
            packet.Write(SceneName);

            packet.Write(Position);
            packet.Write(Scale);

            packet.Write(AnimationClipId);
        }

        public void ReadData(IPacket packet) {
            Username = packet.ReadString();
            SceneName = packet.ReadString();

            Position = packet.ReadVector2();
            Scale = packet.ReadBool();

            AnimationClipId = packet.ReadUShort();
        }
    }
}