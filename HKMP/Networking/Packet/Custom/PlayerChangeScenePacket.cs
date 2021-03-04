using UnityEngine;

namespace HKMP.Networking.Packet.Custom {
    public class PlayerChangeScenePacket : Packet, IPacket {
        
        public string NewSceneName { get; set; }
        
        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; }
        
        public string AnimationClipName { get; set; }

        public PlayerChangeScenePacket() {
        }
        
        public PlayerChangeScenePacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();

            Write(PacketId.PlayerChangeScene);

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