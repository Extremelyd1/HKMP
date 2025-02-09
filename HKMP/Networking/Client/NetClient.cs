using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using Hkmp.Api.Client;
using Hkmp.Api.Client.Networking;
using Hkmp.Logging;
using Hkmp.Networking.Chunk;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using Hkmp.Networking.Packet.Update;
using Hkmp.Util;

namespace Hkmp.Networking.Client;

/// <summary>
/// The networking client that manages the UDP client for sending and receiving data. This only
/// manages client side networking, e.g. sending to and receiving from the server.
/// </summary>
internal class NetClient : INetClient {
    /// <summary>
    /// The packet manager instance.
    /// </summary>
    private readonly PacketManager _packetManager;

    /// <summary>
    /// The client update manager for this net client.
    /// </summary>
    public ClientUpdateManager UpdateManager { get; }

    /// <summary>
    /// Event that is called when the client connects to a server.
    /// </summary>
    public event Action<ServerInfo> ConnectEvent;

    /// <summary>
    /// Event that is called when the client fails to connect to a server.
    /// </summary>
    public event Action<ConnectionFailedResult> ConnectFailedEvent;

    /// <summary>
    /// Event that is called when the client disconnects from a server.
    /// </summary>
    public event Action DisconnectEvent;

    /// <summary>
    /// Event that is called when the client times out from a connection.
    /// </summary>
    public event Action TimeoutEvent;

    /// <summary>
    /// The connection status of the client.
    /// </summary>
    public ClientConnectionStatus ConnectionStatus { get; private set; } = ClientConnectionStatus.NotConnected;

    /// <inheritdoc />
    public bool IsConnected => ConnectionStatus == ClientConnectionStatus.Connected;

    /// <summary>
    /// The DTLS client instance for handling DTLS connections.
    /// </summary>
    private readonly DtlsClient _dtlsClient;

    /// <summary>
    /// Chunk sender instance for sending large amounts of data.
    /// </summary>
    private readonly ClientChunkSender _chunkSender;
    /// <summary>
    /// Chunk receiver instance for receiving large amounts of data.
    /// </summary>
    private readonly ClientChunkReceiver _chunkReceiver;

    /// <summary>
    /// The client connection manager responsible for handling sending and receiving connection data.
    /// </summary>
    private readonly ClientConnectionManager _connectionManager;

    /// <summary>
    /// Byte array containing received data that was not included in a packet object yet.
    /// </summary>
    private byte[] _leftoverData;

    /// <summary>
    /// Construct the net client with the given packet manager.
    /// </summary>
    /// <param name="packetManager">The packet manager instance.</param>
    public NetClient(PacketManager packetManager) {
        _packetManager = packetManager;

        _dtlsClient = new DtlsClient();

        UpdateManager = new ClientUpdateManager();

        _chunkSender = new ClientChunkSender(UpdateManager);
        _chunkReceiver = new ClientChunkReceiver(UpdateManager);
        _connectionManager = new ClientConnectionManager(_packetManager, _chunkSender, _chunkReceiver);

        _dtlsClient.DataReceivedEvent += OnReceiveData;
        _connectionManager.ServerInfoReceivedEvent += OnServerInfoReceived;
    }
    
    /// <summary>
    /// Starts establishing a connection with the given host on the given port.
    /// </summary>
    /// <param name="address">The address of the host to connect to.</param>
    /// <param name="port">The port of the host to connect to.</param>
    /// <param name="username">The username of the client.</param>
    /// <param name="authKey">The auth key of the client.</param>
    /// <param name="addonData">A list of addon data that the client has.</param>
    public void Connect(
        string address,
        int port,
        string username,
        string authKey,
        List<AddonData> addonData
    ) {
        Logger.Debug($"Trying to connect NetClient to '{address}:{port}'");
        ConnectionStatus = ClientConnectionStatus.Connecting;

        try {
            _dtlsClient.Connect(address, port);
        } catch (SocketException e) {
            Logger.Error($"Failed to connect due to SocketException:\n{e}");

            ConnectFailedEvent?.Invoke(new ConnectionFailedResult {
                Reason = ConnectionFailedReason.SocketException
            });
            return;
        } catch (IOException e) {
            Logger.Error($"Failed to connect due to IOException:\n{e}");

            ConnectFailedEvent?.Invoke(new ConnectionFailedResult {
                Reason = ConnectionFailedReason.IOException
            });
            return;
        }

        UpdateManager.DtlsTransport = _dtlsClient.DtlsTransport;
        // During the connection process we register the connection failed callback if we time out
        UpdateManager.TimeoutEvent += OnConnectTimedOut;
        
        UpdateManager.StartUpdates();

        Logger.Debug("Starting connection with connection manager");
        _chunkSender.Start();
        _connectionManager.StartConnection(username, authKey, addonData);
    }

