using System;
using System.IO;
using System.Net;
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
    /// The maximum time the DTLS handshake can take in milliseconds before timing out.
    /// </summary>
    public const int DtlsHandshakeTimeoutMillis = 5000;

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
    /// Token source for cancellation tokens for the receive task.
    /// </summary>
    private CancellationTokenSource _receiveTaskTokenSource;

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
            _receiveTaskTokenSource != null
        ) {
            Disconnect();
        }
        
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        try {
            _socket.Connect(address, port);
        } catch (SocketException e) {
            Logger.Error($"Socket exception when connecting UDP socket:\n{e}");
            
            Disconnect();

            throw;
        }
        
        var clientProtocol = new DtlsClientProtocol();
        _tlsClient = new ClientTlsClient(new BcTlsCrypto());
        _clientDatagramTransport = new ClientDatagramTransport(_socket);
        
        // Create the token source, because we need the token for the receive loop
        _receiveTaskTokenSource = new CancellationTokenSource();
        var cancellationToken = _receiveTaskTokenSource.Token;
        
        // Start the socket receive loop, since during the DTLS connection, it needs to receive data
        new Thread(() => SocketReceiveLoop(cancellationToken)).Start();

        try {
            DtlsTransport = clientProtocol.Connect(_tlsClient, _clientDatagramTransport);
        } catch (TlsTimeoutException) {
            Disconnect();
            throw;
        } catch (IOException e) {
            Logger.Error($"IO exception when connecting DTLS client:\n{e}");
            Disconnect();
            throw;
        }
        
        Logger.Debug($"Successfully connected DTLS client to endpoint: {address}:{port}");

        new Thread(() => DtlsReceiveLoop(cancellationToken)).Start();
    }

    /// <summary>
    /// Disconnect the DTLS client from the server. This will cancel, dispose, or close all internal objects to
    /// clean up potential previous connection attempts.
    /// </summary>
    public void Disconnect() {
        _receiveTaskTokenSource?.Cancel();
        _receiveTaskTokenSource?.Dispose();
        _receiveTaskTokenSource = null;
        
        DtlsTransport?.Close();
        DtlsTransport = null;
        
        _clientDatagramTransport?.Dispose();
        _clientDatagramTransport = null;
        
        _tlsClient?.Cancel();
        _tlsClient = null;
        
        _socket?.Close();
        _socket = null;
    }

    /// <summary>
    /// Continuously tries to receive data from the socket until cancellation is requested.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the loop.</param>
    private void SocketReceiveLoop(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
            
            var numReceived = 0;
            var buffer = new byte[MaxPacketSize];

            try {
                numReceived = _socket.ReceiveFrom(
                    buffer,
                    SocketFlags.None,
                    ref endPoint
                );
            } catch (SocketException e) when (e.SocketErrorCode == SocketError.Interrupted) {
                // SocketError Interrupted happens when the socket is closed during the receive call
                // We close the socket when the client disconnects, thus this exception is expected, so we simply break
                Logger.Debug("SocketException with error code interrupted");
                break;
            } catch (SocketException e) {
                Logger.Error($"UDP Socket exception, ErrorCode: {e.ErrorCode}, Socket ErrorCode: {e.SocketErrorCode}, Exception:\n{e}");
            }

            if (cancellationToken.IsCancellationRequested) {
                Logger.Debug("Cancellation requested");
                break;
            }

            try {
                _clientDatagramTransport.ReceivedDataCollection.Add(new UdpDatagramTransport.ReceivedData {
                    Buffer = buffer,
                    Length = numReceived
                }, cancellationToken);
            } catch (OperationCanceledException) {
                Logger.Debug("OperationCanceledException");
                break;
            }
        }
        
        Logger.Debug("Receive loop cancelled");
    }

    /// <summary>
    /// Continuously tries to receive data from the DTLS transport until cancellation is requested.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the loop.</param>
    private void DtlsReceiveLoop(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested && DtlsTransport != null) {
            var buffer = new byte[MaxPacketSize];
            var length = DtlsTransport.Receive(buffer, 0, buffer.Length, 5);
            if (length >= 0) {
                DataReceivedEvent?.Invoke(buffer, length);
            }
        }
    }
}
