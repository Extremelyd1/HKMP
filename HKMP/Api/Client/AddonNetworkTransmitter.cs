using System;
using System.Collections.Generic;

namespace Hkmp.Api.Client {
    public abstract class AddonNetworkTransmitter<TPacketId> where TPacketId : Enum {
        protected readonly Dictionary<TPacketId, byte> PacketIdDict;

        protected AddonNetworkTransmitter() {
            PacketIdDict = new Dictionary<TPacketId, byte>();
            
            var packetIdValues = Enum.GetValues(typeof(TPacketId));
            for (byte i = 0; i < packetIdValues.Length; i++) {
                PacketIdDict[(TPacketId) packetIdValues.GetValue(i)] = i;
            }
        }
    }
}