    /// <summary>
    /// Disconnect from the current server.
    /// </summary>
    public void Disconnect() {
        UpdateManager.StopUpdates();
        _chunkSender.Stop();
        
        _dtlsClient.Disconnect();

        ConnectionStatus = ClientConnectionStatus.NotConnected;

        // Clear all client addon packet handlers, because their IDs become invalid
        _packetManager.ClearClientAddonUpdatePacketHandlers();

        // Invoke callback if it exists
        DisconnectEvent?.Invoke();
    }

    /// <summary>
    /// Callback method for when the DTLS client receives data. This will update the update manager that we have
    /// received data, handle packet creation from raw data, handle login responses, and forward received packets to
    /// the packet manager.
    /// </summary>
    /// <param name="buffer">Byte array containing the received bytes.</param>
    /// <param name="length">The number of bytes in the <paramref name="buffer"/>.</param>
    private void OnReceiveData(byte[] buffer, int length) {
        if (ConnectionStatus == ClientConnectionStatus.NotConnected) {
            Logger.Error("Client is not connected to a server, but received data, ignoring");
            return;
        }
        
        var packets = PacketManager.HandleReceivedData(buffer, length, ref _leftoverData);
        
        foreach (var packet in packets) {
            // Create a ClientUpdatePacket from the raw packet instance,
            // and read the values into it
            var clientUpdatePacket = new ClientUpdatePacket();
            if (!clientUpdatePacket.ReadPacket(packet)) {
                // If ReadPacket returns false, we received a malformed packet, which we simply ignore for now
                continue;
            }

            UpdateManager.OnReceivePacket<ClientUpdatePacket, ClientUpdatePacketId>(clientUpdatePacket);

            // First check for slice or slice ack data and handle it separately by passing it onto either the chunk 
            // sender or chunk receiver
            var packetData = clientUpdatePacket.GetPacketData();
            if (packetData.TryGetValue(ClientUpdatePacketId.Slice, out var sliceData)) {
                packetData.Remove(ClientUpdatePacketId.Slice);
                _chunkReceiver.ProcessReceivedData((SliceData) sliceData);
            }

            if (packetData.TryGetValue(ClientUpdatePacketId.SliceAck, out var sliceAckData)) {
                packetData.Remove(ClientUpdatePacketId.SliceAck);
                _chunkSender.ProcessReceivedData((SliceAckData) sliceAckData);
            }

            // Then, if we are already connected to a server, we let the packet manager handle the rest of the packet
            // data
            if (ConnectionStatus == ClientConnectionStatus.Connected) {
                _packetManager.HandleClientUpdatePacket(clientUpdatePacket);
            }
        }
    }
    
