using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Hkmp.Logging;
using Org.BouncyCastle.Tls;

namespace Hkmp.Networking.Server;

/// <summary>
/// Class that implements the DatagramTransport interface from DTLS. This class simply sends and receives data based
/// on a blocking collection that is filled by data received by the DTLS server.
/// </summary>
internal class ServerDatagramTransport : DatagramTransport {
    /// <summary>
    /// The socket instance solely used to send data.
    /// </summary>
    private readonly Socket _socket;
    
    /// <summary>
    /// Token source for cancelling the blocking call on the received data collection.
    /// </summary>
    private readonly CancellationTokenSource _cancellationTokenSource;

    /// <summary>
    /// The IP endpoint for the client that this datagram transport belongs to.
    /// </summary>
    public IPEndPoint IPEndPoint { get; set; }

    /// <summary>
    /// A thread-safe blocking collection storing received data that is used to handle the "Receive" calls from the
    /// DTLS transport.
    /// </summary>
    public BlockingCollection<ReceivedData> ReceivedDataCollection { get; }

    public ServerDatagramTransport(Socket socket) {
        _socket = socket;
        
        _cancellationTokenSource = new CancellationTokenSource();

        ReceivedDataCollection = new BlockingCollection<ReceivedData>();
    }

    /// <summary>
    /// The maximum number of bytes to receive in a single call to <see cref="Receive"/>.
    /// </summary>
    /// <returns>The maximum number of bytes that can be received.</returns>
    public int GetReceiveLimit() {
        return DtlsServer.MaxPacketSize;
    }

    /// <summary>
    /// The maximum number of bytes to send in a single call to <see cref="Send"/>.
    /// </summary>
    /// <returns>The maximum number of bytes that can be sent.</returns>
    public int GetSendLimit() {
        return DtlsServer.MaxPacketSize;
    }

    /// <summary>
    /// This method is called whenever the corresponding DtlsTransport's Receive is called. The implementation
    /// obtains data from the blocking collection and store it in the given buffer. If no data is present in the
    /// collection within the given <paramref name="waitMillis"/>, the method returns -1.
    /// </summary>
    /// <param name="buf">Byte array to store the received data.</param>
    /// <param name="off">The offset at which to begin storing the data.</param>
    /// <param name="len">The number of bytes that can be stored in the buffer.</param>
    /// <param name="waitMillis">The number of milliseconds to wait for data to fill.</param>
    /// <returns>The number of bytes that were received, or -1 if no bytes were received in the given time.</returns>
    public int Receive(byte[] buf, int off, int len, int waitMillis) {
        if (_cancellationTokenSource.IsCancellationRequested) {
            return -1;
        }

        bool tryTakeSuccess;
        ReceivedData data;

        try {
            tryTakeSuccess = ReceivedDataCollection.TryTake(out data, waitMillis, _cancellationTokenSource.Token);
        } catch (OperationCanceledException) {
            return -1;
        }

        if (!tryTakeSuccess) {
            return -1;
        }

        // If there is more data in the entry we received from the blocking collection than space in the buffer
        // from the method, we need to add as much data into the buffer and put the rest back in the collection
        if (len < data.Length) {
            // Fill the buffer from the method with as much data from the entry as possible
            for (var i = off; i < off + len; i++) {
                buf[i] = data.Buffer[i - off];
            }

            // Calculate the length of the leftover buffer and instantiate it
            var leftoverLength = data.Length - len;
            var leftoverBuffer = new byte[leftoverLength];

            // Fill the leftover buffer with the leftover data from the entry
            for (var i = 0; i < leftoverLength; i++) {
                leftoverBuffer[i] = data.Buffer[len + i];
            }

            // Add the leftover buffer and its length back to the collection
            ReceivedDataCollection.Add(new ReceivedData {
                Buffer = leftoverBuffer,
                Length = leftoverLength
            });

            return len;
        }

        // In this case, the space in the buffer from the method is large enough, so we fill it with all the data
        // from the collection entry
        for (var i = 0; i < data.Length; i++) {
            buf[off + i] = data.Buffer[i];
        }

        return data.Length;
    }

    /// <summary>
    /// This method is called whenever the corresponding DtlsTransport's Send is called. The implementation simply
    /// sends the data in the buffer over the network.
    /// </summary>
    /// <param name="buf">Byte array containing the bytes to send.</param>
    /// <param name="off">The offset in the buffer at which to start sending bytes.</param>
    /// <param name="len">The number of bytes to send.</param>
    public void Send(byte[] buf, int off, int len) {
        if (IPEndPoint == null) {
            Logger.Error("Cannot send because transport has no endpoint");
            return;
        }

        _socket.SendTo(buf, off, len, SocketFlags.None, IPEndPoint);
    }

    /// <summary>
    /// Cleanup login for when this transport channel should be closed.
    /// </summary>
    public void Close() {
        _cancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// Dispose of the underlying unmanaged resources.
    /// </summary>
    public void Dispose() {
        _cancellationTokenSource?.Dispose();
        ReceivedDataCollection?.Dispose();
    }

    /// <summary>
    /// Data class containing a buffer and the corresponding length of bytes stored in that buffer. Not necessarily
    /// the length of the buffer.
    /// </summary>
    public class ReceivedData {
        /// <summary>
        /// Byte array containing the data.
        /// </summary>
        public byte[] Buffer { get; set; }
        /// <summary>
        /// The number of bytes in the buffer.
        /// </summary>
        public int Length { get; set; }
    }
}
