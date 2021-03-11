using UnityEngine;

namespace HKMP.Networking.Packet.Custom {
    public class ClientPlayerMapUpdatePacket : Packet, IPacket {
        
        public int Id { get; set; }
        public Vector3 Position { get; set; }

        public ClientPlayerMapUpdatePacket() {
        }

        public ClientPlayerMapUpdatePacket(Packet packet) : base(packet) {
        }

        public Packet CreatePacket() {
            Reset();

            Write(PacketId.PlayerMapUpdate);

            Write(Id);

            Write(Position);
            
            WriteLength();

            return this;
        }

        public void ReadPacket() {
            Id = ReadInt();
            Position = ReadVector3();
        }
    }
}