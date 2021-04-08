using System;
using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.IO;
using GlobalEnums;
using Modding;
using UnityEngine;
using HKMP.Networking.Packet;
using HKMP.ServerKnights;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace HKMP.Networking.Server {
    /**
     * Server that manages connection with clients 
     */
    public class NetServer {
        private readonly object _lock = new object();
        
        private readonly PacketManager _packetManager;
        private readonly ServerKnightsManager _serverKnightsManager;
        private readonly Dictionary<ushort, NetServerClient> _clients;

        private TcpListener _tcpListener;
        private UdpClient _udpClient;

        private HttpListener httpListener;

        private byte[] _leftoverData;

        private event Action<ushort> OnHeartBeat;
        private event Action OnShutdownEvent;

        public bool IsStarted { get; private set; }
        public NetServer(PacketManager packetManager ,ServerKnightsManager serverKnightsManager) {
            _packetManager = packetManager;
            _serverKnightsManager = serverKnightsManager;

            _clients = new Dictionary<ushort, NetServerClient>();
        }

        public void RegisterOnClientHeartBeat(Action<ushort> onHeartBeat) {
            OnHeartBeat += onHeartBeat;
        }

        public void RegisterOnShutdown(Action onShutdown) {
            OnShutdownEvent += onShutdown;
        }

        public void HandleHTTPConnections(IAsyncResult result)
        {
            HttpListener listener = (HttpListener) result.AsyncState;
            httpListener.BeginGetContext(new AsyncCallback(HandleHTTPConnections),httpListener);
            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            //todo figure out a better api here.
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(_serverKnightsManager.skinManager.getServerJson());
            response.ContentType = "text/plain";
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer,0,buffer.Length);
            output.Close();
        }

        /**
         * Starts the server on the given port
         */
        public void Start(int port) {
            Logger.Info(this, $"Starting NetServer on port {port}");
            Logger.Info(this,$"Starting Skin Server on {port+1}");
            var url = $"http://*:{port+1}/";
            IsStarted = true;

            // Initialize TCP listener and UDP client
            _tcpListener = new TcpListener(IPAddress.Any, port);
            _udpClient = new UdpClient(port);
            httpListener = new HttpListener();
            httpListener.Prefixes.Add(url);
            httpListener.Start();

            // Start and begin receiving data on both protocols
            _tcpListener.Start();
            _tcpListener.BeginAcceptTcpClient(OnTcpConnection, null);
            _udpClient.BeginReceive(OnUdpReceive, null);
            httpListener.BeginGetContext(new AsyncCallback(HandleHTTPConnections),httpListener);
            _serverKnightsManager.skinManager.getServerJson(); // preload it

        }

        /**
         * Callback for when a TCP connection is accepted
         */
        private void OnTcpConnection(IAsyncResult result) {
            // Retrieve the TCP client from the incoming connection
            var tcpClient = _tcpListener.EndAcceptTcpClient(result);

            // Check whether  there already exists a client with the given IP and store its ID
            ushort id = 0;
            var idFound = false;
            foreach (var clientPair in _clients) {
                var netServerClient = clientPair.Value;

                if (netServerClient.HasAddress((IPEndPoint) tcpClient.Client.RemoteEndPoint)) {
                    Logger.Info(this, "A client with the same IP and port already exists, overwriting NetServerClient");

                    // Since it already exists, we now have to disconnect the old one
                    netServerClient.Disconnect();

                    id = clientPair.Key;
                    idFound = true;
                    break;
                }
            }

            // Create new NetServerClient instance
            // If we found an existing ID for the incoming IP-port combination, we use that existing ID and overwrite the old one
            NetServerClient newClient;
            if (idFound) {
                newClient = new NetServerClient(id, tcpClient, _udpClient);
            } else {
                newClient = new NetServerClient(tcpClient, _udpClient);
            }

            newClient.UpdateManager.StartUdpUpdates();
            _clients[newClient.GetId()] = newClient;

            Logger.Info(this,
                $"Accepted TCP connection from {tcpClient.Client.RemoteEndPoint}, assigned ID {newClient.GetId()}");

            // Start listening for new clients again
            _tcpListener.BeginAcceptTcpClient(OnTcpConnection, null);
        }

        /**
         * Callback for when UDP traffic is received
         */
        private void OnUdpReceive(IAsyncResult result) {
            // Initialize default IPEndPoint for reference in data receive method
            var endPoint = new IPEndPoint(IPAddress.Any, 0);

            byte[] receivedData = { };
            try {
                receivedData = _udpClient.EndReceive(result, ref endPoint);
            } catch (Exception e) {
                Logger.Warn(this, $"UDP Receive exception: {e.Message}");
            }
            
            // Immediately start receiving data again
            _udpClient.BeginReceive(OnUdpReceive, null);

            // Figure out which client ID this data is from
            ushort id = 0;
            var idFound = false;
            foreach (var client in _clients.Values) {
                if (client.HasAddress(endPoint)) {
                    id = client.GetId();
                    idFound = true;
                    break;
                }
            }

            if (!idFound) {
                Logger.Warn(this,
                    $"Received UDP data from {endPoint.Address}, but there was no matching known client");

                return;
            }
            
            List<Packet.Packet> packets;

            // Lock the leftover data array for synchronous data handling
            // This makes sure that from another asynchronous receive callback we don't
            // read/write to it in different places
            lock (_lock) {
                packets = PacketManager.HandleReceivedData(receivedData, ref _leftoverData);
            }

            // We received packets from this client, which means they are still alive
            OnHeartBeat?.Invoke(id);

            foreach (var packet in packets) {
                // Create a server update packet from the raw packet instance
                var serverUpdatePacket = new ServerUpdatePacket(packet);
                serverUpdatePacket.ReadPacket();

                _clients[id].UpdateManager.OnReceivePacket(serverUpdatePacket);

                // Let the packet manager handle the received data
                _packetManager.HandleServerPacket(id, serverUpdatePacket);
            }
        }

        /**
         * Stops the server
         */
        public void Stop() {
            // Clean up existing clients
            foreach (var idClientPair in _clients) {
                idClientPair.Value.Disconnect();
            }

            _clients.Clear();

            _tcpListener.Stop();
            _udpClient.Close();
            httpListener.Close();

            _tcpListener = null;
            _udpClient = null;
            _leftoverData = null;
            httpListener = null;

            IsStarted = false;

            // Invoke the shutdown event to notify all registered parties of the shutdown
            OnShutdownEvent?.Invoke();
        }

        public void OnClientDisconnect(ushort id) {
            if (!_clients.ContainsKey(id)) {
                Logger.Warn(this, $"Disconnect packet received from ID {id}, but client is not in client list");
                return;
            }

            _clients[id].Disconnect();
            _clients.Remove(id);

            Logger.Info(this, $"Client {id} disconnected");
        }

        public ServerUpdateManager GetUpdateManagerForClient(ushort id) {
            if (!_clients.TryGetValue(id, out var netServerClient)) {
                return null;
            }

            return netServerClient.UpdateManager;
        }

        public void SetDataForAllClients(Action<ServerUpdateManager> dataAction) {
            foreach (var netServerClient in _clients.Values) {
                dataAction(netServerClient.UpdateManager);
            }
        }
    }
}