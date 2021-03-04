using UnityEngine;

namespace HKMP.Networking.Packet.Custom {
    public class PlayerEnterScenePacket : Packet, IPacket {

        public int Id { get; set; }
        public string Username { get; set; }
        
        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; }
        
        public string AnimationClipName { get; set; }

        public PlayerEnterScenePacket() {
        }
        
        public PlayerEnterScenePacket(Packet packet) : base(packet) {
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
}