using System.Text;
using Hkmp.Logging;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Utilities.Encoders;

namespace Hkmp.Networking.Client;

/// <summary>
/// Client-side TLS client implementation that handles reporting supported cipher suites, provides client
/// authentication and checks server certificate.
/// </summary>
/// <param name="crypto">TlsCrypto instance for handling low-level cryptography.</param>
internal class ClientTlsClient(TlsCrypto crypto) : AbstractTlsClient(crypto) {
    /// <summary>
    /// List of supported cipher suites on the client-side.
    /// </summary>
    private static readonly int[] SupportedCipherSuites = [
        CipherSuite.TLS_AES_128_GCM_SHA256,
        CipherSuite.TLS_AES_256_GCM_SHA384,
        CipherSuite.TLS_CHACHA20_POLY1305_SHA256,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256
    ];

    /// <inheritdoc />
    protected override ProtocolVersion[] GetSupportedVersions() {
        return ProtocolVersion.DTLSv12.Only();
    }

    /// <summary>
    /// Get the supported cipher suites for this TLS client.
    /// </summary>
    /// <returns>An int array representing the cipher suites.</returns>
    protected override int[] GetSupportedCipherSuites() {
        return SupportedCipherSuites;
    }

    /// <inheritdoc />
    /// <summary>
    /// Get the authentication implementation for this TLS client that handles providing client credentials and
    /// checking server certificates.
    /// </summary>
    /// <returns>The TlsAuthentication instance for this TLS client.</returns>
    public override TlsAuthentication GetAuthentication() {
        return new TlsAuthenticationImpl();
    }

    /// <summary>
    /// Implementation for TLS authentication that handles providing client credentials and checking server
    /// certificates.
    /// </summary>
    private class TlsAuthenticationImpl : TlsAuthentication {
        /// <summary>
        /// Notify the TLS client of the server certificate that the server has sent. This method checks whether to
        /// trust the server based on this certificate or not. If not, the method will throw an exception which will
        /// subsequently abort the connection.
        /// In the current implementation, we only log the fingerprints of the certificates in the chain from the
        /// server.
        /// </summary>
        /// <inheritdoc />
        /// <param name="serverCertificate">The server certificate instance.</param>
        public void NotifyServerCertificate(TlsServerCertificate serverCertificate) {
            if (serverCertificate?.Certificate == null || serverCertificate.Certificate.IsEmpty) {
                throw new TlsFatalAlert(AlertDescription.bad_certificate);
            }
            
            var chain = serverCertificate.Certificate.GetCertificateList();

            Logger.Info("Server certificate fingerprint(s):");
            for (var i = 0; i < chain.Length; i++) {
                var entry = X509CertificateStructure.GetInstance(chain[i].GetEncoded());
                Logger.Info($"  fingerprint:SHA256 {Fingerprint(entry)} ({entry.Subject})");
            }
        }

        /// <summary>
        /// Get the credentials of the client so the server can verify who we are. Currently, we have no way to
        /// provide client-side credentials, so we return null.
        /// </summary>
        /// <inheritdoc />
        public TlsCredentials GetClientCredentials(CertificateRequest certificateRequest) {
            // TODO: provide means for a client to have certificate and return it in this method
            return null;
        }

        /// <summary>
        /// Return a fingerprint for the given X509 certificate.
        /// </summary>
        /// <param name="c">The 509 certificate to fingerprint.</param>
        /// <returns>The fingerprint as a string.</returns>
        private static string Fingerprint(X509CertificateStructure c) {
            var der = c.GetEncoded();
            var hash = DigestUtilities.CalculateDigest("SHA256", der);
            var hexBytes = Hex.Encode(hash);
            var hex = Encoding.ASCII.GetString(hexBytes).ToUpperInvariant();

            var fp = new StringBuilder();
            var i = 0;
            fp.Append(hex.Substring(i, 2));
            while ((i += 2) < hex.Length) {
                fp.Append(':');
                fp.Append(hex.Substring(i, 2));
            }
            return fp.ToString();
        }
    }
}
