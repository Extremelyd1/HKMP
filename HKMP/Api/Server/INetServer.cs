using System;

namespace Hkmp.Api.Server {
    /// <summary>
    /// The net server for all network-related interaction.
    /// </summary>
    public interface INetServer {
        /// <summary>
        /// Whether the server is currently started.
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// Get the network sender interface to send data over the network. Calling this method
        /// a second time will yield the same network sender instance if provided with the same
        /// generic parameter. Supplying a different generic parameter on subsequent calls will
        /// throw an exception as it is not supported.
        /// </summary>
        /// <param name="addon">The addon instance for which to get the sender.</param>
        /// <typeparam name="TPacketId">The network sender interface.</typeparam>
        /// <returns></returns>
        IServerAddonNetworkSender<TPacketId> GetNetworkSender<TPacketId>(
            ServerAddon addon
        ) where TPacketId : Enum;
    }
}