using UnityEngine;

namespace HKMP.Networking.Packet.Custom {
    public class ClientPlayerEnterScenePacket : Packet, IPacket {

        public int Id { get; set; }
        public string Username { get; set; }
        
        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; }
        
        public string AnimationClipName { get; set; }

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
            
            Write(AnimationClipName);
            
            WriteLength();

            return this;
        }

        public void ReadPacket() {
            Id = ReadInt();
            Username = ReadString();

            Position = ReadVector3();
            Scale = ReadVector3();

            AnimationClipName = ReadString();
        }
    }

    public class ServerPlayerEnterScenePacket : Packet, IPacket {
        
        public string NewSceneName { get; set; }
        
        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; }
        
        public string AnimationClipName { get; set; }

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

            Write(AnimationClipName);

            WriteLength();

            return this;
        }

        public void ReadPacket() {
            NewSceneName = ReadString();

            Position = ReadVector3();
            Scale = ReadVector3();

            AnimationClipName = ReadString();
        }
    }
}