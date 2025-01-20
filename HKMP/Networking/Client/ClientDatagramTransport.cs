using System.Net.Sockets;
using Hkmp.Logging;
using Org.BouncyCastle.Tls;

namespace Hkmp.Networking.Client;

/// <summary>
/// Class that implements the DatagramTransport interface from DTLS. This class simply sends and receives data using
/// a UDP socket directly.
/// </summary>
internal class ClientDatagramTransport : DatagramTransport {
    /// <summary>
    /// The socket with which to send and over which to receive data.
    /// </summary>
    private readonly Socket _socket;
    
    public ClientDatagramTransport(Socket socket) {
        _socket = socket;
    }

    /// <summary>
    /// The maximum number of bytes to receive in a single call to <see cref="Receive"/>.
    /// </summary>
    /// <returns>The maximum number of bytes that can be received.</returns>
    public int GetReceiveLimit() {
        return DtlsClient.MaxPacketSize;
    }

    /// <summary>
    /// The maximum number of bytes to send in a single call to <see cref="Send"/>.
    /// </summary>
    /// <returns>The maximum number of bytes that can be sent.</returns>
    public int GetSendLimit() {
        return DtlsClient.MaxPacketSize;
    }

    /// <summary>
    /// This method is called whenever the corresponding DtlsTransport's Receive is called. The implementation
    /// receives data from the network and store it in the given buffer. If no data is received within the given
    /// <paramref name="waitMillis"/>, the method returns -1.
    /// </summary>
    /// <param name="buf">Byte array to store the received data.</param>
    /// <param name="off">The offset at which to begin storing the bytes.</param>
    /// <param name="len">The number of bytes that can be stored in the buffer.</param>
    /// <param name="waitMillis">The number of milliseconds to wait for data to receive.</param>
    /// <returns>The number of bytes that were received, or -1 if no bytes were received in the given time.</returns>
    public int Receive(byte[] buf, int off, int len, int waitMillis) {
        try {
            _socket.ReceiveTimeout = waitMillis;
            var numReceived = _socket.Receive(
                buf,
                off,
                len,
                SocketFlags.None,
                out var socketError
            );

            if (socketError == SocketError.Success) {
                return numReceived;
            }

            if (socketError != SocketError.WouldBlock) {
                Logger.Error($"UDP Socket Error on receive: {socketError}");
            }
        } catch (SocketException e) {
            Logger.Error($"UDP Socket exception, ErrorCode: {e.ErrorCode}, Socket ErrorCode: {e.SocketErrorCode}, Exception:\n{e}");
        }

        return -1;
    }

    /// <summary>
    /// This method is called whenever the corresponding DtlsTransport's Send is called. The implementation simply
    /// sends the data in the buffer over the network.
    /// </summary>
    /// <param name="buf">Byte array containing the bytes to send.</param>
    /// <param name="off">The offset in the buffer at which to start sending bytes.</param>
    /// <param name="len">The number of bytes to send.</param>
    public void Send(byte[] buf, int off, int len) {
        _socket.Send(buf, off, len, SocketFlags.None);
    }

    /// <summary>
    /// Cleanup login for when this transport channel should be closed.
    /// Since we handle socket closing in another class (<seealso cref="DtlsClient"/>), there is nothing here.
    /// </summary>
    public void Close() {
    }
}
