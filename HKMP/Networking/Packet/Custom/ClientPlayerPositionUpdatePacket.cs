using UnityEngine;

namespace HKMP.Networking.Packet.Custom {
    public class ClientPlayerPositionUpdatePacket : Packet, IPacket {

        public int Id { get; set; }
        public Vector3 Position { get; set; }

        public ClientPlayerPositionUpdatePacket() {
        }
        public ClientPlayerPositionUpdatePacket(Packet packet) : base(packet) {
        }
        
        public void CreatePacket() {
            Reset();
            
            Write(PacketId.ClientPlayerPositionUpdate);

            Write(Id);
            
            Write(Position);

            WriteLength();
        }

        public void ReadPacket() {
            Id = ReadInt();
            Position = ReadVector3();
        }
    }
}