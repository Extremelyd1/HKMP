using System.Net.Sockets;
using Hkmp.Logging;
using Org.BouncyCastle.Tls;

namespace Hkmp.Networking.Client;

internal class ClientDatagramTransport : DatagramTransport {
    private readonly Socket _socket;
    
    public ClientDatagramTransport(Socket socket) {
        _socket = socket;
    }
    
    public int GetReceiveLimit() {
        // TODO: change to const defined somewhere
        return 1400;
    }
    
    public int GetSendLimit() {
        // TODO: change to const defined somewhere
        return 1400;
    }

    public int Receive(byte[] buf, int off, int len, int waitMillis) {
        try {
            // _socket.ReceiveTimeout = waitMillis;
            var numReceived = _socket.Receive(
                buf,
                off,
                len,
                SocketFlags.None
            );
            Logger.Debug($"Client socket receive: {numReceived}");
            return numReceived;
        } catch (SocketException e) {
            Logger.Error($"UDP Socket exception:\n{e}");
        }

        return -1;
    }

    public void Send(byte[] buf, int off, int len) {
        Logger.Debug($"Client sending {len} bytes of data");
        _socket.Send(buf, off, len, SocketFlags.None);
    }

    public void Close() {
    }
}
