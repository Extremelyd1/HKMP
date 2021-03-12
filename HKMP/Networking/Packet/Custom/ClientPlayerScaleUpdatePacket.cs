using UnityEngine;

namespace HKMP.Networking.Packet.Custom {
    public class ClientPlayerScaleUpdatePacket : Packet, IPacket {
        
        public int Id { get; set; }
        public Vector3 Scale { get; set; }

        public ClientPlayerScaleUpdatePacket() {
        }
        
        public ClientPlayerScaleUpdatePacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();
            
            Write(PacketId.PlayerScaleUpdate);

            Write(Id);
            
            Write(Scale);

            WriteLength();

            return this;
        }

        public void ReadPacket() {
            Id = ReadInt();
            Scale = ReadVector3();
        }
    }
}