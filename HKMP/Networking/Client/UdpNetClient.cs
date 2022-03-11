using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Hkmp.Networking.Packet;

namespace Hkmp.Networking.Client {
    /// <summary>
    /// NetClient that uses the UDP protocol.
    /// </summary>
    internal class UdpNetClient {
        /// <summary>
        /// Object to lock asynchronous access.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// The underlying UDP client.
        /// </summary>
        public UdpClient UdpClient;

        /// <summary>
        /// Delegate called when packets are received.
        /// </summary>
        private OnReceive _onReceive;

        /// <summary>
        /// Byte array containing received data that was not included in a packet object yet.
        /// </summary>
        private byte[] _leftoverData;

        /// <summary>
        /// Register a callback for when packets are received.
        /// </summary>
        /// <param name="onReceive">The delegate that handles the received packets.</param>
        public void RegisterOnReceive(OnReceive onReceive) {
            _onReceive = onReceive;
        }

        /// <summary>
        /// Connects the UDP client to the host at the given address and port.
        /// </summary>
        /// <param name="address">The address of the host.</param>
        /// <param name="port">The port of the host.</param>
        public void Connect(string address, int port) {
            UdpClient = new UdpClient();
            try {
                UdpClient.Connect(address, port);
            } catch (SocketException) {
                UdpClient.Close();
                UdpClient = null;

                throw;
            }

            UdpClient.BeginReceive(OnReceive, null);

            Logger.Get().Info(this, $"Starting receiving UDP data on endpoint {UdpClient.Client.LocalEndPoint}");
        }

        /// <summary>
        /// Handler for the asynchronous callback of the UDP client receiving data.
        /// </summary>
        /// <param name="result">The async result.</param>
        private void OnReceive(IAsyncResult result) {
            IPEndPoint ipEndPoint = null;
            byte[] receivedData;

            try {
                receivedData = UdpClient.EndReceive(result, ref ipEndPoint);
            } catch (Exception e) {
                Logger.Get().Warn(this, $"UDP Receive exception: {e.Message}");
                return;
            } finally {
                // Immediately start listening for new data
                // Only do this when the client exists, we might have closed the client
                UdpClient?.BeginReceive(OnReceive, null);
            }

            // If we did not receive at least an int of bytes, something went wrong
            if (receivedData.Length < 4) {
                Logger.Get().Error(this, $"Received incorrect data length: {receivedData.Length}");

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

        /// <summary>
        /// Disconnect the UDP client and clean it up.
        /// </summary>
        public void Disconnect() {
            if (!UdpClient.Client.Connected) {
                Logger.Get().Warn(this, "UDP client was not connected, cannot disconnect");
                return;
            }

            UdpClient.Close();
            UdpClient = null;
        }
    }
}