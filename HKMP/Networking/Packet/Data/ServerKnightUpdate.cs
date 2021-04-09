using HKMP.Game;

namespace HKMP.Networking.Packet.Data {

    public class ClientServerKnightUpdate : IPacketData {

        public ushort Id { get; set; }

        public string Username { get; set; }

        public ushort Skin { get; set; }

        public ushort Emote { get; set; }

        public void WriteData(Packet packet) {
            packet.Write(Id);
            packet.Write(Username);
            packet.Write(Skin);
            packet.Write(Emote);
            Logger.Info(this,$"Write CSKU {Id} {Username} {Skin}");

        }

        public void ReadData(Packet packet) {
            Id = packet.ReadUShort();
            Username = packet.ReadString();
            Skin = packet.ReadUShort();
            Emote = packet.ReadUShort();
            Logger.Info(this,$"Read CSKU {Id} {Username} {Skin}");

        }
    }
    
    public class ServerServerKnightUpdate : IPacketData {


        public bool isSkin { get; set; }
        public bool isEmote { get; set; }

        public ushort Skin { get; set; }

        public ushort Emote { get; set; }

        public void WriteData(Packet packet) {
            packet.Write(isSkin);  
            packet.Write(isEmote);      
            packet.Write(Skin);
            packet.Write(Emote);
        }

        public void ReadData(Packet packet) {
            isSkin = packet.ReadBool();
            isEmote = packet.ReadBool();
            Skin = packet.ReadUShort();
            Emote = packet.ReadUShort();
        }
    }
}