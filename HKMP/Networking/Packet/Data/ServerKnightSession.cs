using HKMP.Game;
using HKMP.ServerKnights;

namespace HKMP.Networking.Packet.Data {
    public class ServerKnightSession : IPacketData {

        public string Name { get; set; }
        public string Host { get; set; }
        public string skin_1 { get; set; }
        public string skin_2 { get; set; }
        public string skin_3 { get; set; }
        public string skin_4 { get; set; }
        public string skin_5 { get; set; }
        public string skin_6 { get; set; }
        public string skin_7 { get; set; }
        public string skin_8 { get; set; }
        public string skin_9 { get; set; }

        public void setSession(serverJson session){
            Name = session.Name;
            Host = session.Host;
            skin_1 = session.skin_1;
            skin_2 = session.skin_2;
            skin_3 = session.skin_3;
            skin_4 = session.skin_4;
            skin_5 = session.skin_5;
            skin_6 = session.skin_6;
            skin_7 = session.skin_7;
            skin_8 = session.skin_8;
            skin_9 = session.skin_9;
        }

        public void WriteData(Packet packet) {
            packet.Write(Name);
            packet.Write(Host);
            packet.Write(skin_1);
            packet.Write(skin_2);
            packet.Write(skin_3);
            packet.Write(skin_4);
            packet.Write(skin_5);
            packet.Write(skin_6);
            packet.Write(skin_7);
            packet.Write(skin_8);
            packet.Write(skin_9);
        }

        public void ReadData(Packet packet) {
            Name  = packet.ReadString();
            Host  = packet.ReadString();
            skin_1= packet.ReadString();
            skin_2= packet.ReadString();
            skin_3= packet.ReadString();
            skin_4= packet.ReadString();
            skin_5= packet.ReadString();
            skin_6= packet.ReadString();
            skin_7= packet.ReadString();
            skin_8= packet.ReadString();
            skin_9= packet.ReadString();
        }
    }
 
}