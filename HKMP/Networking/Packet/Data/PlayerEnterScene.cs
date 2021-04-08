using System;
using System.Collections.Generic;
using HKMP.Game;
using UnityEngine;

namespace HKMP.Networking.Packet.Data {
    public class ClientPlayerEnterScene : IPacketData {
        public ushort Id { get; set; }
        public string Username { get; set; }
        
        public Vector3 Position { get; set; }
        public bool Scale { get; set; }
        public Team Team { get; set; }
        
        public ushort Skin { get; set; }
        public ushort AnimationClipId { get; set; }
        
        public void WriteData(Packet packet) {
            packet.Write(Id);
            packet.Write(Username);
            
            packet.Write((Vector2) Position);
            packet.Write(Scale);
            packet.Write((byte) Team);
            packet.Write(Skin);

            packet.Write(AnimationClipId);
        }

        public void ReadData(Packet packet) {
            Id = packet.ReadUShort();
            Username = packet.ReadString();

            Position = packet.ReadVector2();
            Scale = packet.ReadBool();
            Team = (Team) packet.ReadByte();
            Skin = packet.ReadUShort();
            Logger.Info(this,$"reads Skin {Skin}");
            AnimationClipId = packet.ReadUShort();
        }
    }

    public class ClientPlayerAlreadyInScene : IPacketData {
        
        public List<ClientPlayerEnterScene> PlayerEnterSceneList { get; }
        
        public bool SceneHost { get; set; }

        public ClientPlayerAlreadyInScene() {
            PlayerEnterSceneList = new List<ClientPlayerEnterScene>();
        }
        
        public void WriteData(Packet packet) {
            var length = (byte) Math.Min(byte.MaxValue, PlayerEnterSceneList.Count);
            
            packet.Write(length);

            for (var i = 0; i < length; i++) {
                PlayerEnterSceneList[i].WriteData(packet);
            }
            
            packet.Write(SceneHost);
        }

        public void ReadData(Packet packet) {
            var length = packet.ReadByte();

            for (var i = 0; i < length; i++) {
                // Create new instance of generic type
                var instance = new ClientPlayerEnterScene();

                // Read the packet data into the instance
                instance.ReadData(packet);

                // And add it to our already initialized list
                PlayerEnterSceneList.Add(instance);
            }

            SceneHost = packet.ReadBool();
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