using System;
using Hkmp.Networking.Packet;

namespace Hkmp.Api.Client {
    public interface IClientAddonNetworkReceiver<TPacketId> where TPacketId : Enum {
        /**
         * Register a handler for a packet with a specific ID.
         */
        void RegisterPacketHandler(TPacketId packetId, Action handler);

        /**
         * Register a handler for a packet with specific type and ID.
         */
        void RegisterPacketHandler<TPacketData>(
            TPacketId packetId,
            GenericClientPacketHandler<TPacketData> handler
        ) where TPacketData : IPacketData;
    }
}