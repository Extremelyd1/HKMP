using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Hkmp.Api.Client;
using Hkmp.Api.Client.Networking;
using Hkmp.Logging;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
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
    public ClientUpdateManager UpdateManager { get; private set; }

    /// <summary>
    /// Event that is called when the client connects to a server.
    /// </summary>
    public event Action<LoginResponse> ConnectEvent;

    /// <summary>
    /// Event that is called when the client fails to connect to a server.
    /// </summary>
    public event Action<ConnectFailedResult> ConnectFailedEvent;

    /// <summary>
    /// Event that is called when the client disconnects from a server.
    /// </summary>
    public event Action DisconnectEvent;

    /// <summary>
    /// Event that is called when the client times out from a connection.
    /// </summary>
    public event Action TimeoutEvent;

    /// <summary>
    /// Boolean denoting whether the client is connected to a server.
    /// </summary>
    public bool IsConnected { get; private set; }
    
    /// <summary>
    /// Boolean denoting whether the client is currently attempting connection.
    /// </summary>
    public bool IsConnecting { get; private set; }

    /// <summary>
    /// Cancellation token source for the update task.
    /// </summary>
    private readonly CancellationTokenSource _updateTaskTokenSource;

    /// <summary>
    /// The DTLS client instance for handling DTLS connections.
    /// </summary>
    private readonly DtlsClient _dtlsClient;
    
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

        _updateTaskTokenSource = new CancellationTokenSource();
        _dtlsClient = new DtlsClient();

        _dtlsClient.DataReceivedEvent += OnReceiveData;
    }

    /// <summary>
    /// Callback method for when the client receives a login response from a server connection.
    /// </summary>
    /// <param name="loginResponse">The LoginResponse packet data.</param>
    private void OnConnect(LoginResponse loginResponse) {
        Logger.Debug("Connection to server success");

        // De-register the "connect failed" and register the actual timeout handler if we time out
        UpdateManager.OnTimeout -= OnConnectTimedOut;
        UpdateManager.OnTimeout += () => { ThreadUtil.RunActionOnMainThread(() => { TimeoutEvent?.Invoke(); }); };

        // Invoke callback if it exists on the main thread of Unity
        ThreadUtil.RunActionOnMainThread(() => { ConnectEvent?.Invoke(loginResponse); });

        IsConnected = true;
        IsConnecting = false;
    }

    /// <summary>
    /// Callback method for when the client connection times out.
    /// </summary>
    private void OnConnectTimedOut() => OnConnectFailed(new ConnectFailedResult {
        Type = ConnectFailedResult.FailType.TimedOut
    });

    /// <summary>
    /// Callback method for when the client connection fails.
    /// </summary>
    /// <param name="result">The connection failed result.</param>
    private void OnConnectFailed(ConnectFailedResult result) {
        Logger.Debug($"Connection to server failed, cause: {result.Type}");

        UpdateManager?.StopUpdates();

        IsConnected = false;
        IsConnecting = false;

        // Request cancellation for the update task
        _updateTaskTokenSource?.Cancel();

        // Invoke callback if it exists on the main thread of Unity
        ThreadUtil.RunActionOnMainThread(() => { ConnectFailedEvent?.Invoke(result); });
    }

    /// <summary>
    /// Callback method for when the DTLS client receives data. This will update the update manager that we have
    /// received data, handle packet creation from raw data, handle login responses, and forward received packets to
    /// the packet manager.
    /// </summary>
    /// <param name="buffer">Byte array containing the received bytes.</param>
    /// <param name="length">The number of bytes in the <paramref name="buffer"/>.</param>
    private void OnReceiveData(byte[] buffer, int length) {
        var packets = PacketManager.HandleReceivedData(buffer, length, ref _leftoverData);
        
        foreach (var packet in packets) {
            // Create a ClientUpdatePacket from the raw packet instance,
            // and read the values into it
            var clientUpdatePacket = new ClientUpdatePacket(packet);
            if (!clientUpdatePacket.ReadPacket()) {
                // If ReadPacket returns false, we received a malformed packet, which we simply ignore for now
                continue;
            }

            UpdateManager.OnReceivePacket<ClientUpdatePacket, ClientPacketId>(clientUpdatePacket);

            // If we are not yet connected we check whether this packet contains a login response,
            // so we can finish connecting
            if (!IsConnected) {
                if (clientUpdatePacket.GetPacketData().TryGetValue(
                        ClientPacketId.LoginResponse,
                        out var packetData)
                   ) {
                    var loginResponse = (LoginResponse) packetData;

                    switch (loginResponse.LoginResponseStatus) {
                        case LoginResponseStatus.Success:
                            OnConnect(loginResponse);
                            break;
                        case LoginResponseStatus.NotWhiteListed:
                            OnConnectFailed(new ConnectFailedResult {
                                Type = ConnectFailedResult.FailType.NotWhiteListed
                            });
                            return;
                        case LoginResponseStatus.Banned:
                            OnConnectFailed(new ConnectFailedResult {
                                Type = ConnectFailedResult.FailType.Banned
                            });
                            return;
                        case LoginResponseStatus.InvalidAddons:
                            OnConnectFailed(new ConnectFailedResult {
                                Type = ConnectFailedResult.FailType.InvalidAddons,
                                AddonData = loginResponse.AddonData
                            });
                            return;
                        case LoginResponseStatus.InvalidUsername:
                            OnConnectFailed(new ConnectFailedResult {
                                Type = ConnectFailedResult.FailType.InvalidUsername
                            });
                            return;
                        default:
                            OnConnectFailed(new ConnectFailedResult {
                                Type = ConnectFailedResult.FailType.Unknown
                            });
                            return;
                    }

                    break;
                }
            }

            _packetManager.HandleClientPacket(clientUpdatePacket);
        }
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
        IsConnecting = true;

        try {
            _dtlsClient.Connect(address, port);
        } catch (SocketException e) {
            Logger.Error($"Failed to connect due to SocketException:\n{e}");

            OnConnectFailed(new ConnectFailedResult {
                Type = ConnectFailedResult.FailType.SocketException
            });
            return;
        } catch (IOException e) {
            Logger.Error($"Failed to connect due to IOException:\n{e}");

            OnConnectFailed(new ConnectFailedResult {
                Type = ConnectFailedResult.FailType.SocketException
            });
            return;
        }

        UpdateManager = new ClientUpdateManager(_dtlsClient.DtlsTransport);
        // During the connection process we register the connection failed callback if we time out
        UpdateManager.OnTimeout += OnConnectTimedOut;
        
        new Thread(() => {
            var cancellationToken = _updateTaskTokenSource.Token;

            while (!cancellationToken.IsCancellationRequested) {
                UpdateManager.ProcessUpdate();

                // TODO: figure out a good way to get rid of the sleep here
                // some way to signal when clients should be updated again would suffice
                // also see NetServer#StartClientUpdates
                Thread.Sleep(5);
            }
        }).Start();
        
        UpdateManager.StartUpdates();

        UpdateManager.SetLoginRequestData(username, authKey, addonData);
        Logger.Debug("Sending login request");
    }

    /// <summary>
    /// Disconnect from the current server.
    /// </summary>
    public void Disconnect() {
        UpdateManager.StopUpdates();
        
        _dtlsClient.Disconnect();

        IsConnected = false;

        // Request cancellation for the update task
        _updateTaskTokenSource?.Cancel();

        // Clear all client addon packet handlers, because their IDs become invalid
        _packetManager.ClearClientAddonPacketHandlers();

        // Invoke callback if it exists
        DisconnectEvent?.Invoke();
    }

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

/// <summary>
/// Class that stores the result of a failed connection.
/// </summary>
internal class ConnectFailedResult {
    /// <summary>
    /// The type of the failed connection.
    /// </summary>
    public FailType Type { get; set; }

    /// <summary>
    /// If the type for failing is having invalid addons, this field contains the addon data that we should have.
    /// </summary>
    public List<AddonData> AddonData { get; set; }

    /// <summary>
    /// Enumeration of fail types.
    /// </summary>
    public enum FailType {
        NotWhiteListed,
        Banned,
        InvalidAddons,
        InvalidUsername,
        TimedOut,
        SocketException,
        Unknown
    }
}
