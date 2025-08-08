using Microsoft.Extensions.Logging;
using Microsoft.QualityTools.Testing.Fakes;
using Moq;
using TRIM.SDK;
using TRIM.SDK.Fakes;
namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class TrimRepositoryTests
    {
        private IDisposable _shimContext;
        private Mock<ILoggerFactory> _mockLoggerFactory;
        private Mock<TrimConfigSettings> _mockTrimConfig;
        private Database _fakeDatabase;
        private TrimRepository _trimRepository;

        [SetUp]
        public void Setup()
        {
            _shimContext = ShimsContext.Create();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockTrimConfig = new Mock<TrimConfigSettings>();

            // Setup fake database
            _fakeDatabase = new ShimDatabase
            {
                IsValidGet = () => true
            };

            _trimRepository = new TrimRepository(_fakeDatabase, _mockTrimConfig.Object, _mockLoggerFactory.Object);
        }

        [Test]
        public void GetRecord_ValidDocumentIdAndContentRepository_ReturnsArchiveRecord()
        {           
            string docId = "123";
            string contRep = "CM";
            ShimTrimMainObjectSearch.ConstructorDatabaseBaseObjectTypes = (search, db, type) =>
            {
                var shimSearch = new ShimTrimMainObjectSearch(search)
                {
                    CountGet = () => 1,
                    GetResultAsUriArray = () => new[] { new TrimURI(1) }
                };
            };

            ShimRecord.ConstructorDatabaseTrimURI = (record, db, uri) => { };
            ShimTrimSearchClause.ConstructorDatabaseBaseObjectTypesSearchClauseIds = (clause, db, type, id) => { };
        
            ShimTrimSearchClause.AllInstances.SetCriteriaFromStringString = (clause, criteria) => true;
            ShimTrimMainObjectSearch.AllInstances.AddSearchClauseTrimSearchClause =
    (instance, clause) => { };

            var shimmedTrimUri = new ShimTrimURI
            {
                UriAsStringGet = () => "1"
            };
       
            var shimmedTrimUriList = new ShimTrimURIList
            {
                CountGet = () => 1,
                ItemGetInt32 = (index) => shimmedTrimUri.Instance // important!
            };        
            ShimTrimMainObjectSearch.AllInstances.GetResultAsUriArrayInt64 = (instance, count) =>
            {
                return shimmedTrimUriList.Instance;
            };

            ShimRecord.ConstructorDatabaseTrimURI = (record, db, uri) => {
                
            };
            // Act
            var result = _trimRepository.GetRecord(docId, contRep);

         
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void SaveCounters_ShouldIncrementAndSaveCounters()
        {            
            using (ShimsContext.Create())
            {
                var fakeDb = new ShimDatabase(); 
                var archiveId = "ARCH123";
                var counter = new ArchiveCounter();
                typeof(ArchiveCounter).GetProperty("CreateCount")!
                    .SetValue(counter, 2);
                typeof(ArchiveCounter).GetProperty("DeleteCount")!
                    .SetValue(counter, 1);
                typeof(ArchiveCounter).GetProperty("UpdateCount")!
                    .SetValue(counter, 3);
                typeof(ArchiveCounter).GetProperty("ViewCount")!
                    .SetValue(counter, 4);

                bool incrementCalled = false;
                bool saveCalled = false;

                ShimSapRepoCounters.ConstructorDatabase = (instance, db) =>
                {
                    var shimCounters = new ShimSapRepoCounters(instance)
                    {
                        IncrementCountersApiSapRepoCounterItemList = (list) =>
                        {
                            incrementCalled = true;
                            saveCalled = true;
                        },                        
                    };
                };
                ShimTrimUserOptionSet.AllInstances.Save = (instance) =>
                {
                    saveCalled = true;
                };
                               
                Action<string> setArchiveDataID = (val) =>
                {
                    // Common test logic
                    Console.WriteLine("Set archive ID: " + val);
                };
     
                ShimSapRepoCounter.Constructor = (instance) =>
                {
                     new ShimSapRepoCounter(instance)
                    {
                        ArchiveDataIDGet = () => archiveId,
                         setArchiveDataIDString = (t) => { setArchiveDataID(archiveId); },
                         incrementCreateCounterInt64 = (val) => Assert.That(val, Is.EqualTo(counter.CreateCount)),
                        incrementDeleteCounterInt64 = (val) => Assert.That(val, Is.EqualTo(counter.DeleteCount)),
                        incrementUpdateCounterInt64 = (val) => Assert.That(val, Is.EqualTo(counter.UpdateCount)),
                        incrementViewCounterInt64 = (val) => Assert.That(val, Is.EqualTo(counter.ViewCount))
                    };
                };

                // Shim ApiSapRepoCounterItemList to accept Add
                ShimApiSapRepoCounterItemList.Constructor = (list) =>
                {
                    new ShimApiSapRepoCounterItemList(list)
                    {
                        AddSapRepoCounter = (c) =>
                        {
                            Assert.That(c, Is.Not.Null);
                        }
                    };
                };              
                _trimRepository.SaveCounters(archiveId, counter);
                Assert.That(saveCalled, "Save was not called.");
                Assert.That(incrementCalled, "IncrementCounters was not called.");                
            }
        }

        [Test]
        public void GetRecord_ReturnsNull()
        {
            string docId = "doc123";
            string contRep = "DS";
            ShimTrimMainObjectSearch.ConstructorDatabaseBaseObjectTypes = (search, db, type) =>
            {
                var shimSearch = new ShimTrimMainObjectSearch(search)
                {
                    CountGet = () => 0,
                };
            };
            ShimRecord.ConstructorDatabaseTrimURI = (record, db, uri) => { };
            ShimTrimSearchClause.ConstructorDatabaseBaseObjectTypesSearchClauseIds = (clause, db, type, id) => { };
            ShimTrimSearchClause.AllInstances.SetCriteriaFromStringString = (clause, criteria) => true;
            ShimTrimMainObjectSearch.AllInstances.AddSearchClauseTrimSearchClause =
    (instance, clause) => { };

            var result = _trimRepository.GetRecord(docId, contRep);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void IsProductFeatureActivated_ReturnsTrue()
        {
            ShimDatabase.AllInstances.IsProductFeatureActivatedProductFeatures = (feature, val) =>
            {
                return true;
            };
            Assert.That(_trimRepository.IsProductFeatureActivated(), Is.True);
        }

        [Test]
        public void IsProductFeatureActivated_ReturnsFalse()
        {
            ShimDatabase.AllInstances.IsProductFeatureActivatedProductFeatures = (feature, val) =>
            {
                return false;
            };
            Assert.That(_trimRepository.IsProductFeatureActivated(), Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void GetServerInfo_ReturnsServerInfoModel_WithContRep_WithEnabledAndDisabled(bool isEnabled)
        {
            SetupShimsForServerInfo(isEnabled);
            var serverModel = _trimRepository.GetServerInfo("0045", "DS");
            Assert.That(serverModel.ContentRepositories.Count, Is.EqualTo(1));

            var expectedStatus = isEnabled ? "Enabled" : "Disabled";
            Assert.That(serverModel.ContentRepositories[0].ContRepStatus, Is.EqualTo(expectedStatus));
        }

        [Test]
        public void GetServerInfo_ReturnsAllContentRepositories()
        {
            SetupShimsForServerInfo(true);
            var serverModel = _trimRepository.GetServerInfo("0045", "");
            Assert.That(serverModel.ContentRepositories.Count, Is.EqualTo(2));
            Assert.That(serverModel.PVersion, Is.EqualTo("0045"));
        }

        private void SetupShimsForServerInfo(bool isEnabled)
        {
            var fakeShim = new ShimTrimDateTime()
            {
                DateGet = () => DateTime.Now
            };
            var fakeComponents = new List<ShimSapRepoItem>()
            {
                new ShimSapRepoItem()
                {
                    ArchiveDataIDGet = () => "DS",
                    AuthIdGet = () => "authID",
                    ContentGet = () => "TestRepo",
                    ValidFromGet = () => fakeShim.Instance,
                    ValidTillGet = () => fakeShim.Instance,
                    FingerPrintGet = () =>"fingerPrint",
                    PermissionsGet = () => 1,
                    SerialNameGet = () => "serial",
                    IssuerCertificateGet = () => "Issuer"
                },
                new ShimSapRepoItem()
                {
                    ArchiveDataIDGet = () => "CM",
                    AuthIdGet = () => "authID",
                    ContentGet = () => "TestRepo",
                    ValidFromGet = () => fakeShim.Instance,
                    ValidTillGet = () => fakeShim.Instance,
                    FingerPrintGet = () =>"fingerPrint",
                    PermissionsGet = () => 1,
                    SerialNameGet = () => "serial",
                    IssuerCertificateGet = () => "Issuer"
                }
            };
            var sapRepos = new ShimApiSapRepoItemList();
            sapRepos.CountGet = () => 2;
            sapRepos.CapacityGet = () => 2;
            sapRepos.Bind(fakeComponents);
            ShimTrimApplication.SDKVersionGet = () => 4;
            ShimTrimApplication.SoftwareVersionGet = () => "25.4";
            ShimTrimDateTime.AllInstances.IsClearGet = (t) => { return false; };
            ShimTrimDateTime.AllInstances.IsTimeClearGet = (t) => { return false; };
            ShimTrimDateTime.AllInstances.YearGet = (y) => { return 2024; };
            ShimTrimDateTime.AllInstances.MonthGet = (M365Site) => { return 2; };
            ShimTrimDateTime.AllInstances.DayInMonthGet = (d) => { return 2; };
            ShimTrimDateTime.AllInstances.SecondGet = (s) => { return 11; };
            ShimTrimDateTime.AllInstances.HourGet = (h) => { return 11; };
            ShimTrimDateTime.AllInstances.SecondInDayGet = (s) => { return 60; };
            ShimTrimDateTime.AllInstances.MinuteGet = (m) => { return 11; };
            ShimTrimDateTime.ConstructorDateTime = (t, d) => { };
            ShimTrimDateTime.NowGet = () =>
            {               
                return fakeShim.Instance;
            };
            ShimSapRepoConfigUserOptions.ConstructorDatabase = (instance, db) =>
            {
                ShimSapRepoConfigUserOptions.AllInstances.SapReposGet = (_) => sapRepos;
                ShimSapRepoConfigUserOptions.AllInstances.getIsEnabledString = (_, __) => isEnabled;
            };
        }


        [TearDown]
        public void TearDown()
        {
            _trimRepository.Dispose();
            _shimContext.Dispose();
        }

    }

}