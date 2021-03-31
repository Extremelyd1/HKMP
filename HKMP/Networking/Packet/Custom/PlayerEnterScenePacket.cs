using System.Collections.Generic;
using HKMP.Game;
using UnityEngine;

namespace HKMP.Networking.Packet.Custom {
    public class ClientPlayerEnterScenePacket : Packet, IPacket {

        public ScenePlayerData ScenePlayerData { get; set; }

        public ClientPlayerEnterScenePacket() {
            ScenePlayerData = new ScenePlayerData();
        }
        
        public ClientPlayerEnterScenePacket(Packet packet) : base(packet) {
            ScenePlayerData = new ScenePlayerData();
        }
        
        public Packet CreatePacket() {
            Reset();

            Write(PacketId.PlayerEnterScene);
            
            Write(ScenePlayerData.Id);
            Write(ScenePlayerData.Username);
            
            Write(ScenePlayerData.Position);
            Write(ScenePlayerData.Scale);
            Write((byte) ScenePlayerData.Team);
            Write(ScenePlayerData.Skin);

            Write(ScenePlayerData.AnimationClipId);
            
            WriteLength();

            return this;
        }

        public void ReadPacket() {
            ScenePlayerData.Id = ReadUShort();
            ScenePlayerData.Username = ReadString();

            ScenePlayerData.Position = ReadVector3();
            ScenePlayerData.Scale = ReadVector3();
            ScenePlayerData.Team = (Team) ReadByte();
            ScenePlayerData.Skin = ReadInt();

            ScenePlayerData.AnimationClipId = ReadUShort();
        }
    }

    public class ClientAlreadyInScenePacket : Packet, IPacket {
        
        public List<ScenePlayerData> ScenePlayerData { get; }

        public ClientAlreadyInScenePacket() {
            ScenePlayerData = new List<ScenePlayerData>();
        }

        public ClientAlreadyInScenePacket(Packet packet) : base(packet) {
            ScenePlayerData = new List<ScenePlayerData>();
        }

        public Packet CreatePacket() {
            Reset();
            
            Write(PacketId.AlreadyInScene);
            
            // We first write the length of the list and then the list itself
            Write((ushort) ScenePlayerData.Count);

            foreach (var playerData in ScenePlayerData) {
                Write(playerData.Id);
                Write(playerData.Username);

                Write(playerData.Position);
                Write(playerData.Scale);
                Write((byte) playerData.Team);
                Write(playerData.Skin);

                Write(playerData.AnimationClipId);
            }

            WriteLength();
            
            return this;
        }

        public void ReadPacket() {
            // First read the length of the list and then read each individual player data instance
            var numPlayerData = ReadUShort();

            for (var i = 0; i < numPlayerData; i++) {
                // Create a new instance and read all the values from the packet
                var playerData = new ScenePlayerData {
                    Id = ReadUShort(),
                    Username = ReadString(),

                    Position = ReadVector3(),
                    Scale = ReadVector3(),
                    Team = (Team) ReadByte(),
                    Skin = ReadInt(),
                    AnimationClipId = ReadUShort(),
                };

                // Add it to the list
                ScenePlayerData.Add(playerData);
            }
        }
    }

    public class ScenePlayerData {
        public ushort Id { get; set; }
        public string Username { get; set; }
        
        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; }
        public Team Team { get; set; }
        public int Skin { get; set; }
        public ushort AnimationClipId { get; set; }
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