using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Hkmp.Logging;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace Hkmp.Networking.Server;

internal class DtlsServer {
    private DtlsServerProtocol _serverProtocol;
    private ServerTlsServer _tlsServer;

    private Socket _currentAcceptSocket;
    private ServerDatagramTransport _currentDatagramTransport;

    private CancellationTokenSource _acceptTaskTokenSource;
    private CancellationTokenSource _clientUpdateTaskTokenSource;

    private readonly List<DtlsServerClient> _acceptedDtlsClients;

    public event Action<DtlsServerClient, byte[], int> DataReceivedEvent;

    private int _port;

    public DtlsServer() {
        _acceptedDtlsClients = new List<DtlsServerClient>();
    }
    
    public void Start(int port) {
        _port = port;
        
        _serverProtocol = new DtlsServerProtocol();
        _tlsServer = new ServerTlsServer(new BcTlsCrypto());

        _acceptTaskTokenSource = new CancellationTokenSource();
        _clientUpdateTaskTokenSource = new CancellationTokenSource();
        
        var cancellationToken = _acceptTaskTokenSource.Token;
        new Thread(() => AcceptLoop(cancellationToken)).Start();
    }

    public void Stop() {
        _acceptTaskTokenSource?.Cancel();
        _acceptTaskTokenSource?.Dispose();
        _acceptTaskTokenSource = null;
        
        _clientUpdateTaskTokenSource?.Cancel();
        _clientUpdateTaskTokenSource?.Dispose();
        _clientUpdateTaskTokenSource = null;
    }

    private void AcceptLoop(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            Logger.Debug("Creating new socket for accepting connection");
            
            _currentAcceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // _currentAcceptSocket.DualMode = true;
            _currentAcceptSocket.Bind(new IPEndPoint(IPAddress.Any, _port));
            // Logger.Debug("Setting socket to listening mode");
            // _currentAcceptSocket.Listen(1);
            // Logger.Debug("Accepting connections for socket");
            // var socket = _currentAcceptSocket.Accept();
            // Logger.Debug($"Connection accepted, remote endpoint: {socket.RemoteEndPoint}");

            _currentDatagramTransport = new ServerDatagramTransport(_currentAcceptSocket);

            DtlsTransport dtlsTransport;
            try {
                dtlsTransport = _serverProtocol.Accept(_tlsServer, _currentDatagramTransport);
            } catch (IOException e) {
                Logger.Error($"IOException while accepting DTLS connection:\n{e}");
                break;
            }

            Logger.Debug($"Accepted DTLS connection on socket, endpoint: {_currentAcceptSocket.RemoteEndPoint}");

            var dtlsServerClient = new DtlsServerClient {
                DtlsTransport = dtlsTransport,
                EndPoint = _currentAcceptSocket.RemoteEndPoint as IPEndPoint
            };

            _acceptedDtlsClients.Add(dtlsServerClient);

            var clientCancellationToken = _clientUpdateTaskTokenSource.Token;
            new Thread(() => ReceiveClientLoop(dtlsServerClient, clientCancellationToken)).Start();
        }

        _currentAcceptSocket?.Close();
        _currentAcceptSocket = null;
        
        _currentDatagramTransport?.Close();
        _currentDatagramTransport = null;

        _serverProtocol = null;

        foreach (var dtlsServerClient in _acceptedDtlsClients) {
            dtlsServerClient.DtlsTransport?.Close();
        }
        
        _acceptedDtlsClients.Clear();
    }

    private void ReceiveClientLoop(DtlsServerClient dtlsServerClient, CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            // TODO: change to const defined somewhere central
            var buffer = new byte[1400];
            var length = dtlsServerClient.DtlsTransport.Receive(buffer, 0, buffer.Length, 5);
            if (length >= 0) {
                DataReceivedEvent?.Invoke(dtlsServerClient, buffer, length);
            }
        }
    }
}
