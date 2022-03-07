using System;
using Hkmp.Collection;

namespace Hkmp.Api.Client.Networking {
    /// <summary>
    /// Abstract base class for classes that transmit (send/receive) over the network.
    /// </summary>
    /// <typeparam name="TPacketId">The type of the packet ID enum.</typeparam>
    public abstract class AddonNetworkTransmitter<TPacketId> where TPacketId : Enum {
        /// <summary>
        /// A lookup for packet IDs and corresponding raw byte values.
        /// </summary>
        protected readonly BiLookup<TPacketId, byte> PacketIdLookup;
        
        protected AddonNetworkTransmitter() {
            PacketIdLookup = new BiLookup<TPacketId, byte>();

            // We add an entry in the dictionary for each value, so that we have
            // bytes 0, 1, 2, ..., n
            var packetIdValues = Enum.GetValues(typeof(TPacketId));
            for (byte i = 0; i < packetIdValues.Length; i++) {
                var packetId = (TPacketId)packetIdValues.GetValue(i);

                PacketIdLookup.Add(packetId, i);
            }
        }
    }
}