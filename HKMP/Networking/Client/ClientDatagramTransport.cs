using System.Net.Sockets;

namespace Hkmp.Networking.Client;

/// <summary>
/// Class that implements the DatagramTransport interface from DTLS. This class simply sends and receives data using
/// a UDP socket directly.
/// </summary>
internal class ClientDatagramTransport : UdpDatagramTransport {
    /// <summary>
    /// The socket with which to send and over which to receive data.
    /// </summary>
    private readonly Socket _socket;
    
    public ClientDatagramTransport(Socket socket) {
        _socket = socket;
    }

    /// <inheritdoc />
    public override int GetReceiveLimit() {
        return DtlsClient.MaxPacketSize;
    }

    /// <inheritdoc />
    public override int GetSendLimit() {
        return DtlsClient.MaxPacketSize;
    }

    /// <inheritdoc />
    /// The implementation simply sends the data in the buffer over the network using the socket.
    public override void Send(byte[] buf, int off, int len) {
        _socket.Send(buf, off, len, SocketFlags.None);
    }
}
