using System;
using System.Collections.Generic;
using Hkmp.Collection;
using Hkmp.Networking.Packet;

namespace Hkmp.Api.Client.Networking;

/// <summary>
/// Base class for client addon network receivers.
/// </summary>
internal class ClientAddonNetworkReceiver {
    /// <summary>
    /// The instance of the client addon that this network receiver belongs to.
    /// </summary>
    protected readonly ClientAddon ClientAddon;

    /// <summary>
    /// The packet manager used to register packet handlers for the addon.
    /// </summary>
    protected readonly PacketManager PacketManager;

    /// <summary>
    /// Dictionary containing packet handlers for this addon.
    /// </summary>
    protected readonly Dictionary<byte, ClientPacketHandler> PacketHandlers;

    /// <summary>
    /// The packet instantiator for this network receiver.
    /// </summary>
    protected Func<byte, IPacketData> PacketInstantiator;

    /// <summary>
    /// The size of the packet ID space.
    /// </summary>
    protected byte PacketIdSize;

    protected ClientAddonNetworkReceiver(
        ClientAddon clientAddon,
        PacketManager packetManager
    ) {
        ClientAddon = clientAddon;
        PacketManager = packetManager;

        PacketHandlers = new Dictionary<byte, ClientPacketHandler>();
    }

    /// <summary>
    /// Commit all packet handlers in this class to the packet manager using the (now assigned) client addon ID.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the client addon ID is unassigned.</exception>
    public void CommitPacketHandlers() {
        if (!ClientAddon.Id.HasValue) {
            throw new InvalidOperationException("Client addon has no ID, can not commit packet handlers");
        }

        // Assign the addon packet info in the dictionary of the client update packet
        ClientUpdatePacket.AddonPacketInfoDict[ClientAddon.Id.Value] = new AddonPacketInfo(
            PacketInstantiator,
            PacketIdSize
        );

        foreach (var idHandlerPair in PacketHandlers) {
            PacketManager.RegisterClientAddonPacketHandler(
                ClientAddon.Id.Value,
                idHandlerPair.Key,
                idHandlerPair.Value
            );
        }
    }
}

/// <summary>
/// Implementation of client-side network receiver for addons.
/// </summary>
/// <typeparam name="TPacketId">The type of the packet ID enum.</typeparam>
internal class ClientAddonNetworkReceiver<TPacketId> :
    ClientAddonNetworkReceiver,
    IClientAddonNetworkReceiver<TPacketId>
    where TPacketId : Enum {
    /// <summary>
    /// A lookup for packet IDs and corresponding raw byte values.
    /// </summary>
    private readonly BiLookup<TPacketId, byte> _packetIdLookup;

    public ClientAddonNetworkReceiver(
        ClientAddon clientAddon,
        PacketManager packetManager
    ) : base(clientAddon, packetManager) {
        _packetIdLookup = AddonNetworkTransmitter.ConstructPacketIdLookup<TPacketId>();
    }

    /// <inheritdoc/>
    public void RegisterPacketHandler(TPacketId packetId, Action handler) {
        if (!_packetIdLookup.TryGetValue(packetId, out var idValue)) {
            throw new InvalidOperationException(
                "Given packet ID was not part of enum when creating this network receiver");
        }

        if (PacketHandlers.ContainsKey(idValue)) {
            throw new InvalidOperationException("There is already a packet handler for the given ID");
        }

        void ClientPacketHandler(IPacketData _) => handler();

        PacketHandlers[idValue] = ClientPacketHandler;
        if (ClientAddon.Id.HasValue) {
            PacketManager.RegisterClientAddonPacketHandler(
                ClientAddon.Id.Value,
                idValue,
                ClientPacketHandler
            );
        }
    }

    /// <inheritdoc/>
    public void RegisterPacketHandler<TPacketData>(
        TPacketId packetId,
        GenericClientPacketHandler<TPacketData> handler
    ) where TPacketData : IPacketData {
        if (!_packetIdLookup.TryGetValue(packetId, out var idValue)) {
            throw new InvalidOperationException(
                "Given packet ID was not part of enum when creating this network receiver");
        }

        if (PacketHandlers.ContainsKey(idValue)) {
            throw new InvalidOperationException("There is already a packet handler for the given ID");
        }

        void ClientPacketHandler(IPacketData iPacketData) => handler((TPacketData) iPacketData);

        PacketHandlers[idValue] = ClientPacketHandler;
        if (ClientAddon.Id.HasValue) {
            PacketManager.RegisterClientAddonPacketHandler(
                ClientAddon.Id.Value,
                idValue,
                ClientPacketHandler
            );
        }
    }

    /// <inheritdoc/>
    public void DeregisterPacketHandler(TPacketId packetId) {
        if (!_packetIdLookup.TryGetValue(packetId, out var idValue)) {
            throw new InvalidOperationException(
                "Given packet ID was not part of enum when creating this network receiver");
        }

        if (!PacketHandlers.ContainsKey(idValue)) {
            throw new InvalidOperationException("Could not remove nonexistent addon packet handler");
        }

        PacketHandlers.Remove(idValue);

        if (ClientAddon.Id.HasValue) {
            PacketManager.DeregisterClientAddonPacketHandler(ClientAddon.Id.Value, idValue);
        }
    }

    /// <summary>
    /// Assign the addon packet info in the base ClientAddonNetworkReceiver class for later use.
    /// </summary>
    /// <param name="packetInstantiator"></param>
    public void AssignAddonPacketInfo(Func<TPacketId, IPacketData> packetInstantiator) {
        PacketInstantiator = byteId => packetInstantiator(_packetIdLookup[byteId]);
        PacketIdSize = (byte) Enum.GetValues(typeof(TPacketId)).Length;
    }
}
