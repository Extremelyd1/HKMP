using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;

namespace Hkmp.Networking.Client;

internal class ClientTlsClient(TlsCrypto crypto) : AbstractTlsClient(crypto) {
    private static readonly int[] SupportedCipherSuites = [
        CipherSuite.TLS_AES_128_GCM_SHA256,
        CipherSuite.TLS_AES_256_GCM_SHA384,
        CipherSuite.TLS_CHACHA20_POLY1305_SHA256,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256
    ];
    
    protected override ProtocolVersion[] GetSupportedVersions() {
        return ProtocolVersion.DTLSv12.Only();
    }

    protected override int[] GetSupportedCipherSuites() {
        return SupportedCipherSuites;
    }

    public override TlsAuthentication GetAuthentication() {
        return new TlsAuthenticationImpl();
    }

    private class TlsAuthenticationImpl : TlsAuthentication {
        public void NotifyServerCertificate(TlsServerCertificate serverCertificate) {
            // TODO: check server certificate, throw error in case of invalid cert
        }

        public TlsCredentials GetClientCredentials(CertificateRequest certificateRequest) {
            // TODO: provide means for a client to have certificate and return it in this method
            return null;
        }
    }
}
