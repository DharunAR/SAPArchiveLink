using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SAPArchiveLink.Tests
{

    [TestFixture]
    public class ArchiveCertificateTests
    {
        private const string TestCertBase64 = "MIIDcTCCAlmgAwIBAgIUKbbGP3R9llY+HFnRNNiqdYlJOSgwDQYJKoZIhvcNAQELBQAwaDELMAkGA1UEBhMCVVMxEzARBgNVBAgMCkNhbGlmb3JuaWExFjAUBgNVBAcMDVNhbiBGcmFuY2lzY28xETAPBgNVBAoMCFRlc3QgT3JnMRkwFwYDVQQDDBB0ZXN0LmV4YW1wbGUuY29tMB4XDTI1MDcyMTA5NDUyMloXDTI2MDcyMTA5NDUyMlowaDELMAkGA1UEBhMCVVMxEzARBgNVBAgMCkNhbGlmb3JuaWExFjAUBgNVBAcMDVNhbiBGcmFuY2lzY28xETAPBgNVBAoMCFRlc3QgT3JnMRkwFwYDVQQDDBB0ZXN0LmV4YW1wbGUuY29tMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEApwrsPa1HGouOrNSSrqSfqWTSdOfSnDSHIQo0//PQRp49nE9kDFyX0kwZ69unLwYH3JtTFD9C/YgaJWBQotzbSOKnEklpCtLEAWoHbz8MoEiOYxauAETUXoD6OQxlDt6Cpki34xaUov4hwtUBqRpyo0JQl++6YXZHCnD1fCm2mjm6yMnyHe10k66enPGC61lNlFINUAAkB9hgkxzC4/mtEMD5JXpphVStc5+VoSqbp+ol2CXefApErHxZDgw1s8o8GvlCHo3C86WWo0EB5hrtIr5Sg62cLVyRLQ2p8jdGBsTylKLRCJffwLpWZuxhVlLX9DZUaLm9Lhd2ib6xvkQsDwIDAQABoxMwETAPBgNVHRMBAf8EBTADAQH/MA0GCSqGSIb3DQEBCwUAA4IBAQBMHXgg1Rb6H7UdSixzKbFrPBxqvLRq7XhBXTnZtzXd+PLkWa0QmLAROWkw9nUpcLlTJun3XfpmF9TidgPepPvushvi3cNh1ulJ6BVftMpflZD0ZBGMor/IthbFMEY8/TY5Q4hc5tzwa+tRVGuhOYT1wme0O3s0Eal3gS94t9HqIEd4zqZZHNxS7r7BCJAaCW1u0JAxqSM8g0SWLGPh9X6Jm3/eNsSEjkACH5Pi6mPY+CJyPQ7NIt4jYNLbj6vpVf/l+qahqOuil0aqB3skoS7hyPpy0Ik845P+2ePB5Uj9+ChZcFg7KfP/vFDTJBaqcBez9BwJJdKUu37dzqMJbKJE";
        private X509Certificate2 _testCert;
        private ArchiveCertificate _archiveCert;
        private CertificateFactory _certificateFactory;

        [SetUp]
        public void Setup()
        {
            var cert = new X509Certificate2(Convert.FromBase64String(TestCertBase64));
            _testCert = cert;
            _archiveCert = new ArchiveCertificate(_testCert, "test-auth-id", 5, true);
            _certificateFactory = new CertificateFactory();
        }

        [Test]
        public void CertificateFactory_ReturnsIArchiveCertificate()
        {
            var certBytes = _testCert.RawData;
            var archiveCert = _certificateFactory.FromByteArray(certBytes);
            Assert.That(archiveCert, Is.InstanceOf<IArchiveCertificate>());
        }

        [Test]
        public void Constructor_ValidCertificate_SetsProperties()
        {
            Assert.That(_archiveCert.GetCertificate(), Is.EqualTo(_testCert));
            Assert.That(_archiveCert.GetAuthId(), Is.EqualTo("test-auth-id"));
            Assert.That(_archiveCert.GetPermission(), Is.EqualTo(5));
            Assert.That(_archiveCert.IsEnabled(), Is.True);
        }

        [Test]
        public void GetFingerprint_ReturnsThumbprint()
        {
            Assert.That(_archiveCert.GetFingerprint(), Is.EqualTo(_testCert.Thumbprint));
        }

        [Test]
        public void ValidFromAndTill_ReturnsCorrectDates()
        {
            Assert.That(_archiveCert.ValidFrom(), Is.EqualTo(_testCert.NotBefore.ToString()));
            Assert.That(_archiveCert.ValidTill(), Is.EqualTo(_testCert.NotAfter.ToString()));
        }

        [Test]
        public void FromByteArray_InvalidData_ThrowsArgumentException()
        {
            var invalidData = Encoding.UTF8.GetBytes("not-a-certificate");
            Assert.Throws<ArgumentException>(() => ArchiveCertificate.FromByteArray(invalidData));
        }

        [Test]
        public void FromByteArray_ValidDER_ReturnsCertificate()
        {
            var certBytes = _testCert.RawData;
            var result = ArchiveCertificate.FromByteArray(certBytes);
            Assert.That(result.GetFingerprint(), Is.EqualTo(_testCert.Thumbprint));
            Assert.That(result.getSerialNumber(), Is.EqualTo(_testCert.SerialNumber));
            Assert.That(result.getIssuerName(), Is.EqualTo(_testCert.Issuer));
        }

        [Test]
        public void FromByteArray_WithValidPemCertificate_ReturnsArchiveCertificate()
        {
            string pem = @"
-----BEGIN CERTIFICATE-----
MIIDcTCCAlmgAwIBAgIUKbbGP3R9llY+HFnRNNiqdYlJOSgwDQYJKoZIhvcNAQEL
BQAwaDELMAkGA1UEBhMCVVMxEzARBgNVBAgMCkNhbGlmb3JuaWExFjAUBgNVBAcM
DVNhbiBGcmFuY2lzY28xETAPBgNVBAoMCFRlc3QgT3JnMRkwFwYDVQQDDBB0ZXN0
LmV4YW1wbGUuY29tMB4XDTI1MDcyMTA5NDUyMloXDTI2MDcyMTA5NDUyMlowaDEL
MAkGA1UEBhMCVVMxEzARBgNVBAgMCkNhbGlmb3JuaWExFjAUBgNVBAcMDVNhbiBG
cmFuY2lzY28xETAPBgNVBAoMCFRlc3QgT3JnMRkwFwYDVQQDDBB0ZXN0LmV4YW1w
bGUuY29tMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEApwrsPa1HGouO
rNSSrqSfqWTSdOfSnDSHIQo0//PQRp49nE9kDFyX0kwZ69unLwYH3JtTFD9C/Yga
JWBQotzbSOKnEklpCtLEAWoHbz8MoEiOYxauAETUXoD6OQxlDt6Cpki34xaUov4h
wtUBqRpyo0JQl++6YXZHCnD1fCm2mjm6yMnyHe10k66enPGC61lNlFINUAAkB9hg
kxzC4/mtEMD5JXpphVStc5+VoSqbp+ol2CXefApErHxZDgw1s8o8GvlCHo3C86WW
o0EB5hrtIr5Sg62cLVyRLQ2p8jdGBsTylKLRCJffwLpWZuxhVlLX9DZUaLm9Lhd2
ib6xvkQsDwIDAQABoxMwETAPBgNVHRMBAf8EBTADAQH/MA0GCSqGSIb3DQEBCwUA
A4IBAQBMHXgg1Rb6H7UdSixzKbFrPBxqvLRq7XhBXTnZtzXd+PLkWa0QmLAROWkw
9nUpcLlTJun3XfpmF9TidgPepPvushvi3cNh1ulJ6BVftMpflZD0ZBGMor/IthbF
MEY8/TY5Q4hc5tzwa+tRVGuhOYT1wme0O3s0Eal3gS94t9HqIEd4zqZZHNxS7r7B
CJAaCW1u0JAxqSM8g0SWLGPh9X6Jm3/eNsSEjkACH5Pi6mPY+CJyPQ7NIt4jYNLb
j6vpVf/l+qahqOuil0aqB3skoS7hyPpy0Ik845P+2ePB5Uj9+ChZcFg7KfP/vFDT
JBaqcBez9BwJJdKUu37dzqMJbKJE
-----END CERTIFICATE-----";

            byte[] pemBytes = Encoding.UTF8.GetBytes(pem);

            var result = ArchiveCertificate.FromByteArray(pemBytes);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<IArchiveCertificate>());
            Assert.That(result.GetCertificate().Subject, Does.Contain("test.example.com"));
        }

        [Test]
        public void FromByteArray_CompletelyInvalidPem_ThrowsArgumentException()
        {
            string invalidPem = "This is not a certificate or PEM format.";
            byte[] pemBytes = Encoding.UTF8.GetBytes(invalidPem);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ArchiveCertificate.FromByteArray(pemBytes));
        }

        [Test]
        public void FromByteArray_PemWithInvalidBase64_ThrowsFormatException()
        {
            string pem = @"
-----BEGIN CERTIFICATE-----
ThisIsNotBase64==
-----END CERTIFICATE-----";

            byte[] pemBytes = Encoding.UTF8.GetBytes(pem);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => ArchiveCertificate.FromByteArray(pemBytes));
            Assert.That(ex.InnerException, Is.TypeOf<FormatException>());
        }

        [Test]
        public void FromByteArray_PemMissingCertificate_ThrowsInvalidOperationException()
        {
            string pem = @"
-----BEGIN PRIVATE KEY-----
MIIBVwIBADANBgkqhkiG9w0BAQEFAASCAT8wggE7AgEAAkEAz0...
-----END PRIVATE KEY-----";

            byte[] pemBytes = Encoding.UTF8.GetBytes(pem);

            var ex = Assert.Throws<ArgumentException>(() => ArchiveCertificate.FromByteArray(pemBytes));
            Assert.That(ex.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(ex.InnerException.Message, Does.Contain("No CERTIFICATE section found"));
        }
    }

}
