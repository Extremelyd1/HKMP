using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using HKMP.Networking.Packet;

namespace HKMP.Networking.Client {
    
    /**
     * NetClient that uses the UDP protocol
     */
    public class UdpNetClient {
        private readonly object _lock = new object();
        
        private UdpClient _udpClient;
        private IPEndPoint _endPoint;
        
        private OnReceive _onReceive;
        
        private byte[] _leftoverData;

        public void RegisterOnReceive(OnReceive onReceive) {
            _onReceive = onReceive;
        }
        
        public void Connect(string host, int port, int localPort) {
            _endPoint = new IPEndPoint(IPAddress.Any, localPort);

            _udpClient = new UdpClient(localPort);
            _udpClient.Connect(host, port);
            _udpClient.BeginReceive(OnReceive, null);

            Logger.Info(this, $"Starting receiving UDP data on endpoint {_endPoint}");
        }

        private void OnReceive(IAsyncResult result) {
            byte[] receivedData = {};
            
            try {
                receivedData = _udpClient.EndReceive(result, ref _endPoint);
            } catch (Exception e) {
                Logger.Warn(this, $"UDP Receive exception: {e.Message}");
            }

            // Immediately start listening for new data
            // Only do this when the client exists, we might have closed the client
            _udpClient?.BeginReceive(OnReceive, null);
            
            // If we did not receive at least an int of bytes, something went wrong
            if (receivedData.Length < 4) {
                Logger.Error(this, $"Received incorrect data length: {receivedData.Length}");
                
                return;
            }
            
            List<Packet.Packet> packets;

            // Lock the leftover data array for synchronous data handling
            // This makes sure that from another asynchronous receive callback we don't
            // read/write to it in different places
            lock (_lock) {
                packets = PacketManager.HandleReceivedData(receivedData, ref _leftoverData);
            }

            _onReceive?.Invoke(packets);
        }

        /**
         * Disconnect the UDP client and clean it up
         */
        public void Disconnect() {
            if (!_udpClient.Client.Connected) {
                Logger.Warn(this, "UDP client was not connected, cannot disconnect");
                return;
            }
            
            _udpClient.Close();
            _udpClient = null;

            // _sendStopwatch.Reset();
            //
            // _belowThresholdStopwatch.Reset();
        }

        public void Send(Packet.Packet packet) {
            if (_udpClient?.Client == null) {
                return;
            }
        
            if (!_udpClient.Client.Connected) {
                Logger.Error(this, "Tried sending packet, but UDP was not connected");
                return;
            }
            
            // Send the packet
            _udpClient.BeginSend(packet.ToArray(), packet.Length(), null, null);
        }
    }
}