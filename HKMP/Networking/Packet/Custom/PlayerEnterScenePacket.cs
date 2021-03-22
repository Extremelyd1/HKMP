using HKMP.Game;
using UnityEngine;

namespace HKMP.Networking.Packet.Custom {
    public class ClientPlayerEnterScenePacket : Packet, IPacket {

        public ushort Id { get; set; }
        public string Username { get; set; }
        
        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; }
        public Team Team { get; set; }
        
        public ushort AnimationClipId { get; set; }

        public ClientPlayerEnterScenePacket() {
        }
        
        public ClientPlayerEnterScenePacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();

            Write(PacketId.PlayerEnterScene);
            
            Write(Id);
            Write(Username);
            
            Write(Position);
            Write(Scale);
            Write((byte) Team);
            
            Write(AnimationClipId);
            
            WriteLength();

            return this;
        }

        public void ReadPacket() {
            Id = ReadUShort();
            Username = ReadString();

            Position = ReadVector3();
            Scale = ReadVector3();
            Team = (Team) ReadByte();

            AnimationClipId = ReadUShort();
        }
    }

    public class ServerPlayerEnterScenePacket : Packet, IPacket {
        
        public string NewSceneName { get; set; }
        
        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; }
        
        public ushort AnimationClipId { get; set; }

        public ServerPlayerEnterScenePacket() {
        }

        public ServerPlayerEnterScenePacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();

            Write(PacketId.PlayerEnterScene);

            Write(NewSceneName);

            Write(Position);
            Write(Scale);

            Write(AnimationClipId);

            WriteLength();

            return this;
        }

        public void ReadPacket() {
            NewSceneName = ReadString();

            Position = ReadVector3();
            Scale = ReadVector3();

            AnimationClipId = ReadUShort();
        }
    }
}