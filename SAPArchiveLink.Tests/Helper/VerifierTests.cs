using System.Security.Cryptography.X509Certificates;
using Moq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Reflection;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class VerifierTests
    {
        private Verifier _verifier;
        private Mock<IArchiveCertificate> _mockCert;
        private X509Certificate2 _testCert;
        private byte[] _signedData;
        private readonly List<X509Certificate2> _certsToDispose = new();

        [SetUp]
        public void SetUp()
        {
            _verifier = new Verifier();
            _mockCert = new Mock<IArchiveCertificate>();
            _testCert = CreateSelfSignedTestCertificate();
            _mockCert.Setup(c => c.GetCertificate()).Returns(_testCert);
            _mockCert.Setup(c => c.GetPermission()).Returns(0xFF);
            _signedData = new byte[] { 1, 2, 3, 4 };
        }

        [Test]
        public void SetCertificate_ShouldStoreCertificate()
        {
            _verifier.SetCertificate(_mockCert.Object);
            Assert.That(_verifier.GetCertificate(0), Is.Not.Null);
        }

        [Test]
        public void SetSignedData_ShouldThrowOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => _verifier.SetSignedData(null));
        }

        [Test]
        public void SetSignedData_ShouldStoreData()
        {
            Assert.DoesNotThrow(() => _verifier.SetSignedData(_signedData));
        }

        [Test]
        public void SetRequiredPermission_ShouldStorePermission()
        {
            Assert.DoesNotThrow(() => _verifier.SetRequiredPermission(0x10));
        }

        [Test]
        public void VerifyAgainst_ShouldThrowIfSignedDataNotSet()
        {
            Assert.Throws<InvalidOperationException>(() => _verifier.VerifyAgainst(new byte[] { 1, 2 }));
        }

        [Test]
        public void GetCertificate_ShouldReturnVerifiedCertificate()
        {
            _verifier.SetCertificate(_mockCert.Object);
            _verifier.SetSignedData(_signedData);
            Assert.Throws<CryptographicException>(() => _verifier.VerifyAgainst(new byte[] { 5, 6 }));
            _verifier.SetCertificate(_mockCert.Object);
            Assert.That(_verifier.GetCertificate(0), Is.Not.Null);
        }

        [Test]
        public void VerifyAgainst_ShouldSucceedWithValidCertificateAndPermission()
        {
            var dataToSign = new byte[] { 10, 20, 30 };
            var (signedData, cert) = SignData(dataToSign);

            _mockCert.Setup(c => c.GetCertificate()).Returns(cert);
            _mockCert.Setup(c => c.GetPermission()).Returns(0xFF);

            _verifier.SetCertificate(_mockCert.Object);
            _verifier.SetSignedData(signedData);
            _verifier.SetRequiredPermission(0x01);

            Assert.DoesNotThrow(() => _verifier.VerifyAgainst(dataToSign));
        }

        [Test]
        public void VerifyAgainst_ShouldThrowWhenMalformedSignedData()
        {
            var malformedSignedData = new byte[] { 0x00, 0x01, 0x02 };

            _verifier.SetSignedData(malformedSignedData);

            var ex = Assert.Throws<CryptographicException>(() => _verifier.VerifyAgainst(new byte[] { 1, 2, 3 }));
            Assert.That(ex.Message, Does.Contain("Both PKCS#7 and X.509 verification failed")
                .Or.Contain("CMS") // Depending on your error message
            );
        }

        [Test]
        public void VerifyAgainst_ShouldThrowWhenNoMatchingCertificate()
        {
            var (signedData, certUsedToSign) = SignData(new byte[] { 1, 2, 3 });
            var otherCert = CreateSelfSignedTestCertificate();

            _mockCert.Setup(c => c.GetCertificate()).Returns(otherCert);

            _verifier.SetCertificate(_mockCert.Object);
            _verifier.SetSignedData(signedData);

            var ex = Assert.Throws<CryptographicException>(() => _verifier.VerifyAgainst(new byte[] { 1, 2, 3 }));
            Assert.That(ex.Message, Does.Contain("verification failed"));
        }

        [Test]
        public void VerifyAgainst_ShouldThrowWhenPermissionDenied()
        {
            var (signedData, cert) = SignData(new byte[] { 1, 2, 3 });

            _mockCert.Setup(c => c.GetCertificate()).Returns(cert);
            _mockCert.Setup(c => c.GetPermission()).Returns(0x00);

            _verifier.SetCertificate(_mockCert.Object);
            _verifier.SetSignedData(signedData);
            _verifier.SetRequiredPermission(0x10);

            var ex = Assert.Throws<CryptographicException>(() => _verifier.VerifyAgainst(new byte[] { 1, 2, 3 }));
            Assert.That(ex.Message, Does.Contain("verification failed"));
        }

        [Test]
        public void VerifyAgainst_ShouldThrowWhenFallbackVerificationFails()
        {
            var invalidSignedData = new byte[] { 0x00, 0x01, 0x02 };

            _verifier.SetCertificate(_mockCert.Object);
            _verifier.SetSignedData(invalidSignedData);

            var ex = Assert.Throws<CryptographicException>(() => _verifier.VerifyAgainst(new byte[] { 1, 2, 3 }));
            Assert.That(ex.Message, Does.Contain("verification failed"));
        }

        [Test]
        public void VerifyAgainst_ShouldThrowWhenFallbackCertThumbprintMismatch()
        {
            var (signedData, cert) = SignData(new byte[] { 1, 2, 3 });
            var differentCert = CreateSelfSignedTestCertificate();

            _mockCert.Setup(c => c.GetCertificate()).Returns(differentCert);

            _verifier.SetCertificate(_mockCert.Object);
            _verifier.SetSignedData(signedData);

            var ex = Assert.Throws<CryptographicException>(() => _verifier.VerifyAgainst(new byte[] { 1, 2, 3 }));
            Assert.That(ex.Message, Does.Contain("verification failed"));
        }

        [Test]
        public void VerifyAgainst_FallbackCertMismatch_ThrowsSecurityException()
        {
            var fallbackCert = CreateSelfSignedTestCertificate();
            var configuredCert = CreateSelfSignedTestCertificate();

            _verifier.SetSignedData(fallbackCert.RawData);

            var mockCert = new Mock<IArchiveCertificate>();
            mockCert.Setup(c => c.GetCertificate()).Returns(configuredCert);
            mockCert.Setup(c => c.GetPermission()).Returns(0xFF);

            _verifier.SetCertificate(mockCert.Object);
            _verifier.SetRequiredPermission(1);

            var ex = Assert.Throws<CryptographicException>(() =>
                _verifier.VerifyAgainst(new byte[] { 1, 2, 3 }));

            Assert.That(ex.Message, Does.Contain("verification failed"));
        }

        [Test]
        public void VerifyAgainst_IssuerSerialMatch_Succeeds()
        {
            var signerCert = CreateSelfSignedTestCertificate();

            var cms = new SignedCms(new ContentInfo(new byte[] { 1, 2, 3 }), detached: true);
            cms.ComputeSignature(new CmsSigner(signerCert));
            cms.Encode();

            var clonedCert = new X509Certificate2(signerCert.RawData);

            var mockCert = new Mock<IArchiveCertificate>();
            mockCert.Setup(c => c.GetCertificate()).Returns(clonedCert);
            mockCert.Setup(c => c.GetPermission()).Returns(0xFF);

            _verifier.SetCertificate(mockCert.Object);
            _verifier.SetSignedData(cms.Encode());
            _verifier.SetRequiredPermission(1);

            Assert.DoesNotThrow(() => _verifier.VerifyAgainst(new byte[] { 1, 2, 3 }));

            Assert.That(_verifier.GetCertificate(), Is.Not.Null);
            Assert.That(_verifier.GetCertificate().Thumbprint, Is.EqualTo(clonedCert.Thumbprint));
        }


        [Test]
        public void GetCertificate_ShouldReturnNullForInvalidIndex()
        {
            _verifier.SetCertificate(_mockCert.Object);
            var result = _verifier.GetCertificate(10);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetCertificate_ShouldReturnVerifiedCertificateForDefaultIndex()
        {
            var data = new byte[] { 1, 2, 3 };
            var (signedData, cert) = SignData(data);

            _mockCert.Setup(c => c.GetCertificate()).Returns(cert);
            _mockCert.Setup(c => c.GetPermission()).Returns(0xFF);

            _verifier.SetCertificate(_mockCert.Object);
            _verifier.SetSignedData(signedData);
            _verifier.SetRequiredPermission(0x01);
            _verifier.VerifyAgainst(data);

            var result = _verifier.GetCertificate();

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Thumbprint, Is.EqualTo(cert.Thumbprint));
            });
        }

        [Test]
        public void VerifyAgainst_ShouldSkipPermissionCheckWhenCertificateIsNull()
        {
            var (signedData, cert) = SignData(new byte[] { 1, 2, 3 });
            _verifier.SetSignedData(signedData);

            var field = typeof(Verifier).GetField("_rawCertificates", BindingFlags.NonPublic | BindingFlags.Instance);
            var collection = new X509Certificate2Collection(cert);
            field.SetValue(_verifier, collection);

            _verifier.SetRequiredPermission(0x01); // Permission set, but _certificate is null

            Assert.DoesNotThrow(() => _verifier.VerifyAgainst(new byte[] { 1, 2, 3 }));
        }


        [Test]
        public void VerifyAgainst_ShouldMatchCertificateByThumbprint()
        {
            var (signedData, cert) = SignData(new byte[] { 1, 2, 3 });

            _mockCert.Setup(c => c.GetCertificate()).Returns(cert);
            _mockCert.Setup(c => c.GetPermission()).Returns(0xFF);

            _verifier.SetCertificate(_mockCert.Object);
            _verifier.SetSignedData(signedData);
            _verifier.SetRequiredPermission(0x01);

            Assert.DoesNotThrow(() => _verifier.VerifyAgainst(new byte[] { 1, 2, 3 }));
            Assert.That(_verifier.GetCertificate().Thumbprint, Is.EqualTo(cert.Thumbprint));
        }

        [TearDown]
        public void Dispose()
        {
            foreach (var cert in _certsToDispose)
                cert.Dispose();

            _testCert.Dispose();
        }

        private X509Certificate2 CreateSelfSignedTestCertificate()
        {
            var ecdsa = ECDsa.Create();
            var req = new CertificateRequest("CN=TestCert", ecdsa, HashAlgorithmName.SHA256);
            var cert = req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(1));
            _certsToDispose.Add(cert);
            return cert;
        }

        private (byte[] signedData, X509Certificate2 cert) SignData(byte[] data)
        {
            var cert = CreateSelfSignedTestCertificate();
            var cms = new SignedCms(new ContentInfo(data), detached: true);
            var signer = new CmsSigner(cert);
            cms.ComputeSignature(signer);
            return (cms.Encode(), cert);
        }
    }
}
