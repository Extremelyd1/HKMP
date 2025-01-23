using System;
using System.IO;
using Hkmp.Logging;
using Hkmp.Util;
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
using Org.BouncyCastle.Utilities.IO.Pem;
using Org.BouncyCastle.X509;
using PemReader = Org.BouncyCastle.OpenSsl.PemReader;
using PemWriter = Org.BouncyCastle.OpenSsl.PemWriter;

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
    /// File name of the file that stores the private key.
    /// </summary>
    private const string KeyPairFileName = "key.pem";
    /// <summary>
    /// File name of the file that stores the certificate.
    /// </summary>
    private const string CertificateFileName = "cert.cer";

    /// <summary>
    /// Full file path of the file that stores the private key.
    /// </summary>
    private readonly string KeyPairFilePath;
    /// <summary>
    /// Full file path of the file that stores the certificate.
    /// </summary>
    private readonly string CertificateFilePath;

    /// <summary>
    /// Asymmetric key pair for the server. Used to create the server certificate.
    /// </summary>
    private readonly AsymmetricCipherKeyPair _keyPair;
    /// <summary>
    /// X509 certificate for the server. Reported to clients to validate they are connecting to the correct server.
    /// </summary>
    private readonly X509Certificate _certificate;

    public ServerTlsServer(TlsCrypto crypto) : base(crypto) {
        KeyPairFilePath = Path.Combine(FileUtil.GetCurrentPath(), KeyPairFileName);
        CertificateFilePath = Path.Combine(FileUtil.GetCurrentPath(), CertificateFileName);
        
        _keyPair = LoadOrGenerateECDHKeyPair();
        _certificate = LoadOrGenerateCertificate();
    }

    /// <summary>
    /// Loads the ECDH key pair from file if it exists, otherwise generates the key pair and stores it to a file.
    /// </summary>
    /// <returns>The loaded or generated asymmetric EC key pair.</returns>
    private AsymmetricCipherKeyPair LoadOrGenerateECDHKeyPair() {
        Logger.Info($"LoadOrGenerateECDHKeyPair: {KeyPairFilePath}");
        
        if (File.Exists(KeyPairFilePath)) {
            Logger.Info("KeyPair file exists, loading...");
            return LoadECDHKeyPair();
        }

        Logger.Info("KeyPair file does not exist, generating and storing...");
        var generatedKeyPair = GenerateECDHKeyPair(GetECBuiltInBinaryDomainParameters());
        WriteObjectAsPemToFile(KeyPairFilePath, generatedKeyPair);

        return generatedKeyPair;
    }

    /// <summary>
    /// Load the ECDH key pair from file.
    /// </summary>
    /// <returns>The loaded asymmetric EC key pair, or null if the file could not be found or read.</returns>
    private AsymmetricCipherKeyPair LoadECDHKeyPair() {
        string fileContents;
        try {
            fileContents = File.ReadAllText(KeyPairFilePath);
        } catch (Exception e) {
            Logger.Error($"Could not read PEM key file:\n{e}");
            return null;
        }

        var stringReader = new StringReader(fileContents);
        var pemReader = new PemReader(stringReader);

        try {
            return (AsymmetricCipherKeyPair) pemReader.ReadObject();
        } catch (Exception e) {
            Logger.Error($"Could not read PEM key file:\n{e}");
            return null;
        } finally {
            stringReader.Close();
            pemReader.Dispose();
        }
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

    /// <summary>
    /// Loads the X509 certificate from file if it exists, otherwise generates the certificate and stores it to a file.
    /// </summary>
    /// <returns>The loaded or generated certificate.</returns>
    private X509Certificate LoadOrGenerateCertificate() {
        Logger.Info($"LoadOrGenerateCertificate: {CertificateFilePath}");

        if (File.Exists(CertificateFilePath)) {
            Logger.Info("Certificate file exists, loading...");
            return LoadCertificate();
        }

        Logger.Info("Certificate does not exist, generating and storing...");
        var generatedCertificate = GenerateCertificate(
            new X509Name("CN=TestCA"),
            new X509Name("CN=TestEE"),
            _keyPair.Private,
            _keyPair.Public
        );
        WriteObjectAsPemToFile(CertificateFilePath, generatedCertificate);

        return generatedCertificate;
    }

    /// <summary>
    /// Load the X509 certificate from file.
    /// </summary>
    /// <returns>The loaded certificate, or null if the file could not be found or read.</returns>
    private X509Certificate LoadCertificate() {
        string fileContents;
        try {
            fileContents = File.ReadAllText(CertificateFilePath);
        } catch (Exception e) {
            Logger.Error($"Could not read certificate file:\n{e}");
            return null;
        }

        var stringReader = new StringReader(fileContents);
        var pemReader = new PemReader(stringReader);

        try {
            return (X509Certificate) pemReader.ReadObject();
        } catch (Exception e) {
            Logger.Error($"Could not read certificate file:\n{e}");
            return null;
        } finally {
            stringReader.Close();
            pemReader.Dispose();
        }
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
        certGenerator.SetNotAfter(DateTime.UtcNow.AddYears(1));
        certGenerator.SetNotBefore(DateTime.UtcNow);
        certGenerator.SetPublicKey(subjectPublic);
        return certGenerator.Generate(signatureFactory);
    }

    /// <summary>
    /// Write a given object to a file at the given file path. This uses a <seealso cref="PemWriter"/> and thus can
    /// only write certain objects:
    /// X509Certificate, X509Crl, AsymmetricCipherKeyPair, AsymmetricKeyParameter,
    /// IX509AttributeCertificate, Pkcs10CertificationRequest, Asn1.Cms.ContentInfo
    /// </summary>
    /// <param name="filePath">The full path of the file to write the object to.</param>
    /// <param name="obj">The object to write to file.</param>
    private static void WriteObjectAsPemToFile(string filePath, object obj) {
        var stringWriter = new StringWriter();
        var pemWriter = new PemWriter(stringWriter);

        try {
            pemWriter.WriteObject(obj);
        } catch (PemGenerationException e) {
            Logger.Error($"Could not write object to PEM file:\n{e}");
            return;
        }

        pemWriter.Writer.Flush();

        var contents = stringWriter.ToString();
        
        stringWriter.Close();

        try {
            File.WriteAllText(filePath, contents);
        } catch (Exception e) {
            Logger.Error($"Could not write object to PEM file:\n{e}");
        }
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
