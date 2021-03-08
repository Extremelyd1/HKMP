using System;
using System.Net;
using System.Net.Sockets;
using HKMP.Networking.Packet;

namespace HKMP.Networking.Client {
    
    /**
     * NetClient that uses the UDP protocol
     */
    public class UdpNetClient {
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
            // Initialize default IPEndPoint for reference in data receive method
            var receivedData = _udpClient.EndReceive(result, ref _endPoint);
            // If we did not receive at least an int of bytes, something went wrong
            if (receivedData.Length < 4) {
                Logger.Error(this, $"Received incorrect data length: {receivedData.Length}");
            } else {
                var currentData = receivedData;
                
                // TODO: this code is used in 3 places at the moment, perhaps refactor to a different place
                // TODO: maybe we need to make sure that we always read from the UDP stream, and process
                // the packets either in a different thread or something that doesn't block the reading
                // The same holds for TCP
                
                // Check whether we have leftover data from the previous read, and concatenate the two byte arrays
                if (_leftoverData != null && _leftoverData.Length > 0) {
                    currentData = new byte[_leftoverData.Length + receivedData.Length];

                    // Copy over the leftover data into the current data array
                    for (var i = 0; i < _leftoverData.Length; i++) {
                        currentData[i] = _leftoverData[i];
                    }
                        
                    // Copy over the trimmed data into the current data array
                    for (var i = 0; i < receivedData.Length; i++) {
                        currentData[_leftoverData.Length + i] = receivedData[i];
                    }

                    _leftoverData = null;
                }

                // Create packets from the data
                var packets = PacketManager.ByteArrayToPackets(currentData, ref _leftoverData);
                
                _onReceive?.Invoke(packets);
            }
            
            // After the callback is invoked, start listening for new data
            // Only do this when the client exists, we might have closed the client
            _udpClient?.BeginReceive(OnReceive, null);
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
        }

        public void Send(Packet.Packet packet) {
            if (!_udpClient.Client.Connected) {
                Logger.Error(this, "Tried sending packet, but UDP was not connected");
                return;
            }

            _udpClient.BeginSend(packet.ToArray(), packet.Length(), null, null);
        }
    }
}