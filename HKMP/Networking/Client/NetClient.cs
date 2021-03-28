using System;
using System.Collections.Generic;
using HKMP.Animation;
using HKMP.Game.Client.Entity;
using HKMP.Networking.Packet;
using UnityEngine;

namespace HKMP.Networking.Client {
    public delegate void OnReceive(List<Packet.Packet> receivedPackets);
    
    /**
     * The networking client that manages both a TCP and UDP client for sending and receiving data.
     * This only manages client side networking, e.g. sending to and receiving from the server.
     */
    public class NetClient {
        private readonly PacketManager _packetManager;
        
        private readonly TcpNetClient _tcpNetClient;
        private readonly UdpNetClient _udpNetClient;

        private readonly UdpUpdateManager _udpUpdateManager;

        private event Action OnConnectEvent;
        private event Action OnConnectFailedEvent;
        private event Action OnDisconnectEvent;

        private string _lastHost;
        private int _lastPort;

        public bool IsConnected { get; private set; }
        
        public NetClient(PacketManager packetManager) {
            _packetManager = packetManager;
            
            _tcpNetClient = new TcpNetClient();
            _udpNetClient = new UdpNetClient();
            
            _tcpNetClient.RegisterOnConnect(OnConnect);
            _tcpNetClient.RegisterOnConnectFailed(OnConnectFailed);
            
            // Register the same function for both TCP and UDP receive callbacks
            _tcpNetClient.RegisterOnReceive(OnReceiveData);
            _udpNetClient.RegisterOnReceive(OnReceiveData);

            _udpUpdateManager = new UdpUpdateManager(_udpNetClient);
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

        private void OnConnect() {
            // Only when the TCP connection is successful, we connect the UDP
            _udpNetClient.Connect(_lastHost, _lastPort, _tcpNetClient.GetConnectedPort());
            
            IsConnected = true;

            _udpUpdateManager.StartUdpUpdates();
            
            // Invoke callback if it exists
            OnConnectEvent?.Invoke();
        }

        private void OnConnectFailed() {
            IsConnected = false;
            
            // Invoke callback if it exists
            OnConnectFailedEvent?.Invoke();
        }

        private void OnReceiveData(List<Packet.Packet> packets) {
            _udpUpdateManager.OnReceivePackets(packets);
            
            _packetManager.HandleClientPackets(packets);
        }

        /**
         * Starts establishing a connection with the given host on the given port
         */
        public void Connect(string host, int port) {
            _lastHost = host;
            _lastPort = port;
                
            _tcpNetClient.Connect(host, port);
        }

        public void SendTcp(Packet.Packet packet) {
            _tcpNetClient.Send(packet);
        }

        public void SendPositionUpdate(Vector3 position) {
            _udpUpdateManager.UpdatePlayerPosition(position);
        }

        public void SendScaleUpdate(Vector3 scale) {
            _udpUpdateManager.UpdatePlayerScale(scale);
        }

        public void SendMapUpdate(Vector3 mapPosition) {
            _udpUpdateManager.UpdatePlayerMapPosition(mapPosition);
        }

        public void SendAnimationUpdate(AnimationClip clipId, int frame = 0, bool[] effectInfo = null) {
            _udpUpdateManager.UpdatePlayerAnimation((ushort) clipId, (byte) frame, effectInfo);
        }

        public void SendEntityPositionUpdate(EntityType entityType, byte entityId, Vector3 position) {
            _udpUpdateManager.UpdateEntityPosition(entityType, entityId, position);
        }

        public void SendEntityStateUpdate(EntityType entityType, byte entityId, byte stateIndex) {
            _udpUpdateManager.UpdateEntityState(entityType, entityId, stateIndex);
        }

        public void SendEntityVariableUpdate(EntityType entityType, byte entityId, List<byte> fsmVariables) {
            _udpUpdateManager.UpdateEntityVariables(entityType, entityId, fsmVariables);
        }

        public void Disconnect() {
            _udpUpdateManager.StopUdpUpdates();
        
            _tcpNetClient.Disconnect();
            _udpNetClient.Disconnect();
            
            IsConnected = false;
            
            // Invoke callback if it exists
            OnDisconnectEvent?.Invoke();
        }

    }
}