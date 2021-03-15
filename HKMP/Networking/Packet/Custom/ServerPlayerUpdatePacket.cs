using UnityEngine;

namespace HKMP.Networking.Packet.Custom {
    public class ServerPlayerUpdatePacket : Packet, IPacket {
        // Total packet: 38(+animation name) bytes

        public ushort SequenceNumber { get; set; }

        // Position: 3x float - 3x4 = 12 bytes
        public Vector3 Position { get; set; } = Vector3.zero;
        
        // Scale: 3x float - 3x4 = 12 bytes
        public Vector3 Scale { get; set; } = Vector3.zero;
        
        // Map position: 3x float - 3x4 = 12 bytes
        public Vector3 MapPosition { get; set; } = Vector3.zero;

        public ServerPlayerUpdatePacket() {
        }
        
        public ServerPlayerUpdatePacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();

            Write(PacketId.PlayerUpdate);
            
            Write(SequenceNumber);

            Write(Position);
            Write(Scale);
            Write(MapPosition);

            WriteLength();

            return this;
        }

        public void ReadPacket() {
            SequenceNumber = ReadUShort();
            Position = ReadVector3();
            Scale = ReadVector3();
            MapPosition = ReadVector3();
        }
    }
}