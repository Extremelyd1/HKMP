using UnityEngine;

namespace HKMP.Networking.Packet.Custom {
    public class ClientPlayerUpdatePacket : Packet, IPacket {
        // Total packet: 40(+animation name) bytes

        // ID: ushort - 2 bytes
        public ushort Id { get; set; }
        
        // Position: 3x float - 3x4 = 12 bytes
        public Vector3 Position { get; set; }
        
        // Scale: 3x float - 3x4 = 12 bytes
        public Vector3 Scale { get; set; }
        
        // Map position: 3x float - 3x4 = 12 bytes
        public Vector3 MapPosition { get; set; }

        public ClientPlayerUpdatePacket() {
        }
        public ClientPlayerUpdatePacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();
            
            Write(PacketId.PlayerUpdate);

            Write(Id);
            
            Write(Position);
            Write(Scale);
            Write(MapPosition);

            WriteLength();

            return this;
        }

        public void ReadPacket() {
            Id = ReadUShort();
            Position = ReadVector3();
            Scale = ReadVector3();
            MapPosition = ReadVector3();
        }
    }
}