using UnityEngine;

namespace HKMP.Networking.Packet.Custom {
    public class HelloServerPacket : Packet, IPacket {
        
        public string Username { get; set; }
        public string SceneName { get; set; }
        
        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; }
        
        public string AnimationClipName { get; set; }

        public HelloServerPacket() {
        }

        public HelloServerPacket(Packet packet) : base(packet) {
        }
        
        public void CreatePacket() {
            Reset();

            Write(PacketId.HelloServer);

            Write(Username);
            Write(SceneName);
            
            Write(Position);
            Write(Scale);
            
            Write(AnimationClipName);
            
            WriteLength();
        }

        public void ReadPacket() {
            Username = ReadString();
            SceneName = ReadString();

            Position = ReadVector3();
            Scale = ReadVector3();

            AnimationClipName = ReadString();
        }
    }
}