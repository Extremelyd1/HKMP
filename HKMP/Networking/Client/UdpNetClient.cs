using System.Net;
using System.Net.Sockets;
using System.Threading;
using Hkmp.Logging;
using Hkmp.Networking.Packet;
using Org.BouncyCastle.Tls;

namespace Hkmp.Networking.Client;

/// <summary>
/// NetClient that uses the UDP protocol.
/// </summary>
internal class UdpNetClient : DatagramTransport {
    /// <summary>
    /// Maximum size of a UDP packet in bytes.
    /// </summary>
    private const int MaxUdpPacketSize = 65527;

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
    /// Cancellation token source for the thread of receiving network data.
    /// </summary>
    private CancellationTokenSource _receiveTokenSource;

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
            Logger.Error($"Socket exception when connecting UDP socket:\n{e}");

            UdpSocket.Close();
            UdpSocket = null;

            throw;
        }
    }

    /// <summary>
    /// Disconnect the UDP client and clean it up.
    /// </summary>
    public void Disconnect() {
        // Request cancellation of the receive thread
        _receiveTokenSource.Cancel();

        UdpSocket?.Close();
        UdpSocket = null;
    }

    public int GetReceiveLimit() {
        return 1400;
    }
    
    public int GetSendLimit() {
        return 1400;
    }

    public int Receive(byte[] buf, int off, int len, int waitMillis) {
        EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);

        try {
            return UdpSocket.ReceiveFrom(
                buf,
                off,
                len,
                SocketFlags.None,
                ref endPoint
            );
        } catch (SocketException e) {
            Logger.Error($"UDP Socket exception:\n{e}");
        }

        return -1;
    }

    public void Send(byte[] buf, int off, int len) {
        UdpSocket.Send(buf, off, len, SocketFlags.None);
    }

    public void Close() {
        throw new System.NotImplementedException();
    }
}
