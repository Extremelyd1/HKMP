using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Hkmp.Logging;
using Org.BouncyCastle.Tls;

namespace Hkmp.Networking.Server;

internal class ServerDatagramTransport : DatagramTransport {
    private readonly Socket _socket;

    public IPEndPoint IPEndPoint { get; set; }
    
    public BlockingCollection<ReceivedData> ReceivedDataCollection { get; }

    public ServerDatagramTransport(Socket socket) {
        _socket = socket;

        ReceivedDataCollection = new BlockingCollection<ReceivedData>();
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
        if (!ReceivedDataCollection.TryTake(out var data, waitMillis)) {
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

    public void Send(byte[] buf, int off, int len) {
        if (IPEndPoint == null) {
            Logger.Error("Cannot send because transport has no endpoint");
            return;
        }
        
        Logger.Debug($"Server sending {len} bytes of data to: {IPEndPoint}");
        
        _socket.SendTo(buf, off, len, SocketFlags.None, IPEndPoint);
    }

    public void Close() {
    }

    public class ReceivedData {
        public byte[] Buffer { get; set; }
        public int Length { get; set; }
    }
}
