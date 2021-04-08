using HKMP.Game;

namespace HKMP.Networking.Packet.Custom {
    public class ClientPlayerSkinUpdatePacket : Packet, IPacket {

        public ushort Id { get; set; }

        public string Username { get; set; }

        public Team Team { get; set; }
        
        public ushort Skin { get; set; }
        
        public ClientPlayerSkinUpdatePacket() {
        }

        public ClientPlayerSkinUpdatePacket(Packet packet) : base(packet) {
        }

        public Packet CreatePacket() {
            Reset();
            Write(PacketId.PlayerSkinUpdate);
            Write(Id);
            Write(Username);
            Write((byte) Team);
            Write(Skin);
            WriteLength();
            return this;
        }

        public void ReadPacket() {
            Id = ReadUShort();
            Username = ReadString();
            Team = (Team) ReadByte();
            Skin = ReadUShort();
        }
    }
}