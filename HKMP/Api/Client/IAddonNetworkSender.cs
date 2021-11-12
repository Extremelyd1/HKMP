using System;
using Hkmp.Networking.Packet;

namespace Hkmp.Api.Client {
    public interface IAddonNetworkSender<in TPacketId> where TPacketId : Enum {

        /**
         * Send a single instance of IPacketData over the network with the given packet ID.
         * Calling this method again with the same packet ID will overwrite existing data for that packet ID
         * if the packet has not yet been sent.
         */
        void SendSingleData(TPacketId packetId, IPacketData packetData);

        /**
         * Send an instance of IPacketData in a collection over the network with the given packet ID.
         * Calling this method again with the same packet ID will add the instance of IPacketData to the existing
         * collection if the packet has not yet been sent.
         */
        void SendCollectionData<TPacketData>(
            TPacketId packetId, 
            IPacketData packetData
        ) where TPacketData : IPacketData, new();

    }
}