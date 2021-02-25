using System;
using System.Net.Sockets;
using HKMP.Networking.Client;
using UnityEngine;

namespace HKMP.Networking {
    /**
     * NetClient that uses the TCP protocol
     */
    public class TcpNetClient {
        private static readonly int MaxBufferSize = (int) Mathf.Pow(2, 20);

        private TcpClient _tcpClient;
        private NetworkStream _stream;

        private byte[] _receivedData;

        private Action _onConnect;
        private Action _onConnectFailed;
        private OnReceive _onReceive;

        public void RegisterOnConnect(Action onConnect) {
            _onConnect = onConnect;
        }

        public void RegisterOnConnectFailed(Action onConnectFailed) {
            _onConnectFailed = onConnectFailed;
        }
        
        public void RegisterOnReceive(OnReceive onReceive) {
            _onReceive = onReceive;
        }

        /**
         * Connects to the given host with given port
         */
        public void Connect(string host, int port) {
            _tcpClient = new TcpClient {
                ReceiveBufferSize = MaxBufferSize,
                SendBufferSize = MaxBufferSize,
            };

            _tcpClient.BeginConnect(host, port, OnConnect, _tcpClient);
            Logger.Info(this, "TCP Begin Connect");
        }

        /**
         * Initialize this client with an existing TcpClient instance.
         * Used instead of connection with host and port if the TcpClient was already established.
         */
        public void InitializeWithClient(TcpClient tcpClient) {
            _tcpClient = tcpClient;

            // Finish connection setup
            FinishConnectionSetup();
        }

        /**
         * Callback for when the connection is finished.
         * Sets up TCP stream for sending and receiving data
         */
        private void OnConnect(IAsyncResult result) {
            if (result != null) {
                try {
                    _tcpClient.EndConnect(result);
                } catch (Exception e) {
                    Logger.Info(this, $"Connection failed: {e.Message}");
                    // Invoke callback if it exists
                    _onConnectFailed?.Invoke();

                    return;
                }
            } else {
                Logger.Warn(this, "Result in OnConnect is null");
                // This probably means that the connection failed, so invoke the callback
                _onConnectFailed?.Invoke();
                return;
            }

            FinishConnectionSetup();
        }

        private void FinishConnectionSetup() {
            if (!_tcpClient.Connected) {
                Logger.Error(this, "Connection failed in FinishConnectionSetup, this shouldn't happend");
                return;
            }

            _stream = _tcpClient.GetStream();
            
            _receivedData = new byte[MaxBufferSize];

            _stream.BeginRead(_receivedData, 0, MaxBufferSize, OnReceive, null);
           
            // Invoke callback if it exists
            _onConnect?.Invoke();
        }

        /**
         * Callback for when data is received over the TCP stream
         */
        private void OnReceive(IAsyncResult result) {
            var dataLength = _stream.EndRead(result);
            if (dataLength <= 0) {
                // TODO: investigate why this happens, for now the message is removed
                //Logger.Error(this, $"Received incorrect data length: {dataLength}");
            } else {
                // Create new byte array with exact length of received data
                var trimmedData = new byte[dataLength];
                // Copy over the data to new array
                Array.Copy(_receivedData, trimmedData, dataLength);
                
                // If callback exists, execute it
                _onReceive?.Invoke(trimmedData);
            }

            // After the callback is invoked, create new byte array
            // and start listening/reading for new data
            _receivedData = new byte[MaxBufferSize];
            _stream.BeginRead(_receivedData, 0, MaxBufferSize, OnReceive, null);
        }

        /**
         * Disconnects the TCP client from the open connection
         */
        public void Disconnect() {
            if (!_tcpClient.Connected) {
                Logger.Warn(this, "TCP client was not connected, cannot disconnect");                
                return;
            }
            
            _tcpClient.Close();
        }

        /**
         * Sends the given Packet over the current TCP stream
         */
        public void Send(Packet.Packet packet) {
            if (!_tcpClient.Connected) {
                Logger.Warn(this, "Tried calling send while TCP client is not connected");
                return;
            }

            if (_stream == null) {
                Logger.Warn(this, "TCP stream is null, cannot send");
                return;
            }

            // Make sure that the packet contains its length at the front before sending
            packet.WriteLength();

            _stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
        }
    }
}