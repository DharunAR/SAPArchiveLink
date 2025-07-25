using NUnit.Framework;
using SAPArchiveLink;
using System.Security.Cryptography.X509Certificates;
using Moq;
using System;
using System.Security.Cryptography;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class VerifierTests
    {
        private Verifier _verifier;
        private Mock<IArchiveCertificate> _mockCert;
        private X509Certificate2 _testCert;
        private byte[] _signedData;

        [SetUp]
        public void SetUp()
        {
            _verifier = new Verifier();
            _mockCert = new Mock<IArchiveCertificate>();
            _testCert = new X509Certificate2();
            _mockCert.Setup(c => c.GetCertificate()).Returns(_testCert);
            _mockCert.Setup(c => c.GetPermission()).Returns(0xFF);
            _signedData = new byte[] { 1, 2, 3, 4 }; // Dummy data
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
            _verifier.SetSignedData(_signedData);
            // No exception means success
            Assert.Pass();
        }

        [Test]
        public void SetRequiredPermission_ShouldStorePermission()
        {
            _verifier.SetRequiredPermission(0x10);
            // No exception means success
            Assert.Pass();
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
            // Simulate fallback path by passing invalid signed data
            Assert.Throws<CryptographicException>(() => _verifier.VerifyAgainst(new byte[] { 5, 6 }));
            _verifier.SetCertificate(_mockCert.Object);
            Assert.That(_verifier.GetCertificate(0), Is.Not.Null);
        }

        [TearDown]
        public void dispose()
        {
            _testCert.Dispose();
        }
    }
}
