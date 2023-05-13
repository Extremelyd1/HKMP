using System;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet;

namespace Hkmp.Api.Client.Networking;

/// <summary>
/// Implementation of client-side network sender for addons.
/// </summary>
/// <typeparam name="TPacketId">The type of the packet ID enum.</typeparam>
internal class ClientAddonNetworkSender<TPacketId> :
    AddonNetworkTransmitter<TPacketId>,
    IClientAddonNetworkSender<TPacketId>
    where TPacketId : Enum {
    /// <summary>
    /// Message for the exception when the client is not connected.
    /// </summary>
    private const string NotConnectedMsg = "NetClient is not connected, cannot send data";

    /// <summary>
    /// Message for the exception when the given packet ID is invalid.
    /// </summary>
    private const string InvalidPacketIdMsg =
        "Given packet ID was not part of enum when creating this network sender";

    /// <summary>
    /// Message for the exception when the client addon has no ID.
    /// </summary>
    private const string NoClientAddonId = "Cannot send data when client addon has no ID";

    /// <summary>
    /// The net client used to send data.
    /// </summary>
    private readonly NetClient _netClient;

    /// <summary>
    /// The instance of the client addon that this network sender belongs to.
    /// </summary>
    private readonly ClientAddon _clientAddon;

    /// <summary>
    /// The size of the packet ID space.
    /// </summary>
    private readonly byte _packetIdSize;

    public ClientAddonNetworkSender(
        NetClient netClient,
        ClientAddon clientAddon
    ) {
        _netClient = netClient;
        _clientAddon = clientAddon;

        _packetIdSize = (byte) PacketIdLookup.Count;
    }

    /// <inheritdoc/>
    public void SendSingleData(TPacketId packetId, IPacketData packetData) {
        if (!_netClient.IsConnected) {
            throw new InvalidOperationException(NotConnectedMsg);
        }

        if (!PacketIdLookup.TryGetValue(packetId, out var idValue)) {
            throw new InvalidOperationException(
                InvalidPacketIdMsg);
        }

        if (!_clientAddon.Id.HasValue) {
            throw new InvalidOperationException(NoClientAddonId);
        }

        _netClient.UpdateManager.SetAddonData(
            _clientAddon.Id.Value,
            idValue,
            _packetIdSize,
            packetData
        );
    }

    /// <inheritdoc/>
    public void SendCollectionData<TPacketData>(
        TPacketId packetId,
        TPacketData packetData
    ) where TPacketData : IPacketData, new() {
        if (!_netClient.IsConnected) {
            throw new InvalidOperationException(NotConnectedMsg);
        }

        if (!PacketIdLookup.TryGetValue(packetId, out var idValue)) {
            throw new InvalidOperationException(
                InvalidPacketIdMsg);
        }

        if (!_clientAddon.Id.HasValue) {
            throw new InvalidOperationException(NoClientAddonId);
        }

        _netClient.UpdateManager.SetAddonDataAsCollection<TPacketData>(
            _clientAddon.Id.Value,
            idValue,
            _packetIdSize,
            packetData
        );
    }
}
