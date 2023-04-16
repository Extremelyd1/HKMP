using System;
using Hkmp.Networking.Packet;

namespace Hkmp.Api.Client.Networking;

/// <summary>
/// Client-side network sender for addons.
/// </summary>
/// <typeparam name="TPacketId">The type of the packet ID enum.</typeparam>
public interface IClientAddonNetworkSender<in TPacketId> where TPacketId : Enum {
    /// <summary>
    /// Send a single instance of IPacketData over the network with the given packet ID.
    /// Calling this method again with the same packet ID will overwrite existing data for that packet ID
    /// if the packet has not yet been sent.
    /// </summary>
    /// <param name="packetId">The packet ID.</param>
    /// <param name="packetData">An instance of IPacketData to send.</param>
    void SendSingleData(TPacketId packetId, IPacketData packetData);

    /// <summary>
    /// Send an instance of IPacketData in a collection over the network with the given packet ID.
    /// Calling this method again with the same packet ID will add the instance of IPacketData to the existing
    /// collection if the packet has not yet been sent.
    /// </summary>
    /// <param name="packetId">The packet ID.</param>
    /// <param name="packetData">An instance of IPacketData to send.</param>
    /// <typeparam name="TPacketData">The type of the packetData parameter.</typeparam>
    void SendCollectionData<TPacketData>(
        TPacketId packetId,
        TPacketData packetData
    ) where TPacketData : IPacketData, new();
}
