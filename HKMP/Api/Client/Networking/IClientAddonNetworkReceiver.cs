using System;
using Hkmp.Networking.Packet;

namespace Hkmp.Api.Client.Networking;

/// <summary>
/// Client-side network receiver for addons.
/// </summary>
/// <typeparam name="TPacketId">The type of the packet ID enum.</typeparam>
public interface IClientAddonNetworkReceiver<in TPacketId> where TPacketId : Enum {
    /// <summary>
    /// Registers a data independent handler for a packet with a specific ID.
    /// The given action will not get the packet data as parameter.
    /// </summary>
    /// <param name="packetId">The packet ID.</param>
    /// <param name="handler">The action to be used as handler.</param>
    void RegisterPacketHandler(TPacketId packetId, Action handler);

    /// <summary>
    /// Registers a handler for a packet with specific type and ID.
    /// </summary>
    /// <param name="packetId">The packet ID.</param>
    /// <param name="handler">The delegate instance with packet data parameter to be used as handler.</param>
    /// <typeparam name="TPacketData">The type of the packet data.</typeparam>
    void RegisterPacketHandler<TPacketData>(
        TPacketId packetId,
        GenericClientPacketHandler<TPacketData> handler
    ) where TPacketData : IPacketData;

    /// <summary>
    /// De-registers the handler for the given packet ID.
    /// </summary>
    /// <param name="packetId">The packet ID.</param>
    void DeregisterPacketHandler(TPacketId packetId);
}
