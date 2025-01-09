using System.Net;
using System.Threading;
using Org.BouncyCastle.Tls;

namespace Hkmp.Networking.Server;

internal class DtlsServerClient {
    public DtlsTransport DtlsTransport { get; init; }
    public ServerDatagramTransport DatagramTransport { get; init; }
    public IPEndPoint EndPoint { get; init; }
    
    public CancellationTokenSource ReceiveLoopTokenSource { get; init; }
}
