using HKMP.Game;

namespace HKMP.Networking.Packet.Custom {
    public class ServerPlayerTeamUpdatePacket : Packet, IPacket {

        public Team Team { get; set; }
        
        public ServerPlayerTeamUpdatePacket() {
        }

        public ServerPlayerTeamUpdatePacket(Packet packet) : base(packet) {
        }
        
        public Packet CreatePacket() {
            Reset();

            Write(PacketId.PlayerTeamUpdate);
            
            Write((byte) Team);
            
            WriteLength();

            return this;
        }

        public void ReadPacket() {
            Team = (Team) ReadByte();
        }
    }
}