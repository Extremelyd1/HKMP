using System;
using Hkmp.Networking.Packet;

namespace Hkmp.Api.Server.Networking;

/// <summary>
/// Server-side network receiver for addons.
/// </summary>
/// <typeparam name="TPacketId">The type of the packet ID enum.</typeparam>
public interface IServerAddonNetworkReceiver<in TPacketId> where TPacketId : Enum {
    /// <summary>
    /// Registers a data independent handler for a packet with a specific ID.
    /// The given action will not get the packet data as parameter, but will get the player ID.
    /// </summary>
    /// <param name="packetId">The packet ID.</param>
    /// <param name="handler">The action with player ID parameter to be used as handler.</param>
    void RegisterPacketHandler(TPacketId packetId, Action<ushort> handler);

    /// <summary>
    /// Registers a handler for a packet with a specific ID.
    /// </summary>
    /// <param name="packetId">The packet ID.</param>
    /// <param name="handler">The delegate instance with packet data parameter to be used as handler.</param>
    /// <typeparam name="TPacketData">The type of the packet data.</typeparam>
    void RegisterPacketHandler<TPacketData>(
        TPacketId packetId,
        GenericServerPacketHandler<TPacketData> handler
    ) where TPacketData : IPacketData;

    /// <summary>
    /// De-registers the handler for the given packet ID.
    /// </summary>
    /// <param name="packetId">The packet ID.</param>
    void DeregisterPacketHandler(TPacketId packetId);
}
