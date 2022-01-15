using System;
using Hkmp.Networking.Packet;

namespace Hkmp.Api.Client {
    public interface INetClient {
        /**
         * Whether the client is currently connected to a server.
         */
        bool IsConnected { get; }

        /**
         * Get the network sender interface to send data over the network.
         */
        IAddonNetworkSender<TPacketId> GetNetworkSender<TPacketId>(
            ClientAddon addon
        ) where TPacketId : Enum;

        /**
         * Get the network receiver interface to register callbacks for receiving data over the network.
         */
        IClientAddonNetworkReceiver<TPacketId> GetNetworkReceiver<TPacketId>(
            ClientAddon addon,
            Func<byte, IPacketData> packetInstantiator
        ) where TPacketId : Enum;
    }
}