    private void OnServerInfoReceived(ServerInfo serverInfo) {
        if (serverInfo.ConnectionResult == ServerConnectionResult.Accepted) {
            Logger.Debug("Connection to server accepted");
            
            // De-register the "connect failed" and register the actual timeout handler if we time out
            UpdateManager.TimeoutEvent -= OnConnectTimedOut;
            UpdateManager.TimeoutEvent += () => {
                ThreadUtil.RunActionOnMainThread(() => { TimeoutEvent?.Invoke(); });
            };

            // Invoke callback if it exists on the main thread of Unity
            ThreadUtil.RunActionOnMainThread(() => { ConnectEvent?.Invoke(serverInfo); });

            ConnectionStatus = ClientConnectionStatus.Connected;
            return;
        }

        ConnectionFailedResult result;

        if (serverInfo.ConnectionResult == ServerConnectionResult.InvalidAddons) {
            Logger.Debug("Connection to server failed due to invalid addons");
            
            result = new ConnectionInvalidAddonsResult {
                Reason = ConnectionFailedReason.InvalidAddons,
                AddonData = serverInfo.AddonData
            };
        } else if (serverInfo.ConnectionResult == ServerConnectionResult.RejectedOther) {
            Logger.Debug($"Connection to server failed, message: {serverInfo.ConnectionRejectedMessage}");

            result = new ConnectionFailedMessageResult {
                Reason = ConnectionFailedReason.Other,
                Message = serverInfo.ConnectionRejectedMessage
            };
        } else {
            throw new NotImplementedException("Unknown connection result in server info");
        }
        
        UpdateManager?.StopUpdates();

        ConnectionStatus = ClientConnectionStatus.NotConnected;
        
        // Invoke callback if it exists on the main thread of Unity
        ThreadUtil.RunActionOnMainThread(() => { ConnectFailedEvent?.Invoke(result); });
    }
    
    /// <summary>
    /// Callback method for when the client connection times out.
    /// </summary>
    private void OnConnectTimedOut() => ConnectFailedEvent?.Invoke(new ConnectionFailedResult {
        Reason = ConnectionFailedReason.TimedOut
    });

    /// <inheritdoc />
    public IClientAddonNetworkSender<TPacketId> GetNetworkSender<TPacketId>(
        ClientAddon addon
    ) where TPacketId : Enum {
        if (addon == null) {
            throw new ArgumentException("Parameter 'addon' cannot be null");
        }

        // Check whether this addon has actually requested network access through their property
        // We check this otherwise an ID has not been assigned and it can't send network data
        if (!addon.NeedsNetwork) {
            throw new InvalidOperationException("Addon has not requested network access through property");
        }

        // Check whether there already is a network sender for the given addon
        if (addon.NetworkSender != null) {
            if (!(addon.NetworkSender is IClientAddonNetworkSender<TPacketId> addonNetworkSender)) {
                throw new InvalidOperationException(
                    "Cannot request network senders with differing generic parameters");
            }

            return addonNetworkSender;
        }

        // Otherwise create one, store it and return it
        var newAddonNetworkSender = new ClientAddonNetworkSender<TPacketId>(this, addon);
        addon.NetworkSender = newAddonNetworkSender;

        return newAddonNetworkSender;
    }

    /// <inheritdoc />
    public IClientAddonNetworkReceiver<TPacketId> GetNetworkReceiver<TPacketId>(
        ClientAddon addon,
        Func<TPacketId, IPacketData> packetInstantiator
    ) where TPacketId : Enum {
        if (addon == null) {
            throw new ArgumentException("Parameter 'addon' cannot be null");
        }

        if (packetInstantiator == null) {
            throw new ArgumentException("Parameter 'packetInstantiator' cannot be null");
        }

        // Check whether this addon has actually requested network access through their property
        // We check this otherwise an ID has not been assigned and it can't send network data
        if (!addon.NeedsNetwork) {
            throw new InvalidOperationException("Addon has not requested network access through property");
        }

        ClientAddonNetworkReceiver<TPacketId> networkReceiver = null;

        // Check whether an existing network receiver exists
        if (addon.NetworkReceiver == null) {
            networkReceiver = new ClientAddonNetworkReceiver<TPacketId>(addon, _packetManager);
            addon.NetworkReceiver = networkReceiver;
        } else if (!(addon.NetworkReceiver is IClientAddonNetworkReceiver<TPacketId>)) {
            throw new InvalidOperationException(
                "Cannot request network receivers with differing generic parameters");
        }

        networkReceiver?.AssignAddonPacketInfo(packetInstantiator);

        return addon.NetworkReceiver as IClientAddonNetworkReceiver<TPacketId>;
    }
}
