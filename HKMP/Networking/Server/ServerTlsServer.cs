using System;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using Org.BouncyCastle.X509;
// ReSharper disable InconsistentNaming

namespace Hkmp.Networking.Server;

/// <summary>
/// Server-side TLS client implementation that handles reporting supported cipher suites and provides the server's
/// certificate.
/// </summary>
internal class ServerTlsServer : AbstractTlsServer {
    /// <summary>
    /// List of supported cipher suites on the server-side.
    /// </summary>
    private static readonly int[] SupportedCipherSuites = [
        CipherSuite.TLS_AES_128_GCM_SHA256,
        CipherSuite.TLS_AES_256_GCM_SHA384,
        CipherSuite.TLS_CHACHA20_POLY1305_SHA256,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256
    ];

    /// <summary>
    /// Asymmetric key pair for the server. Used to create the server certificate.
    /// </summary>
    private readonly AsymmetricCipherKeyPair _keyPair;
    /// <summary>
    /// X509 certificate for the server. Reported to clients to validate they are connecting to the correct server.
    /// </summary>
    private readonly X509Certificate _certificate;

    // TODO: use existing certificate on disk if available and store generated certificate to disk if a new one was generated
    public ServerTlsServer(TlsCrypto crypto) : base(crypto) {
        _keyPair = GenerateECDHKeyPair(GetECBuiltInBinaryDomainParameters());
        _certificate = GenerateCertificate(
            new X509Name("CN=TestCA"),
            new X509Name("CN=TestEE"),
            _keyPair.Private,
            _keyPair.Public
        );
    }

    /// <summary>
    /// Generate a X509 certificate with the given issuer and subject names, and with the given keys.
    /// </summary>
    /// <param name="issuer">The issuer name.</param>
    /// <param name="subject">The subject name.</param>
    /// <param name="issuerPrivate">The issuer private key.</param>
    /// <param name="subjectPublic">The subject public key.</param>
    /// <returns>The generated X509 certificate.</returns>
    private static X509Certificate GenerateCertificate(
        X509Name issuer, X509Name subject,
        AsymmetricKeyParameter issuerPrivate,
        AsymmetricKeyParameter subjectPublic
    ) {
        ISignatureFactory signatureFactory;
        if (issuerPrivate is ECPrivateKeyParameters) {
            signatureFactory = new Asn1SignatureFactory(
                X9ObjectIdentifiers.ECDsaWithSha256.ToString(),
                issuerPrivate);
        } else {
            signatureFactory = new Asn1SignatureFactory(
                PkcsObjectIdentifiers.Sha256WithRsaEncryption.ToString(),
                issuerPrivate);
        }

        var certGenerator = new X509V3CertificateGenerator();
        certGenerator.SetIssuerDN(issuer);
        certGenerator.SetSubjectDN(subject);
        certGenerator.SetSerialNumber(BigInteger.ValueOf(1));
        certGenerator.SetNotAfter(DateTime.UtcNow.AddHours(1));
        certGenerator.SetNotBefore(DateTime.UtcNow);
        certGenerator.SetPublicKey(subjectPublic);
        return certGenerator.Generate(signatureFactory);
    }

    /// <summary>
    /// Get built-in binary domain parameters for elliptic curve crypto using curve "K-283".
    /// </summary>
    /// <returns>The domain parameters corresponding to "K-283".</returns>
    private static ECDomainParameters GetECBuiltInBinaryDomainParameters() {
        var ecParams = ECNamedCurveTable.GetByName("K-283");

        return new ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H, ecParams.GetSeed());
    }

    /// <summary>
    /// Generate an asymmetric key pair using the given domain parameters.
    /// </summary>
    /// <param name="ecParams">The domain parameters for the generation.</param>
    /// <returns>The asymmetric key pair.</returns>
    private static AsymmetricCipherKeyPair GenerateECDHKeyPair(ECDomainParameters ecParams) {
        var ecKeyGenParams = new ECKeyGenerationParameters(ecParams, new SecureRandom());
        var ecKeyPairGen = new ECKeyPairGenerator();
        ecKeyPairGen.Init(ecKeyGenParams);
        var ecKeyPair = ecKeyPairGen.GenerateKeyPair();

        return ecKeyPair;
    }

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

    /// <summary>
    /// Get the server credentials for sending to clients to authenticate the server.
    /// </summary>
    /// <returns>TlsCredentials instance representing the credentials.</returns>
    /// <exception cref="TlsFatalAlert">Thrown when the key exchange algorithm agreed upon by the server and client
    /// does not match the expected, and we can thus not send our credentials.</exception>
    public override TlsCredentials GetCredentials() {
        var keyExchangeAlgorithm = TlsUtilities.GetKeyExchangeAlgorithm(m_selectedCipherSuite);

        if (keyExchangeAlgorithm != KeyExchangeAlgorithm.ECDHE_ECDSA) {
            throw new TlsFatalAlert(AlertDescription.internal_error);
        }

        var bcTlsCrypto = new BcTlsCrypto(new SecureRandom());

        return new DefaultTlsCredentialedSigner(
            new TlsCryptoParameters(m_context), 
            new BcTlsECDsaSigner(
                bcTlsCrypto,
                (ECPrivateKeyParameters) _keyPair.Private
            ), 
            new Certificate([new BcTlsCertificate(bcTlsCrypto, _certificate.CertificateStructure)]), 
            SignatureAndHashAlgorithm.GetInstance(
                HashAlgorithm.sha384,
                SignatureAlgorithm.ecdsa
            )
        );
    }
}
