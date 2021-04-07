using System;
using System.Reflection;
using System.IO;
using System.Net;
using GlobalEnums;
using Modding;
using System.Collections.Generic;
using HKMP.Networking.Packet;
using HKMP.ServerKnights;
using UnityEngine;

namespace HKMP.Networking.Client {
    public delegate void OnReceive(List<Packet.Packet> receivedPackets);
    
    /**
     * The networking client that manages both a TCP and UDP client for sending and receiving data.
     * This only manages client side networking, e.g. sending to and receiving from the server.
     */
    public class NetClient {
        private readonly PacketManager _packetManager;
        private readonly SkinManager _skinManager;
        private readonly TcpNetClient _tcpNetClient;
        private readonly UdpNetClient _udpNetClient;

        public ClientUpdateManager UpdateManager { get; private set; }

        private event Action OnConnectEvent;
        private event Action OnConnectFailedEvent;
        private event Action OnDisconnectEvent;
        private event Action OnHeartBeat;

        public string _lastHost;
        public int _lastPort;

        public bool IsConnected { get; private set; }

        public NetClient(PacketManager packetManager,SkinManager skinManager) {
            _packetManager = packetManager;
            _skinManager = skinManager;

            _tcpNetClient = new TcpNetClient();
            _udpNetClient = new UdpNetClient();
            
            _tcpNetClient.RegisterOnConnect(OnConnect);
            _tcpNetClient.RegisterOnConnectFailed(OnConnectFailed);
            
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
            // Only when the TCP connection is successful, we connect the UDP
            _udpNetClient.Connect(_lastHost, _lastPort, _tcpNetClient.GetConnectedPort());

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
                
            _tcpNetClient.Connect(host, port);
        }

        public void Disconnect() {
            UpdateManager.StopUdpUpdates();
        
            _tcpNetClient.Disconnect();
            _udpNetClient.Disconnect();
            
            IsConnected = false;
            
            // Invoke callback if it exists
            OnDisconnectEvent?.Invoke();
        }

    }
}