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

        private event Action OnConnectEvent;
        private event Action OnConnectFailedEvent;
        private event Action OnDisconnectEvent;
        private event Action OnTimeout;

        private string _lastHost;
        private int _lastPort;

        public bool IsConnected { get; private set; }

        public NetClient(PacketManager packetManager) {
            _packetManager = packetManager;

            _udpNetClient = new UdpNetClient();

            // Register the same function for both TCP and UDP receive callbacks
            _udpNetClient.RegisterOnReceive(OnReceiveData);
        }

        public void RegisterOnConnect(Action onConnect) {
            OnConnectEvent += onConnect;
        }

        public void RegisterOnConnectFailed(Action onConnectFailed) {
            OnConnectFailedEvent += onConnectFailed;
        }

        public void RegisterOnDisconnect(Action onDisconnect) {
            OnDisconnectEvent += onDisconnect;
        }

        public void RegisterOnTimeout(Action onTimeout) {
            OnTimeout += onTimeout;
        }

        private void OnConnect() {
            Logger.Get().Info(this, "Connection to server success");
            
            IsConnected = true;
            
            // De-register the connect failed and register the actual timeout handler if we time out
            UpdateManager.OnTimeout -= OnConnectFailed;
            UpdateManager.OnTimeout += OnTimeout;

            // Invoke callback if it exists
            OnConnectEvent?.Invoke();
        }

        private void OnConnectFailed() {
            Logger.Get().Info(this, "Connection to server failed");
            
            UpdateManager?.StopUdpUpdates();

            IsConnected = false;

            // Invoke callback if it exists
            OnConnectFailedEvent?.Invoke();
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
                        out var packetData)) {

                        var loginResponse = (LoginResponse) packetData;

                        Logger.Get().Info(this,
                            $"Received login response, status: {loginResponse.LoginResponseStatus}");
                        switch (loginResponse.LoginResponseStatus) {
                            case LoginResponseStatus.Success:
                                OnConnect();
                                break;
                        }
                    }
                }

                _packetManager.HandleClientPacket(clientUpdatePacket);
            }
        }

        /**
         * Starts establishing a connection with the given host on the given port
         */
        public void Connect(string host, int port, string username) {
            _lastHost = host;
            _lastPort = port;

            try {
                _udpNetClient.Connect(_lastHost, _lastPort);
            } catch (SocketException e) {
                Logger.Get().Warn(this, $"Failed to connect due to SocketException, message: {e.Message}");
                
                OnConnectFailed();
                return;
            }

            UpdateManager = new ClientUpdateManager(_udpNetClient);
            UpdateManager.StartUdpUpdates();
            // During the connection process we register the connection failed callback if we time out
            UpdateManager.OnTimeout += OnConnectFailed;
            
            UpdateManager.SetLoginRequestData(username);
        }

        public void Disconnect() {
            UpdateManager.StopUdpUpdates();

            _udpNetClient.Disconnect();

            IsConnected = false;

            // Invoke callback if it exists
            OnDisconnectEvent?.Invoke();
        }
    }
}