using System.Collections.Generic;

namespace Hkmp.Networking.Packet {
    public class AddonPacketData {
        public int PacketIdSize { get; }

        public Dictionary<int, IPacketData> PacketData { get; }

        public AddonPacketData(int packetIdSize) {
            PacketIdSize = packetIdSize;
            PacketData = new Dictionary<int, IPacketData>();
        }
    }
}