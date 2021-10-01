using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Client {
    public delegate void OnReceive(List<Packet.Packet> receivedPackets);

    /**
     * The networking client that manages the UDP client for sending and receiving data.
     * This only manages client side networking, e.g. sending to and receiving from the server.
     */
    public class NetClient {
        private readonly PacketManager _packetManager;
        private readonly UdpNetClient _udpNetClient;

        public ClientUpdateManager UpdateManager { get; private set; }

        public event Action<LoginResponse> ConnectEvent;
        public event Action<ConnectFailedResult> ConnectFailedEvent;
        public event Action DisconnectEvent;
        public event Action TimeoutEvent;

        private string _lastHost;
        private int _lastPort;

        public bool IsConnected { get; private set; }

        public NetClient(PacketManager packetManager) {
            _packetManager = packetManager;

            _udpNetClient = new UdpNetClient();

            // Register the same function for both TCP and UDP receive callbacks
            _udpNetClient.RegisterOnReceive(OnReceiveData);
        }

        private void OnConnect(LoginResponse loginResponse) {
            Logger.Get().Info(this, "Connection to server success");
            
            IsConnected = true;
            
            // De-register the connect failed and register the actual timeout handler if we time out
            UpdateManager.OnTimeout -= OnConnectTimedOut;
            UpdateManager.OnTimeout += TimeoutEvent;

            // Invoke callback if it exists
            ConnectEvent?.Invoke(loginResponse);
        }

        private void OnConnectTimedOut() => OnConnectFailed(new ConnectFailedResult {
            Type = ConnectFailedResult.FailType.TimedOut
        });

        private void OnConnectFailed(ConnectFailedResult result) {
            Logger.Get().Info(this, $"Connection to server failed, cause: {result}");
            
            UpdateManager?.StopUdpUpdates();

            IsConnected = false;

            // Invoke callback if it exists
            ConnectFailedEvent?.Invoke(result);
        }

        private void OnReceiveData(List<Packet.Packet> packets) {
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
                            case LoginResponseStatus.InvalidAddons:
                                OnConnectFailed(new ConnectFailedResult {
                                    Type = ConnectFailedResult.FailType.InvalidAddons,
                                    AddonData = loginResponse.AddonData
                                });
                                return;
                        }

                        break;
                    }
                }

                _packetManager.HandleClientPacket(clientUpdatePacket);
            }
        }

        /**
         * Starts establishing a connection with the given host on the given port
         */
        public void Connect(string host, int port, string username, List<AddonData> addonData) {
            _lastHost = host;
            _lastPort = port;

            try {
                _udpNetClient.Connect(_lastHost, _lastPort);
            } catch (SocketException e) {
                Logger.Get().Warn(this, $"Failed to connect due to SocketException, message: {e.Message}");
                
                OnConnectFailed(new ConnectFailedResult {
                    Type = ConnectFailedResult.FailType.SocketException
                });
                return;
            }

            UpdateManager = new ClientUpdateManager(_udpNetClient);
            UpdateManager.StartUdpUpdates();
            // During the connection process we register the connection failed callback if we time out
            UpdateManager.OnTimeout += OnConnectTimedOut;
            
            UpdateManager.SetLoginRequestData(username, addonData);
        }

        public void Disconnect() {
            UpdateManager.StopUdpUpdates();

            _udpNetClient.Disconnect();

            IsConnected = false;

            // Invoke callback if it exists
            DisconnectEvent?.Invoke();
        }
    }

    /**
     * Class that stores the result of a failed connection.
     */
    public class ConnectFailedResult {
        public FailType Type { get; set; }
        // If the type for failing is having invalid addons, this field contains the addon data
        public List<AddonData> AddonData { get; set; }

        public enum FailType {
            InvalidAddons,
            TimedOut,
            SocketException
        }
    }
}