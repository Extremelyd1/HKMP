﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Hkmp.Logging;
using Hkmp.Networking.Packet;

namespace Hkmp.Networking.Client {
    /// <summary>
    /// NetClient that uses the UDP protocol.
    /// </summary>
    internal class UdpNetClient {
        private const int MaxUdpPacketSize = 65527;

        private static readonly IPEndPoint BlankEndpoint = new IPEndPoint(IPAddress.Any, 0);

        /// <summary>
        /// The underlying UDP socket.
        /// </summary>
        public Socket UdpSocket;

        /// <summary>
        /// Delegate called when packets are received.
        /// </summary>
        private OnReceive _onReceive;

        /// <summary>
        /// Byte array containing received data that was not included in a packet object yet.
        /// </summary>
        private byte[] _leftoverData;

        /// <summary>
        /// Cancellation token source for the task of receiving network data.
        /// </summary>
        private CancellationTokenSource _receiveTaskTokenSource;

        /// <summary>
        /// Register a callback for when packets are received.
        /// </summary>
        /// <param name="onReceive">The delegate that handles the received packets.</param>
        public void RegisterOnReceive(OnReceive onReceive) {
            _onReceive = onReceive;
        }

        /// <summary>
        /// Connects the UDP socket to the host at the given address and port.
        /// </summary>
        /// <param name="address">The address of the host.</param>
        /// <param name="port">The port of the host.</param>
        public void Connect(string address, int port) {
            UdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try {
                UdpSocket.Connect(address, port);
            } catch (SocketException e) {
                Logger.Error($"Socket exception when connecting UDP socket: {e.Message}");

                UdpSocket.Close();
                UdpSocket = null;

                throw;
            }

            Logger.Info($"Starting receiving UDP data on endpoint {UdpSocket.LocalEndPoint}");

            // Start a long-running task to receive network data and a corresponding cancellation token
            _receiveTaskTokenSource = new CancellationTokenSource();
            var cancellationToken = _receiveTaskTokenSource.Token;

            Task.Factory.StartNew(
                () => ReceiveAsync(cancellationToken),
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            );
        }

        /// <summary>
        /// Task that continuously receives network UDP data and queues it for processing.
        /// </summary>
        /// <param name="token">The cancellation token for checking whether this task is requested to cancel.</param>
        private async Task ReceiveAsync(CancellationToken token) {
            while (!token.IsCancellationRequested) {
                var buffer = new byte[MaxUdpPacketSize];
                var bufferMem = new ArraySegment<byte>(buffer);

                try {
                    await UdpSocket.ReceiveFromAsync(
                        bufferMem,
                        SocketFlags.None,
                        BlankEndpoint
                    );

                    var packets = PacketManager.HandleReceivedData(bufferMem.Array, ref _leftoverData);

                    _onReceive?.Invoke(packets);
                } catch (SocketException e) {
                    Logger.Error($"UDP Socket exception: {e.GetType()}, {e.Message}");
                }
            }
        }

        /// <summary>
        /// Disconnect the UDP client and clean it up.
        /// </summary>
        public void Disconnect() {
            // Request cancellation of the receive task
            _receiveTaskTokenSource.Cancel();

            UdpSocket.Close();
            UdpSocket = null;
        }
    }
}
