using System;
using System.Collections.Generic;

namespace Hkmp.Api.Client.Networking {
    /// <summary>
    /// Abstract base class for classes that transmit (send/receive) over the network.
    /// </summary>
    /// <typeparam name="TPacketId">The type of the packet ID enum.</typeparam>
    public abstract class AddonNetworkTransmitter<TPacketId> where TPacketId : Enum {
        /// <summary>
        /// A dictionary mapping packet ID enum values to raw bytes.
        /// </summary>
        protected readonly Dictionary<TPacketId, byte> PacketIdDict;

        /// <summary>
        /// A dictionary mapping raw bytes to packet ID enum values.
        /// </summary>
        protected readonly Dictionary<byte, TPacketId> ReversePacketIdDict;

        protected AddonNetworkTransmitter() {
            PacketIdDict = new Dictionary<TPacketId, byte>();
            ReversePacketIdDict = new Dictionary<byte, TPacketId>();
            
            // We add an entry in the dictionary for each value, so that we have
            // bytes 0, 1, 2, ..., n
            var packetIdValues = Enum.GetValues(typeof(TPacketId));
            for (byte i = 0; i < packetIdValues.Length; i++) {
                var packetId = (TPacketId)packetIdValues.GetValue(i);
                
                PacketIdDict[packetId] = i;
                ReversePacketIdDict[i] = packetId;
            }
        }
    }
}