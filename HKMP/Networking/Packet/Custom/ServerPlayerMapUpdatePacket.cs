using UnityEngine;

namespace HKMP.Networking.Packet.Custom {
    public class ServerPlayerMapUpdatePacket : Packet, IPacket {
        
        public Vector3 Position { get; set; }

        public ServerPlayerMapUpdatePacket() {
        }

        public ServerPlayerMapUpdatePacket(Packet packet) : base(packet) {
        }

        public Packet CreatePacket() {
            Reset();

            Write(PacketId.PlayerMapUpdate);

            Write(Position);
            
            WriteLength();

            return this;
        }

        public void ReadPacket() {
            Position = ReadVector3();
        }
    }
}