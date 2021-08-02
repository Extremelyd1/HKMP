using System;
using System.Collections.Generic;
using Hkmp.Networking.Packet;

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
        private event Action OnHeartBeat;

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

        public void RegisterOnHeartBeat(Action onHeartBeat) {
            OnHeartBeat += onHeartBeat;
        }

        private void OnConnect() {
            UpdateManager = new ClientUpdateManager(_udpNetClient);
            UpdateManager.StartUdpUpdates();

            IsConnected = true;

            // Invoke callback if it exists
            OnConnectEvent?.Invoke();
        }

        private void OnConnectFailed() {
            IsConnected = false;

            // Invoke callback if it exists
            OnConnectFailedEvent?.Invoke();
        }

        private void OnReceiveData(List<Packet.Packet> packets) {
            // We received packets from the server, which means the server is still alive
            OnHeartBeat?.Invoke();

            foreach (var packet in packets) {
                // Create a ClientUpdatePacket from the raw packet instance,
                // and read the values into it
                var clientUpdatePacket = new ClientUpdatePacket(packet);
                clientUpdatePacket.ReadPacket();

                UpdateManager.OnReceivePacket(clientUpdatePacket);

                _packetManager.HandleClientPacket(clientUpdatePacket);
            }
        }

        /**
         * Starts establishing a connection with the given host on the given port
         */
        public void Connect(string host, int port) {
            _lastHost = host;
            _lastPort = port;
            
            _udpNetClient.Connect(_lastHost, _lastPort);
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