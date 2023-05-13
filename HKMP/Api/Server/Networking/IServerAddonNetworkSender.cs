using System;
using Hkmp.Networking.Packet;

namespace Hkmp.Api.Server.Networking;

/// <summary>
/// Server-side network sender for addons.
/// </summary>
public interface IServerAddonNetworkSender<in TPacketId> where TPacketId : Enum {
    /// <summary>
    /// Send a single instance of IPacketData with the given packet ID over the network to the player
    /// with the given ID. Calling this method again with the same packet ID will overwrite existing
    /// data for that packet if the packet has not yet been sent.
    /// </summary>
    /// <param name="packetId">The packet ID.</param>
    /// <param name="packetData">An instance of IPacketData to send.</param>
    /// <param name="playerId">The ID of the player.</param>
    void SendSingleData(TPacketId packetId, IPacketData packetData, ushort playerId);

    /// <summary>
    /// Send a single instance of IPacketData with the given packet ID over the network to the players
    /// with the given IDs. Calling this method again with the same packet ID will overwrite existing
    /// data for that packet if the packet has not yet been sent.
    /// </summary>
    /// <param name="packetId">The packet ID.</param>
    /// <param name="packetData">An instance of IPacketData to send.</param>
    /// <param name="playerIds">The IDs of the players.</param>
    void SendSingleData(TPacketId packetId, IPacketData packetData, params ushort[] playerIds);

    /// <summary>
    /// Send a single instance of IPacketData with the given packet ID over the network to all connected
    /// players. Calling this method again with the same packet ID will overwrite existing
    /// data for that packet if the packet has not yet been sent.
    /// </summary>
    /// <param name="packetId">The packet ID.</param>
    /// <param name="packetData">An instance of IPacketData to send.</param>
    void BroadcastSingleData(TPacketId packetId, IPacketData packetData);

    /// <summary>
    /// Send an instance of IPacketData in a collection with the given packet ID over the network to the
    /// player with the given ID. Calling this method again with the same packet ID will add the instance
    /// of IPacketData to the existing collection if the packet has not yet been sent.
    /// </summary>
    /// <param name="packetId">The packet ID.</param>
    /// <param name="packetData">An instance of IPacketData to send.</param>
    /// <param name="playerId">The ID of the player.</param>
    /// <typeparam name="TPacketData">The type of the packetData parameter.</typeparam>
    void SendCollectionData<TPacketData>(
        TPacketId packetId,
        TPacketData packetData,
        ushort playerId
    ) where TPacketData : IPacketData, new();

    /// <summary>
    /// Send an instance of IPacketData in a collection with the given packet ID over the network to the
    /// players with the given IDs. Calling this method again with the same packet ID will add the instance
    /// of IPacketData to the existing collection if the packet has not yet been sent.
    /// </summary>
    /// <param name="packetId">The packet ID.</param>
    /// <param name="packetData">An instance of IPacketData to send.</param>
    /// <param name="playerIds">The IDs of the players.</param>
    /// <typeparam name="TPacketData">The type of the packetData parameter.</typeparam>
    void SendCollectionData<TPacketData>(
        TPacketId packetId,
        TPacketData packetData,
        params ushort[] playerIds
    ) where TPacketData : IPacketData, new();

    /// <summary>
    /// Send an instance of IPacketData in a collection with the given packet ID over the network to all
    /// connected players. Calling this method again with the same packet ID will add the instance
    /// of IPacketData to the existing collection if the packet has not yet been sent.
    /// </summary>
    /// <param name="packetId">The packet ID.</param>
    /// <param name="packetData">An instance of IPacketData to send.</param>
    /// <typeparam name="TPacketData">The type of the packetData parameter.</typeparam>
    void BroadcastCollectionData<TPacketData>(
        TPacketId packetId,
        TPacketData packetData
    ) where TPacketData : IPacketData, new();
}
