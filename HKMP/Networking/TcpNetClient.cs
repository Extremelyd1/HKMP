using System;
using System.Collections.Generic;
using System.Net.Sockets;
using HKMP.Networking.Client;
using HKMP.Networking.Packet;
using UnityEngine;

namespace HKMP.Networking {
    /**
     * NetClient that uses the TCP protocol
     */
    public class TcpNetClient {
        private static readonly int MaxBufferSize = (int) Mathf.Pow(2, 20);
        
        private readonly object _lock = new object();

        private TcpClient _tcpClient;
        private NetworkStream _stream;

        private byte[] _receivedData;
        private byte[] _leftoverData;

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
                Logger.Error(this, "Connection failed in FinishConnectionSetup, this shouldn't happen");
                return;
            }

            Logger.Info(this, "Connection success, setting up network stream");

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
            var dataLength = 0;
            try {
                dataLength = _stream.EndRead(result);
            } catch (Exception e) {
                Logger.Info(this, $"TCP Receive exception, message: {e.Message}");
            }
            
            if (dataLength <= 0) {
                Logger.Error(this, $"Received incorrect data length: {dataLength}");

                if (!_tcpClient.Connected) {
                    Logger.Info(this, "The TCP client is not connected anymore, stopped reading from stream");
                    return;
                }
                
                // Create new byte array and start listening/reading for new data
                _receivedData = new byte[MaxBufferSize];
                _stream.BeginRead(_receivedData, 0, MaxBufferSize, OnReceive, null);
                return;
            }
            
            // Create new byte array with exact length of received data
            var trimmedData = new byte[dataLength];
            // Copy over the data to new array
            Array.Copy(_receivedData, trimmedData, dataLength);
            
            // After the data is copied, create new byte array
            // and start listening/reading for new data
            _receivedData = new byte[MaxBufferSize];
            _stream.BeginRead(_receivedData, 0, MaxBufferSize, OnReceive, null);
            
            List<Packet.Packet> packets;
            
            // Lock the leftover data array for synchronous data handling
            // This makes sure that from another asynchronous receive callback we don't
            // read/write to it in different places
            lock (_lock) {
                packets = PacketManager.HandleReceivedData(trimmedData, ref _leftoverData);
            }

            // If callback exists, execute it
            _onReceive?.Invoke(packets);
        }

        /**
         * Disconnects the TCP client from the open connection
         */
        public void Disconnect() {
            if (!_tcpClient.Connected) {
                Logger.Warn(this, "TCP client was not connected, trying to close anyway");
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

            _stream.Write(packet.ToArray(), 0, packet.Length());
        }
    }
}