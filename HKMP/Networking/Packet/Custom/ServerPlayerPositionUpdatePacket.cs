using UnityEngine;

namespace HKMP.Networking.Packet.Custom {
    public class ServerPlayerPositionUpdatePacket : Packet, IPacket {
        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; }

        public ServerPlayerPositionUpdatePacket() {
        }
        
        public ServerPlayerPositionUpdatePacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();
            
            Write(PacketId.PlayerPositionUpdate);

            Write(Position);
            Write(Scale);

            WriteLength();

            return this;
        }

        public void ReadPacket() {
            Position = ReadVector3();
            Scale = ReadVector3();
        }
    }
}