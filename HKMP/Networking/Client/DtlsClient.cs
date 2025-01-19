using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Hkmp.Logging;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace Hkmp.Networking.Client;

/// <summary>
/// DTLS implementation for a client-side peer for networking.
/// </summary>
internal class DtlsClient {
    /// <summary>
    /// The maximum packet size for sending and receiving DTLS packets.
    /// </summary>
    public const int MaxPacketSize = 1400;

    /// <summary>
    /// The socket instance for the underlying networking.
    /// </summary>
    private Socket _socket;
    /// <summary>
    /// The TLS client for communicating supported cipher suites and handling certificates.
    /// </summary>
    private ClientTlsClient _tlsClient;
    /// <summary>
    /// The client datagram transport that provides networking to the DTLS client.
    /// </summary>
    private ClientDatagramTransport _clientDatagramTransport;

    /// <summary>
    /// Token source for cancellation tokens for the update task.
    /// </summary>
    private CancellationTokenSource _updateTaskTokenSource;

    /// <summary>
    /// DTLS transport instance from establishing a connection to a server.
    /// </summary>
    public DtlsTransport DtlsTransport { get; private set; }
    
    /// <summary>
    /// Event that is called when data is received from the server. 
    /// </summary>
    public event Action<byte[], int> DataReceivedEvent;

    /// <summary>
    /// Try to establish a connection to a server with the given address and port.
    /// </summary>
    /// <param name="address">The address of the server.</param>
    /// <param name="port">The port of the server.</param>
    /// <exception cref="SocketException">Thrown when the underlying socket fails to connect to the server.</exception>
    /// <exception cref="IOException">Thrown when the DTLS protocol fails to connect to the server.</exception>
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

    /// <summary>
    /// Disconnect the DTLS client from the server.
    /// </summary>
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

    /// <summary>
    /// Continuously tries to receive data from the DTLS transport until cancellation is requested.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the loop.</param>
    private void ReceiveLoop(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested && DtlsTransport != null) {
            var buffer = new byte[MaxPacketSize];
            var length = DtlsTransport.Receive(buffer, 0, buffer.Length, 5);
            if (length >= 0) {
                DataReceivedEvent?.Invoke(buffer, length);
            }
        }
    }
}
