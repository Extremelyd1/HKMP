using UnityEngine;

namespace HKMP.Networking.Packet.Custom {
    public class ServerPlayerPositionUpdatePacket : Packet, IPacket {
        public Vector3 Position { get; set; }

        public ServerPlayerPositionUpdatePacket() {
        }
        
        public ServerPlayerPositionUpdatePacket(Packet packet) : base(packet) {
        }
        
        public void CreatePacket() {
            Reset();
            
            Write(PacketId.ServerPlayerPositionUpdate);

            Write(Position);

            WriteLength();
        }

        public void ReadPacket() {
            Position = ReadVector3();
        }
    }
}