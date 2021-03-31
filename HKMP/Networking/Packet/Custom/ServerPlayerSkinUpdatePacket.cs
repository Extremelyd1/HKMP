using HKMP.Game;

namespace HKMP.Networking.Packet.Custom {
    public class ServerPlayerSkinUpdatePacket : Packet, IPacket {

        public int Skin { get; set; }
        
        public ServerPlayerSkinUpdatePacket() {
        }

        public ServerPlayerSkinUpdatePacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();

            Write(PacketId.PlayerSkinUpdate);
            
            Write(Skin);
            
            WriteLength();

            return this;
        }

        public void ReadPacket() {
            Skin = ReadInt();
        }
    }
}