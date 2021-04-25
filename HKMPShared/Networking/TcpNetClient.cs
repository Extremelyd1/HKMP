using System;
using System.Net;
using System.Net.Sockets;

namespace HKMP {
    /**
     * NetClient that uses the TCP protocol
     */
    public class TcpNetClient {
        private static readonly int MaxBufferSize = (int) System.Math.Pow(2, 20);

        private TcpClient _tcpClient;

        private Action _onConnect;
        private Action _onConnectFailed;

        public void RegisterOnConnect(Action onConnect) {
            _onConnect = onConnect;
        }

        public void RegisterOnConnectFailed(Action onConnectFailed) {
            _onConnectFailed = onConnectFailed;
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
            Logger.Get().Info(this, "TCP Begin Connect");
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
                    Logger.Get().Info(this, $"Connection failed: {e.Message}");
                    // Invoke callback if it exists
                    _onConnectFailed?.Invoke();

                    return;
                }
            } else {
                Logger.Get().Warn(this, "Result in OnConnect is null");
                // This probably means that the connection failed, so invoke the callback
                _onConnectFailed?.Invoke();
                return;
            }

            FinishConnectionSetup();
        }

        private void FinishConnectionSetup() {
            if (!_tcpClient.Connected) {
                Logger.Get().Error(this, "Connection failed in FinishConnectionSetup, this shouldn't happen");
                return;
            }

            Logger.Get().Info(this, "Connection success");

            // Invoke callback if it exists
            _onConnect?.Invoke();
        }

        /**
         * Disconnects the TCP client from the open connection
         */
        public void Disconnect() {
            if (!_tcpClient.Connected) {
                Logger.Get().Warn(this, "TCP client was not connected, trying to close anyway");
            }
            
            _tcpClient.Close();
        }

        public int GetConnectedPort() {
            if (!_tcpClient.Connected) {
                return -1;
            }

            return ((IPEndPoint) _tcpClient.Client.LocalEndPoint).Port;
        }
    }
}