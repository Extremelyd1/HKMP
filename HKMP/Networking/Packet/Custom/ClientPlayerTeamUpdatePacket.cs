using HKMP.Game;

namespace HKMP.Networking.Packet.Custom {
    public class ClientPlayerTeamUpdatePacket : Packet, IPacket {

        public ushort Id { get; set; }

        public string Username { get; set; }

        public Team Team { get; set; }
        
        public ClientPlayerTeamUpdatePacket() {
        }

        public ClientPlayerTeamUpdatePacket(Packet packet) : base(packet) {
        }
        
        public int Skin { get; set; }
    

        public Packet CreatePacket() {
            Reset();

            Write(PacketId.PlayerTeamUpdate);

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
            Skin = ReadInt();
        }
    }
}