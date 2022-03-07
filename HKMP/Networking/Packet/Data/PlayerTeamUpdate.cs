using Hkmp.Game;

namespace Hkmp.Networking.Packet.Data {
    public class ClientPlayerTeamUpdate : GenericClientData {

        public string Username { get; set; }

        public Team Team { get; set; }

        public ClientPlayerTeamUpdate() {
            IsReliable = true;
            DropReliableDataIfNewerExists = true;
        }
        
        public override void WriteData(IPacket packet) {
            packet.Write(Id);
            packet.Write(Username);
            packet.Write((byte) Team);
        }

        public override void ReadData(IPacket packet) {
            Id = packet.ReadUShort();
            Username = packet.ReadString();
            Team = (Team) packet.ReadByte();
        }
    }

    public class ServerPlayerTeamUpdate : IPacketData {
        public bool IsReliable => true;
        
        public bool DropReliableDataIfNewerExists => true;
        
        public Team Team { get; set; }

        public void WriteData(IPacket packet) {
            packet.Write((byte) Team);
        }

        public void ReadData(IPacket packet) {
            Team = (Team) packet.ReadByte();
        }
    }
}