using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.QualityTools.Testing.Fakes;
using Moq;
using TRIM.SDK;
using TRIM.SDK.Fakes;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class DatabaseConnectionTests
    {
        private IDisposable _shimContext;
        private Mock<IOptionsMonitor<TrimConfigSettings>> _mockTrimConfig;
        private Mock<ILoggerFactory> _mockLoggerFactory;
        private DatabaseConnection? _databaseConnection;

        [SetUp]
        public void Setup()
        {
            _shimContext = ShimsContext.Create();
            _mockTrimConfig = new Mock<IOptionsMonitor<TrimConfigSettings>>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            SetupShimDatabaseDefaults();
        }

        [Test]
        public void ConnectDatabase_ReturnsTrimRepository()
        {
            var configSettings = new TrimConfigSettings() { WorkPath = "C:\\Temp", DatabaseId = "P1", WGSName = "local" };
            _mockTrimConfig.Setup(c => c.CurrentValue).Returns(configSettings);
            ShimDatabase.AllInstances.IsValidGet = (db) => { return true; };
            _databaseConnection = new DatabaseConnection(_mockTrimConfig.Object, _mockLoggerFactory.Object);
            var databaseConnection = _databaseConnection.GetDatabase();
            Assert.That(databaseConnection, Is.Not.Null);
        }

        [Test]
        public void ConnectDatabase_WithClientIdAndSecret_WithoutTrustedUser_ThrowsException()
        {
            string exMessage = string.Empty;
            _mockTrimConfig.Setup(c => c.CurrentValue).Returns(new TrimConfigSettings()
            {
                ClientId = "testClientId",
                ClientSecret = "testSecret",
                WorkPath = "C:\\Temp",
                DatabaseId = "P1",
                WGSName = "local"
            });
            ShimDatabase.AllInstances.IsValidGet = (db) => { return true; };
            ShimDatabase.AllInstances.SetAuthenticationCredentialsStringString = (db, clientId, clientSecret) => { };
            ShimTrimException.ConstructorString = (ex, value) =>
            {
                exMessage = value;
            };
            _databaseConnection = new DatabaseConnection(_mockTrimConfig.Object, _mockLoggerFactory.Object);

            Assert.Multiple(() =>
            {
                Assert.Throws<TrimException>(() => _databaseConnection.GetDatabase());
                Assert.That(exMessage, Is.EqualTo("TrustedUser must be set when using ClientId and ClientSecret for authentication."));
            });

        }

        [Test]
        public void ConnectDatabase_WithValidClientIDandSecret_ReturnsTrimRepository()
        {
            _mockTrimConfig.Setup(c => c.CurrentValue).Returns(new TrimConfigSettings()
            {
                ClientId = "testClientId",
                ClientSecret = "testSecret",
                WorkPath = "C:\\Temp",
                DatabaseId = "P1",
                WGSName = "local",
                TrustedUser = "TRIMSERVICES"
            });
            ShimDatabase.AllInstances.IsValidGet = (db) => { return true; };
            ShimDatabase.AllInstances.SetAuthenticationCredentialsStringString = (db, clientId, clientSecret) => { };
            _databaseConnection = new DatabaseConnection(_mockTrimConfig.Object, _mockLoggerFactory.Object);
            var databaseConnection = _databaseConnection.GetDatabase();
            Assert.That(databaseConnection, Is.Not.Null);
            Assert.That(databaseConnection, Is.InstanceOf<ITrimRepository>());
        }

        [Test]
        public void GetDatabase_WithAlternateWGSName_ReturnsTrimRepository()
        {
            _mockTrimConfig.Setup(c => c.CurrentValue).Returns(new TrimConfigSettings()
            {
                WorkPath = "C:\\Temp",
                DatabaseId = "P1",
                WGSName = "local",
                WGSAlternateName = "alternate",
                WGSAlternatePort = 1234
            });
            ShimDatabase.AllInstances.IsValidGet = (db) => { return true; };
            ShimDatabase.AllInstances.AlternateWorkgroupServerNameSetString = (db, value) => { };
            ShimDatabase.AllInstances.AlternateWorkgroupServerPortSetInt32 = (db, value) => { };
            _databaseConnection = new DatabaseConnection(_mockTrimConfig.Object, _mockLoggerFactory.Object);
            var databaseConnection = _databaseConnection.GetDatabase();
            Assert.That(databaseConnection, Is.Not.Null);
        }

        private void SetupShimDatabaseDefaults()
        {

            ShimDatabase.Constructor = (db) =>
            {
                ShimDatabase.AllInstances.IdGet = (value) => { return "P1"; };
                ShimDatabase.AllInstances.WorkgroupServerNameGet = (value) => { return "local"; };
                ShimDatabase.AllInstances.WorkgroupServerPortGet = (value) => { return 1234; };
                ShimDatabase.AllInstances.TrustedUserGet = (value) => { return "TRIMSERVICES"; };
                ShimDatabase.AllInstances.IsSingleHopClientGet = (value) => { return true; };
            };

            ShimDatabase.AllInstances.AuthenticationMethodSetClientAuthenticationMechanism = (db, method) => {};
            ShimDatabase.AllInstances.Connect = (db) => { };
            ShimDatabase.AllInstances.IdSetString = (db, value) => { };
            ShimDatabase.AllInstances.WorkgroupServerNameSetString = (db, value) => { };
            ShimDatabase.AllInstances.WorkgroupServerPortSetInt32 = (db, value) => { };
            ShimDatabase.AllInstances.TrustedUserSetString = (db, value) => { };
            ShimDatabase.AllInstances.IsSingleHopClientSetBoolean = (db, value) => { };
            ShimDatabase.AllInstances.AuthenticationMethodSetClientAuthenticationMechanism = (db, value) => { };
        }


        [TearDown]
        public void TearDown()
        {
            _shimContext.Dispose();
        }
    }
}
