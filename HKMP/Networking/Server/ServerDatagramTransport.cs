using System.Net;
using System.Net.Sockets;
using Hkmp.Logging;
using Org.BouncyCastle.Tls;

namespace Hkmp.Networking.Server;

internal class ServerDatagramTransport : DatagramTransport {
    private readonly Socket _socket;

    private bool _hasReceived;

    public ServerDatagramTransport(Socket socket) {
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
        if (!_hasReceived) {
            EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
            
            try {
                Logger.Debug("Blocking on first receive");
            
                // _socket.ReceiveTimeout = waitMillis;
                var numReceived = _socket.ReceiveFrom(
                    buf,
                    off,
                    len,
                    SocketFlags.None,
                    ref endPoint
                );
            
                _socket.Connect(endPoint);
                _hasReceived = true;
                
                Logger.Debug($"First receive finished ({numReceived}), connecting socket to endpoint: {endPoint}");
                return numReceived;
            } catch (SocketException e) {
                Logger.Error($"UDP Socket exception:\n{e}");
            }
        } else {
            try {
                Logger.Debug("Non-first blocking receive called");
                // _socket.ReceiveTimeout = waitMillis;
                var numReceived = _socket.Receive(
                    buf,
                    off,
                    len,
                    SocketFlags.None
                );
                Logger.Debug($"End of non-first blocking receive: {numReceived}");
                return numReceived;
            } catch (SocketException e) {
                Logger.Error($"UDP Socket exception:\n{e}");
            }
        }

        return -1;
    }

    public void Send(byte[] buf, int off, int len) {
        if (!_hasReceived) {
            Logger.Error("Cannot send because socket has not received yet");
            return;
        }
        
        Logger.Debug($"Server sending {len} bytes of data");
        
        _socket.Send(buf, off, len, SocketFlags.None);
    }

    public void Close() {
    }
}
