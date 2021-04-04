using HKMP.Game;

namespace HKMP.Networking.Packet.Data {
    public class ClientPlayerTeamUpdate : IPacketData {

        public ushort Id { get; set; }

        public string Username { get; set; }

        public Team Team { get; set; }

        public void WriteData(Packet packet) {
            packet.Write(Id);
            packet.Write(Username);
            packet.Write((byte) Team);
        }

        public void ReadData(Packet packet) {
            Id = packet.ReadUShort();
            Username = packet.ReadString();
            Team = (Team) packet.ReadByte();
        }
    }
    
    public class ServerPlayerTeamUpdate : IPacketData {

        public Team Team { get; set; }

        public void WriteData(Packet packet) {
            packet.Write((byte) Team);
        }

        public void ReadData(Packet packet) {
            Team = (Team) packet.ReadByte();
        }
    }
}