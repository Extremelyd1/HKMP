using System;
using System.Net;
using System.Net.Sockets;

namespace HKMP.Networking.Client {
    public class UdpNetClient : NetClient {

        private UdpClient _udpClient;
        private IPEndPoint _endPoint;
        
        public override void Connect(string host, int port) {
            _endPoint = new IPEndPoint(IPAddress.Any, port);

            _udpClient = new UdpClient(port);
            _udpClient.Connect(host, port);
            _udpClient.BeginReceive(OnReceive, null);
        }

        private void OnReceive(IAsyncResult result) {
            var receivedData = _udpClient.EndReceive(result, ref _endPoint);
            // If we did not receive at least an int of bytes, something went wrong
            if (receivedData.Length < 4) {
                Logger.Error(this, $"Received incorrect data length: {receivedData.Length}");
            } else {
                onReceive?.Invoke(receivedData);
            }
            
            // After the callback is invoked, start listening for new data
            // Only do this when the client exists, we might have closed the client
            _udpClient?.BeginReceive(OnReceive, null);
        }

        /**
         * Disconnect the UDP client and clean it up
         */
        public override void Disconnect() {
            if (!_udpClient.Client.Connected) {
                Logger.Warn(this, "UDP client was not connected, cannot disconnect");
                return;
            }
            
            _udpClient.Close();
            _udpClient = null;
        }

        public override void Send(Packet.Packet packet) {
            // We are using UDP so we need to identify ourselves
            // TODO: insert client ID in packet before sending

            if (!_udpClient.Client.Connected) {
                Logger.Error(this, $"Tried sending packet, but UDP was not connected");
                return;
            }

            _udpClient.BeginSend(packet.ToArray(), packet.Length(), null, null);
        }
    }
}