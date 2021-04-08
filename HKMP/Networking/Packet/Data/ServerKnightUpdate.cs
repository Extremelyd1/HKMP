using HKMP.Game;

namespace HKMP.Networking.Packet.Data {
    public class ClientServerKnightUpdate : IPacketData {

        public ushort Id { get; set; }

        public string Username { get; set; }

        public ushort Skin { get; set; }

        //public byte Emote { get; set; } = 255;

        public void WriteData(Packet packet) {
            packet.Write(Id);
            packet.Write(Username);
            packet.Write(Skin);
            //packet.Write(Emote);
        }

        public void ReadData(Packet packet) {
            Id = packet.ReadUShort();
            Username = packet.ReadString();
            Skin = packet.ReadUShort();
            //Emote = packet.ReadByte();

        }
    }
    
    public class ServerServerKnightUpdate : IPacketData {

        public ushort Id { get; set; }

        public string Username { get; set; }
        
        public ushort Skin { get; set; }

        //public byte Emote { get; set; } = 255;

        public void WriteData(Packet packet) {
            packet.Write(Id);
            packet.Write(Username);
            packet.Write(Skin);
           // packet.Write(Emote);
        }

        public void ReadData(Packet packet) {
            Id = packet.ReadUShort();
            Username = packet.ReadString();
            Skin = packet.ReadUShort();
           // Emote = packet.ReadByte();
        }
    }
}