using System;
using System.Collections.Generic;

namespace Hkmp.Networking.Packet {
    public class AddonPacketData {
        public Dictionary<byte, IPacketData> PacketData { get; }

        public byte PacketIdSize { get; }

        public AddonPacketData(byte packetIdSize) {
            PacketData = new Dictionary<byte, IPacketData>();

            PacketIdSize = packetIdSize;
        }
    }

    /**
     * Class that stores information about addons that is needed to read addon data
     * from raw packet instances.
     */
    public class AddonPacketInfo {
        public Func<byte, IPacketData> PacketDataInstantiator { get; }
    
        public byte PacketIdSize { get; }
        
        public AddonPacketInfo(Func<byte, IPacketData> packetDataInstantiator, byte packetIdSize) {
            PacketDataInstantiator = packetDataInstantiator;
            PacketIdSize = packetIdSize;
        }
    }
}