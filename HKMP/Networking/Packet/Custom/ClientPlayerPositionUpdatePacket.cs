using UnityEngine;

namespace HKMP.Networking.Packet.Custom {
    public class ClientPlayerPositionUpdatePacket : Packet, IPacket {

        public int Id { get; set; }
        public Vector3 Position { get; set; }

        public ClientPlayerPositionUpdatePacket() {
        }
        public ClientPlayerPositionUpdatePacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();
            
            Write(PacketId.ClientPlayerPositionUpdate);

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