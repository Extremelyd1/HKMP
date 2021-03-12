using UnityEngine;

namespace HKMP.Networking.Packet.Custom {
    public class ServerPlayerPositionUpdatePacket : Packet, IPacket {
        public Vector3 Position { get; set; }

        public ServerPlayerPositionUpdatePacket() {
        }
        
        public ServerPlayerPositionUpdatePacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();
            
            Write(PacketId.PlayerPositionUpdate);

            Write(Position);

            WriteLength();

            return this;
        }

        public void ReadPacket() {
            Position = ReadVector3();
        }
    }
}