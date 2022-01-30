using System.Collections.Generic;

namespace Hkmp.Networking.Packet.Data {
    public class HelloClient : IPacketData {
        public bool IsReliable => true;

        public bool DropReliableDataIfNewerExists => true;

        public List<(ushort, string)> ClientInfo { get; set; }

        public HelloClient() {
            ClientInfo = new List<(ushort, string)>();
        }

        public void WriteData(IPacket packet) {
            packet.Write((ushort)ClientInfo.Count);

            foreach (var (id, username) in ClientInfo) {
                packet.Write(id);
                packet.Write(username);
            }
        }

        public void ReadData(IPacket packet) {
            var length = packet.ReadUShort();

            for (var i = 0; i < length; i++) {
                ClientInfo.Add((
                    packet.ReadUShort(),
                    packet.ReadString()
                ));
            }
        }
    }
}