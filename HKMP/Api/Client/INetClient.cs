using System;
using Hkmp.Networking.Packet;

namespace Hkmp.Api.Client {
    /// <summary>
    /// The net client for all network-related interaction.
    /// </summary>
    public interface INetClient {
        /// <summary>
        /// Whether the client is currently connected to a server.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Get the network sender interface to send data over the network.
        /// </summary>
        /// <param name="addon">The addon instance for which to get the sender.</param>
        /// <typeparam name="TPacketId">The type of the packet ID enum.</typeparam>
        /// <returns></returns>
        IAddonNetworkSender<TPacketId> GetNetworkSender<TPacketId>(
            ClientAddon addon
        ) where TPacketId : Enum;

        // TODO: change function parameter from byte to TPacketID and propagate changes accordingly
        /// <summary>
        /// Get the network receiver interface to register callbacks for receiving data over the network.
        /// </summary>
        /// <param name="addon">The addon instance for which to get the receiver.</param>
        /// <param name="packetInstantiator">A function that instantiates IPacketData instances from a packet ID.</param>
        /// <typeparam name="TPacketId"></typeparam>
        /// <returns></returns>
        IClientAddonNetworkReceiver<TPacketId> GetNetworkReceiver<TPacketId>(
            ClientAddon addon,
            Func<TPacketId, IPacketData> packetInstantiator
        ) where TPacketId : Enum;
    }
}