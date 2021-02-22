using System;
using System.Net.Sockets;

namespace HKMP.Networking.Client {
    /**
     * TCP implementation of the abstract NetClient 
     */
    public class TcpNetClient : NetClient {

        private TcpClient _tcpClient;
        private NetworkStream _stream;

        private byte[] _receivedData;

        /**
         * Connects to the given host with given port
         */
        public override void Connect(string host, int port) {
            _tcpClient = new TcpClient {
                ReceiveBufferSize = MaxBufferSize,
                SendBufferSize = MaxBufferSize,
            };

            _tcpClient.BeginConnect(host, port, OnConnect, _tcpClient);
        }

        /**
         * Callback for when the connection is finished.
         * Sets up TCP stream for sending and receiving data
         */
        private void OnConnect(IAsyncResult result) {
            _tcpClient.EndConnect(result);

            if (!_tcpClient.Connected) {
                return;
            }

            _stream = _tcpClient.GetStream();

            _receivedData = new byte[MaxBufferSize];

            _stream.BeginRead(_receivedData, 0, MaxBufferSize, OnReceive, null);
        }

        /**
         * Callback for when data is received over the TCP stream
         */
        private void OnReceive(IAsyncResult result) {
            var dataLength = _stream.EndRead(result);
            if (dataLength <= 0) {
                Logger.Error(this, $"Received incorrect data length: {dataLength}");
            } else {
                // If callback exists, execute it
                onReceive?.Invoke(_receivedData);
            }

            // After the callback is invoked, create new byte array
            // and start listening/reading for new data
            _receivedData = new byte[MaxBufferSize];
            _stream.BeginRead(_receivedData, 0, MaxBufferSize, OnReceive, null);
        }

        /**
         * Disconnects the TCP client from the open connection
         */
        public override void Disconnect() {
            if (!_tcpClient.Connected) {
                Logger.Warn(this, "TCP client was not connected, cannot disconnect");                
                return;
            }
            
            _tcpClient.Close();
        }

        /**
         * Sends the given Packet over the current TCP stream
         */
        public override void Send(Packet.Packet packet) {
            if (_tcpClient != null) {
                _stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
            }
        }
    }
}