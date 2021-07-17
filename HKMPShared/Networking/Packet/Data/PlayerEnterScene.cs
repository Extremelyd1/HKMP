using System.Collections.Generic;
using Hkmp.Game;
using Hkmp.Math;

namespace Hkmp.Networking.Packet.Data {
    public class ClientPlayerEnterScene : IPacketData {
        public ushort Id { get; set; }
        public string Username { get; set; }

        public Vector2 Position { get; set; }
        public bool Scale { get; set; }

        public Team Team { get; set; }
        public byte SkinId { get; set; }

        public ushort AnimationClipId { get; set; }

        public void WriteData(Packet packet) {
            packet.Write(Id);
            packet.Write(Username);

            packet.Write(Position);
            packet.Write(Scale);
            packet.Write((byte) Team);
            packet.Write(SkinId);

            packet.Write(AnimationClipId);
        }

        public void ReadData(Packet packet) {
            Id = packet.ReadUShort();
            Username = packet.ReadString();

            Position = packet.ReadVector2();
            Scale = packet.ReadBool();
            Team = (Team) packet.ReadByte();
            SkinId = packet.ReadByte();
            AnimationClipId = packet.ReadUShort();
        }
    }

    /**
     * Class that represents data to be sent to the client regarding players and entities already in the scene
     * on arrival
     */
    public class ClientAlreadyInScene : IPacketData {
        public List<ClientPlayerEnterScene> PlayerEnterSceneList { get; }
        public List<EntityUpdate> EntityUpdates { get; }

        public bool SceneHost { get; set; }

        public ClientAlreadyInScene() {
            PlayerEnterSceneList = new List<ClientPlayerEnterScene>();
            EntityUpdates = new List<EntityUpdate>();
        }

        public void WriteData(Packet packet) {
            var length = (byte) System.Math.Min(byte.MaxValue, PlayerEnterSceneList.Count);
            packet.Write(length);

            for (var i = 0; i < length; i++) {
                PlayerEnterSceneList[i].WriteData(packet);
            }

            length = (byte) System.Math.Min(byte.MaxValue, EntityUpdates.Count);
            packet.Write(length);

            for (var i = 0; i < length; i++) {
                EntityUpdates[i].WriteData(packet);
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

            length = packet.ReadByte();

            for (var i = 0; i < length; i++) {
                // Create new instance of entity update
                var instance = new EntityUpdate();
                
                // Read the packet data into the instance
                instance.ReadData(packet);
                
                // Add it to the list
                EntityUpdates.Add(instance);
            }

            SceneHost = packet.ReadBool();
        }
    }

    public class ServerPlayerEnterScene : IPacketData {
        public string NewSceneName { get; set; }

        public Vector2 Position { get; set; }
        public bool Scale { get; set; }

        public ushort AnimationClipId { get; set; }

        public void WriteData(Packet packet) {
            packet.Write(NewSceneName);

            packet.Write(Position);
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