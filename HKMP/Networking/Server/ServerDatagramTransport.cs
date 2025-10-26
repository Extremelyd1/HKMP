using System.Net;
using System.Net.Sockets;
using Hkmp.Logging;

namespace Hkmp.Networking.Server;

/// <summary>
/// Class that implements the DatagramTransport interface from DTLS. This class simply sends and receives data based
/// on a blocking collection that is filled by data received by the DTLS server.
/// </summary>
internal class ServerDatagramTransport : UdpDatagramTransport {
    /// <summary>
    /// The socket instance solely used to send data.
    /// </summary>
    private readonly Socket _socket;
    
    /// <summary>
    /// The IP endpoint for the client that this datagram transport belongs to.
    /// </summary>
    public IPEndPoint IPEndPoint { get; set; }

    public ServerDatagramTransport(Socket socket) {
        _socket = socket;
    }

    /// <inheritdoc />
    public override int GetReceiveLimit() {
        return DtlsServer.MaxPacketSize;
    }

    /// <inheritdoc />
    public override int GetSendLimit() {
        return DtlsServer.MaxPacketSize;
    }

    /// <inheritdoc />
    /// The implementation simply sends the data in the buffer over the network to the IP endpoint in this instance.
    public override void Send(byte[] buf, int off, int len) {
        if (IPEndPoint == null) {
            Logger.Error("Cannot send because transport has no endpoint");
            return;
        }

        _socket.SendTo(buf, off, len, SocketFlags.None, IPEndPoint);
    }
}
