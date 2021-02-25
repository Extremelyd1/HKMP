using System;
using System.Net;
using System.Net.Sockets;

namespace HKMP.Networking.Client {
    
    /**
     * NetClient that uses the UDP protocol
     */
    public class UdpNetClient {
        private UdpClient _udpClient;
        private IPEndPoint _endPoint;
        
        private OnReceive _onReceive;
        
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
                _onReceive?.Invoke(receivedData);
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
            // We are using UDP so we need to identify ourselves
            // TODO: insert client ID in packet before sending

            if (!_udpClient.Client.Connected) {
                Logger.Error(this, "Tried sending packet, but UDP was not connected");
                return;
            }
            
            // Make sure that the packet contains its length at the front before sending
            packet.WriteLength();

            _udpClient.BeginSend(packet.ToArray(), packet.Length(), null, null);
        }
    }
}