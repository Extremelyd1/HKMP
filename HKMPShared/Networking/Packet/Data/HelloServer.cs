using Hkmp.Math;

namespace Hkmp.Networking.Packet.Data {
    public class HelloServer : IPacketData {
        public string Username { get; set; }
        public string SceneName { get; set; }

        public Vector2 Position { get; set; }
        public bool Scale { get; set; }

        public ushort AnimationClipId { get; set; }

        public void WriteData(Packet packet) {
            packet.Write(Username);
            packet.Write(SceneName);

            packet.Write(Position);
            packet.Write(Scale);

            packet.Write(AnimationClipId);
        }

        public void ReadData(Packet packet) {
            Username = packet.ReadString();
            SceneName = packet.ReadString();

            Position = packet.ReadVector2();
            Scale = packet.ReadBool();

            AnimationClipId = packet.ReadUShort();
        }
    }
}