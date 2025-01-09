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

    private Socket _socket;
    private ServerDatagramTransport _currentDatagramTransport;

    private CancellationTokenSource _cancellationTokenSource;

    private readonly Dictionary<IPEndPoint, DtlsServerClient> _dtlsClients;

    public event Action<DtlsServerClient, byte[], int> DataReceivedEvent;

    private int _port;

    public DtlsServer() {
        _dtlsClients = new Dictionary<IPEndPoint, DtlsServerClient>();
    }
    
    public void Start(int port) {
        _port = port;
        
        _serverProtocol = new DtlsServerProtocol();
        _tlsServer = new ServerTlsServer(new BcTlsCrypto());

        _cancellationTokenSource = new CancellationTokenSource();

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(new IPEndPoint(IPAddress.Any, _port));
        
        new Thread(() => AcceptLoop(_cancellationTokenSource.Token)).Start();
        new Thread(() => SocketReceiveLoop(_cancellationTokenSource.Token)).Start();
    }

    public void Stop() {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    public void DisconnectClient(IPEndPoint endPoint) {
        if (!_dtlsClients.TryGetValue(endPoint, out var dtlsServerClient)) {
            Logger.Warn("Could not find DtlsServerClient to disconnect");
            return;
        }

        dtlsServerClient.ReceiveLoopTokenSource?.Cancel();
        dtlsServerClient.ReceiveLoopTokenSource?.Dispose();
        
        dtlsServerClient.DatagramTransport?.Close();
        dtlsServerClient.DtlsTransport?.Close();
    }

    private void SocketReceiveLoop(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);

            Logger.Debug("Blocking on server socket receive");
            var numReceived = 0;
            var buffer = new byte[1400];

            try {
                numReceived = _socket.ReceiveFrom(
                    buffer,
                    SocketFlags.None,
                    ref endPoint
                );
            } catch (SocketException e) {
                Logger.Error($"UDP server socket exception:\n{e}");
            }

            var ipEndPoint = (IPEndPoint) endPoint;

            ServerDatagramTransport serverDatagramTransport;
            if (!_dtlsClients.TryGetValue(ipEndPoint, out var dtlsServerClient)) {
                Logger.Debug("Received data on server socket from unknown IP");

                serverDatagramTransport = _currentDatagramTransport;
                // Set the IP endpoint of the datagram transport instance so it can send data to the correct IP
                serverDatagramTransport.IPEndPoint = ipEndPoint;
            } else {
                serverDatagramTransport = dtlsServerClient.DatagramTransport;
            }

            try {
                serverDatagramTransport.ReceivedDataCollection.Add(new ServerDatagramTransport.ReceivedData {
                    Buffer = buffer,
                    Length = numReceived
                }, cancellationToken);
            } catch (OperationCanceledException) {
                break;
            }
        }
    }

    private void AcceptLoop(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            Logger.Debug("Creating new ServerDatagramTransport for handling new connection");
            
            _currentDatagramTransport = new ServerDatagramTransport(_socket);

            DtlsTransport dtlsTransport;
            try {
                dtlsTransport = _serverProtocol.Accept(_tlsServer, _currentDatagramTransport);
            } catch (IOException e) {
                Logger.Error($"IOException while accepting DTLS connection:\n{e}");
                break;
            }

            var endPoint = _currentDatagramTransport.IPEndPoint;

            Logger.Debug($"Accepted DTLS connection on socket, endpoint: {endPoint}");

            if (_dtlsClients.ContainsKey(endPoint)) {
                Logger.Error($"DtlsClient with endpoint ({endPoint}) already exists, cannot add");
                continue;
            }
            
            var dtlsServerClient = new DtlsServerClient {
                DtlsTransport = dtlsTransport,
                DatagramTransport = _currentDatagramTransport,
                EndPoint = endPoint,
                ReceiveLoopTokenSource = new CancellationTokenSource()
            };
            
            _dtlsClients.Add(endPoint, dtlsServerClient);
            
            Logger.Debug("Starting receive loop for client");
            new Thread(() => ClientReceiveLoop(
                dtlsServerClient.ReceiveLoopTokenSource.Token, 
                dtlsServerClient)
            ).Start();
        }

        _currentDatagramTransport?.Close();
        _currentDatagramTransport = null;

        _serverProtocol = null;

        foreach (var dtlsServerClient in _dtlsClients.Values) {
            dtlsServerClient.DtlsTransport?.Close();
            dtlsServerClient.DatagramTransport?.Close();
        }
        
        _dtlsClients.Clear();
    }

    private void ClientReceiveLoop(CancellationToken cancellationToken, DtlsServerClient dtlsServerClient) {
        var dtlsTransport = dtlsServerClient.DtlsTransport;
        
        while (!cancellationToken.IsCancellationRequested) {
            var buffer = new byte[dtlsTransport.GetReceiveLimit()];

            var numReceived = dtlsTransport.Receive(buffer, 0, dtlsTransport.GetReceiveLimit(), 5);
            
            DataReceivedEvent?.Invoke(dtlsServerClient, buffer, numReceived);
        }
    }
}
