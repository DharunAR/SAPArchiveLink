using Microsoft.Extensions.Options;
using Moq;
using TRIM.SDK;

namespace SAPArchiveLink.Tests
{
    public class CMArchieveLinkClientTest
    {
        private Mock<IDatabaseConnection> _dbConnectionMock;
        private IOptions<TrimConfigSettings> _trimConfig;
        private CMArchieveLinkClient _client;
        private Mock<ILogHelper<BaseServices>> _helperLoggerMock;
        private Mock<ICommandResponseFactory> _commandResponseFactoryMock;       

        [SetUp]
        public void Setup()
        {
            _dbConnectionMock = new Mock<IDatabaseConnection>();
            _trimConfig = Options.Create(new MockTrimConfigProvider().GetTrimConfig());
            _helperLoggerMock = new Mock<ILogHelper<BaseServices>>();
            _commandResponseFactoryMock = new Mock<ICommandResponseFactory>();

            TrimApplication.TrimBinariesLoadPath = _trimConfig.Value.BinariesLoadPath;
            _client = new CMArchieveLinkClient(
                _trimConfig,
                _dbConnectionMock.Object,
                _helperLoggerMock.Object,
                _commandResponseFactoryMock.Object
            );
        }        

        [Test]
        public void IsRecordComponentAvailable_ReturnsTrue_WhenComponentExists()
        {
            var compId = "comp1";
            var component = new Mock<RecordSapComponent>();
            component.SetupGet(c => c.ComponentId).Returns(compId);
            var components = new List<RecordSapComponent> { component.Object };
            var recordSapComponents = Mock.Of<RecordSapComponents>(c => c.GetEnumerator().MoveNext() == components.GetEnumerator().MoveNext());

            var result = _client.IsRecordComponentAvailable(recordSapComponents, compId);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsRecordComponentAvailable_ReturnsFalse_WhenComponentDoesNotExist()
        {
            var compId = "comp1";
            var component = new Mock<RecordSapComponent>();
            component.SetupGet(c => c.ComponentId).Returns("other");
            var components = new List<RecordSapComponent> { component.Object };
            var recordSapComponents = Mock.Of<RecordSapComponents>(c => c.GetEnumerator().MoveNext() == components.GetEnumerator().MoveNext());

            var result = _client.IsRecordComponentAvailable(recordSapComponents, compId);

            Assert.That(result, Is.False);
        }

        [Test]
        public void GetDatabase_ReturnsDatabaseFromConnection()
        {
            var db = new Mock<Database>().Object;
            _dbConnectionMock.Setup(x => x.GetDatabase()).Returns(db);

            var result = _client.GetDatabase();

            Assert.That(result, Is.EqualTo(db));
        }

        [Test]
        public async Task PutArchiveCertificate_ValidCertificate_CallsArchiveCertificateMethods()
        {
            // Arrange
            var authId = "testAuth";
            int protectionLevel = 1;
            byte[] certificateBytes = new byte[] { 1, 2, 3, 4 };
            string contRep = "REP1";

            var archiveCertificateMock = new Mock<ArchiveCertificate>();
            archiveCertificateMock.Setup(a => a.getSerialNumber()).Returns("serial123");
            archiveCertificateMock.Setup(a => a.GetFingerprint()).Returns("fingerprint123");
            archiveCertificateMock.Setup(a => a.getIssuerName()).Returns("issuer123");
            archiveCertificateMock.Setup(a => a.ValidFrom()).Returns("2024-01-01");
            archiveCertificateMock.Setup(a => a.ValidTill()).Returns("2025-01-01");

            // Replace the problematic line with a factory method
           // Func<byte[], ArchiveCertificate> mockFactory = _ => archiveCertificateMock.Object;

            // Act
            await _client.PutArchiveCertificate(authId, protectionLevel, certificateBytes, contRep);

            // Assert
            archiveCertificateMock.Verify(a => a.getSerialNumber(), Times.Once);
            archiveCertificateMock.Verify(a => a.GetFingerprint(), Times.Once);
            archiveCertificateMock.Verify(a => a.getIssuerName(), Times.Once);
            archiveCertificateMock.Verify(a => a.ValidFrom(), Times.Once);
            archiveCertificateMock.Verify(a => a.ValidTill(), Times.Once);
        }

        // Helper to allow mocking static method for test

    }
}
