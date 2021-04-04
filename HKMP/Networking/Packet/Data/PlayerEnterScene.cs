using HKMP.Game;
using UnityEngine;

namespace HKMP.Networking.Packet.Data {
    public class ClientPlayerEnterScene : IPacketData {
        public ushort Id { get; set; }
        public string Username { get; set; }
        
        public Vector3 Position { get; set; }
        public bool Scale { get; set; }
        public Team Team { get; set; }
        public ushort AnimationClipId { get; set; }
        
        public void WriteData(Packet packet) {
            packet.Write(Id);
            packet.Write(Username);
            
            packet.Write((Vector2) Position);
            packet.Write(Scale);
            packet.Write((byte) Team);

            packet.Write(AnimationClipId);
        }

        public void ReadData(Packet packet) {
            Id = packet.ReadUShort();
            Username = packet.ReadString();

            Position = packet.ReadVector2();
            Scale = packet.ReadBool();
            Team = (Team) packet.ReadByte();

            AnimationClipId = packet.ReadUShort();
        }
    }

    public class ServerPlayerEnterScene : IPacketData {
        
        public string NewSceneName { get; set; }
        
        public Vector3 Position { get; set; }
        public bool Scale { get; set; }
        
        public ushort AnimationClipId { get; set; }

        public void WriteData(Packet packet) {
            packet.Write(NewSceneName);

            packet.Write((Vector2) Position);
            packet.Write(Scale);

            packet.Write(AnimationClipId);
        }

        public void ReadData(Packet packet) {
            NewSceneName = packet.ReadString();

            Position = packet.ReadVector2();
            Scale = packet.ReadBool();

            AnimationClipId = packet.ReadUShort();
        }
    }
}