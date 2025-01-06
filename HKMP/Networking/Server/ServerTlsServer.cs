using System;
using Hkmp.Logging;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.Sec;
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

namespace Hkmp.Networking.Server;

internal class ServerTlsServer : AbstractTlsServer {
    private static readonly int[] SupportedCipherSuites = [
        CipherSuite.TLS_AES_128_GCM_SHA256,
        CipherSuite.TLS_AES_256_GCM_SHA384,
        CipherSuite.TLS_CHACHA20_POLY1305_SHA256,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
        // CipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256
    ];

    private readonly AsymmetricCipherKeyPair _keyPair;
    private readonly X509Certificate _certificate;

    public ServerTlsServer(TlsCrypto crypto) : base(crypto) {
        _keyPair = GenerateECDHKeyPair(GetECBuiltInBinaryDomainParameters());
        _certificate = GenerateCertificate(
            new X509Name("CN=TestCA"),
            new X509Name("CN=TestEE"),
            _keyPair.Private,
            _keyPair.Public
        );
    }

    static X509Certificate GenerateCertificate(
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

    private ECDomainParameters GetECBuiltInBinaryDomainParameters() {
        var ecParams = ECNamedCurveTable.GetByName("K-283");

        return new ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H, ecParams.GetSeed());
    }

    private AsymmetricCipherKeyPair GenerateECDHKeyPair(ECDomainParameters ecParams) {
        var ecKeyGenParams = new ECKeyGenerationParameters(ecParams, new SecureRandom());
        var ecKeyPairGen = new ECKeyPairGenerator();
        ecKeyPairGen.Init(ecKeyGenParams);
        var ecKeyPair = ecKeyPairGen.GenerateKeyPair();

        return ecKeyPair;
    }
    
    

    protected override ProtocolVersion[] GetSupportedVersions() {
        return ProtocolVersion.DTLSv12.Only();
    }
    
    protected override int[] GetSupportedCipherSuites() {
        return SupportedCipherSuites;
    }

    public override TlsCredentials GetCredentials() {
        var keyExchangeAlgorithm = TlsUtilities.GetKeyExchangeAlgorithm(m_selectedCipherSuite);
        Logger.Debug($"keyExchangeAlgorithm: {keyExchangeAlgorithm}");

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
