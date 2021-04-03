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
using HKMP.Networking.Packet.Custom;
using HKMP.Networking.Packet.Custom.Update;
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
        private readonly SkinManager _skinManager;

        private readonly Dictionary<ushort, NetServerClient> _clients;
        
        private readonly Dictionary<ushort, Queue<ushort>> _toAckSequenceNumbers;

        private TcpListener _tcpListener;
        private UdpClient _udpClient;

        private HttpListener httpListener;

        private byte[] _leftoverData;

        private event Action OnShutdownEvent;

        public bool IsStarted { get; private set; }
        public NetServer(PacketManager packetManager ,SkinManager skinManager) {
            _packetManager = packetManager;
            _skinManager = skinManager;

            _clients = new Dictionary<ushort, NetServerClient>();

            _toAckSequenceNumbers = new Dictionary<ushort, Queue<ushort>>();
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
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(_skinManager.getServerJson());
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
            _skinManager.getServerJson(); // preload it 
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
                    Logger.Info(this, "A client with the same IP already exists, overwriting NetServerClient");

                    // Since it already exists, we now have to disconnect the old one
                    netServerClient.Disconnect();

                    id = clientPair.Key;
                    idFound = true;
                    break;
                }
            }

            // Create client and register TCP receive callback
            // If we found an existing ID for the incoming IP, we use that existing ID and overwrite the old one
            NetServerClient newClient;
            if (idFound) {
                newClient = new NetServerClient(id, tcpClient);
            } else {
                newClient = new NetServerClient(tcpClient);
            }

            newClient.RegisterOnTcpReceive(OnTcpReceive);
            _clients[newClient.GetId()] = newClient;

            Logger.Info(this,
                $"Accepted TCP connection from {tcpClient.Client.RemoteEndPoint}, assigned ID {newClient.GetId()}");

            // Start listening for new clients again
            _tcpListener.BeginAcceptTcpClient(OnTcpConnection, null);
        }

        /**
         * Callback for when TCP traffic is received
         */
        private void OnTcpReceive(ushort id, List<Packet.Packet> packets) {
            _packetManager.HandleServerPackets(id, packets);
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

            foreach (var packet in packets) {
                // Read packet ID without advancing read position
                var packetId = packet.ReadPacketId(false);
            
                // If this is an player update packet it contains a sequence number which
                // we need to acknowledge at some point
                if (packetId.Equals(PacketId.PlayerUpdate)) {
                    // Read the sequence number and enqueue it so we can acknowledge it later
                    var sequenceNumber = packet.ReadSequenceNumber();
                    Queue<ushort> ackQueue;
                    if (!_toAckSequenceNumbers.ContainsKey(id)) {
                        ackQueue = new Queue<ushort>();
                        _toAckSequenceNumbers.Add(id, ackQueue);
                    } else {
                        ackQueue = _toAckSequenceNumbers[id];
                    }
                    
                    ackQueue.Enqueue(sequenceNumber);
                }
            }

            // Let the packet manager handle the received data
            _packetManager.HandleServerPackets(id, packets);
        }

        /**
         * Sends a packet to the client with the given ID over TCP
         */
        public void SendTcp(ushort id, Packet.Packet packet) {
            if (!_clients.ContainsKey(id)) {
                Logger.Info(this, $"Could not find ID {id} in clients, could not send TCP packet");
                return;
            }

            // Make sure that we use a clean packet object every time
            var newPacket = new Packet.Packet(packet.ToArray());
            // Send the newly constructed packet to the client
            _clients[id].SendTcp(newPacket);
        }

        /**
         * Sends a packet to the client with the given ID over UDP
         */
        private void SendUdp(ushort id, Packet.Packet packet) {
            if (!_clients.ContainsKey(id)) {
                Logger.Info(this, $"Could not find ID {id} in clients, could not send UDP packet");
                return;
            }

            // Make sure that we use a clean packet object every time
            var newPacket = new Packet.Packet(packet.ToArray());
            // Send the newly constructed packet to the client
            _clients[id].SendUdp(_udpClient, newPacket);
        }

        public void SendPlayerUpdate(ushort id, ClientUpdatePacket packet) {
            ushort ackSequenceNumber;

            Queue<ushort> ackQueue;
            if (!_toAckSequenceNumbers.ContainsKey(id)) {
                ackQueue = new Queue<ushort>();
                _toAckSequenceNumbers.Add(id, ackQueue);
            } else {
                ackQueue = _toAckSequenceNumbers[id];
            }
            
            if (ackQueue.Count == 0) {
                // The queue is somehow empty, this shouldn't happen,
                // but we can still send the update packet
                Logger.Warn(this, "No more client packets to acknowledge, our queue is empty!");
                
                ackSequenceNumber = 0;
            } else {
                // Retrieve a sequence number that we need to acknowledge and
                // add it to the packet
                ackSequenceNumber = ackQueue.Dequeue();
            }

            packet.SequenceNumber = ackSequenceNumber;

            // Create the packet and send it
            SendUdp(id, packet.CreatePacket());
        }

        /**
         * Sends a packet to all connected clients over TCP
         */
        public void BroadcastTcp(Packet.Packet packet) {
            foreach (var idClientPair in _clients) {
                // Make sure that we use a clean packet object every time
                var newPacket = new Packet.Packet(packet.ToArray());
                // Send the newly constructed packet to the client
                idClientPair.Value.SendTcp(newPacket);
            }
        }

        /**
         * Sends a packet to all connected clients over UDP
         */
        private void BroadcastUdp(Packet.Packet packet) {
            foreach (var client in _clients.Values) {
                // Make sure that we use a clean packet object every time
                var newPacket = new Packet.Packet(packet.ToArray());
                // Send the newly constructed packet to the client
                client.SendUdp(_udpClient, newPacket);
            }
        }

        /**
         * Stops the server
         */
        public void Stop() {
            _tcpListener.Stop();
            _udpClient.Close();
            httpListener.Close();

            _tcpListener = null;
            _udpClient = null;
            _leftoverData = null;
            httpListener = null;

            // Clean up existing clients
            foreach (var idClientPair in _clients) {
                idClientPair.Value.Disconnect();
            }

            _clients.Clear();

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
    }
}