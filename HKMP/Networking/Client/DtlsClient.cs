using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Hkmp.Logging;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace Hkmp.Networking.Client;

internal class DtlsClient {
    private Socket _socket;
    private ClientTlsClient _tlsClient;
    private ClientDatagramTransport _clientDatagramTransport;

    private CancellationTokenSource _updateTaskTokenSource;

    public DtlsTransport DtlsTransport { get; set; }
    public event Action<byte[], int> DataReceivedEvent;
    
    public void Connect(string address, int port) {
        if (_socket != null || 
            _tlsClient != null ||
            _clientDatagramTransport != null || 
            DtlsTransport != null ||
            _updateTaskTokenSource != null
        ) {
            Disconnect();
        }
        
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        // _socket.DualMode = true;

        try {
            _socket.Connect(address, port);
        } catch (SocketException e) {
            Logger.Error($"Socket exception when connecting UDP socket:\n{e}");
            
            _socket.Close();

            throw;
        }
        
        var clientProtocol = new DtlsClientProtocol();
        _tlsClient = new ClientTlsClient(new BcTlsCrypto());
        _clientDatagramTransport = new ClientDatagramTransport(_socket);

        try {
            DtlsTransport = clientProtocol.Connect(_tlsClient, _clientDatagramTransport);
        } catch (IOException e) {
            Logger.Error($"IO exception when connecting DTLS client:\n{e}");

            _clientDatagramTransport.Close();

            throw;
        }
        
        Logger.Debug($"Successfully connected DTLS client to endpoint: {address}:{port}");

        _updateTaskTokenSource = new CancellationTokenSource();
        var cancellationToken = _updateTaskTokenSource.Token;
        new Thread(() => ReceiveLoop(cancellationToken)).Start();
    }

    public void Disconnect() {
        _updateTaskTokenSource?.Cancel();
        _updateTaskTokenSource?.Dispose();
        _updateTaskTokenSource = null;
        
        DtlsTransport?.Close();
        DtlsTransport = null;
        
        _clientDatagramTransport?.Close();
        _clientDatagramTransport = null;
        
        _tlsClient?.Cancel();
        _tlsClient = null;
        
        _socket?.Close();
        _socket = null;
    }

    public void SendPacket(Packet.Packet packet) {
        if (DtlsTransport == null) {
            Logger.Error("DTLS transport instance is null, cannot send packet");
            return;
        }
        
        var buffer = packet.ToArray();

        DtlsTransport.Send(buffer, 0, buffer.Length);
    }

    private void ReceiveLoop(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested && DtlsTransport != null) {
            // TODO: change to const define somewhere central
            var buffer = new byte[1400];
            var length = DtlsTransport.Receive(buffer, 0, buffer.Length, 5);
            if (length >= 0) {
                DataReceivedEvent?.Invoke(buffer, length);
            }
        }
    }
}
