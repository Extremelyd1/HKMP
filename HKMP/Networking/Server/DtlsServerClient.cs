using System.Net;
using Org.BouncyCastle.Tls;

namespace Hkmp.Networking.Server;

internal class DtlsServerClient {
    public DtlsTransport DtlsTransport { get; init; }
    public IPEndPoint EndPoint { get; init; }
}
