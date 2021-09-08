using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Hkmp.Networking.Packet;

namespace Hkmp.Networking.Client {
    /**
     * NetClient that uses the UDP protocol
     */
    public class UdpNetClient {
        private readonly object _lock = new object();

        public UdpClient UdpClient;

        private OnReceive _onReceive;

        private byte[] _leftoverData;

        public void RegisterOnReceive(OnReceive onReceive) {
            _onReceive = onReceive;
        }

        public void Connect(string host, int port) {
            UdpClient = new UdpClient();
            try {
                UdpClient.Connect(host, port);
            } catch (SocketException) {
                UdpClient.Close();
                UdpClient = null;

                throw;
            }

            UdpClient.BeginReceive(OnReceive, null);

            Logger.Get().Info(this, $"Starting receiving UDP data on endpoint {UdpClient.Client.LocalEndPoint}");
        }

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

        /**
         * Disconnect the UDP client and clean it up
         */
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