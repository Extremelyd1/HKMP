using UnityEngine;

namespace HKMP.Networking.Packet.Custom {
    public class ServerPlayerScaleUpdatePacket : Packet, IPacket {
        
        public Vector3 Scale { get; set; }

        public ServerPlayerScaleUpdatePacket() {
        }
        
        public ServerPlayerScaleUpdatePacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();
            
            Write(PacketId.ServerPlayerScaleUpdate);

            Write(Scale);

            WriteLength();

            return this;
        }

        public void ReadPacket() {
            Scale = ReadVector3();
        }
    }
}