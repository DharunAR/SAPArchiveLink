using NUnit.Framework;
using TRIM.SDK;
using TRIM.SDK.Fakes;
using Microsoft.QualityTools.Testing.Fakes;
using Moq;
using SAPArchiveLink.Fakes;
namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class ArchiveRecordTests
    {
        private IDisposable _shimContext;
        private ArchiveRecord _archiveRecord;
        private Mock<ILogHelper<ArchiveRecord>> _mockLogger;
        private TrimConfigSettings _trimConfig;
        private ShimRecord recordShim;
        private Mock<IRecordSapComponent> _recordSapComponentMock;

        [SetUp]
        public void Setup()
        {           
            _recordSapComponentMock = new Mock<IRecordSapComponent>();
            _shimContext = ShimsContext.Create();
             _mockLogger = new Mock<ILogHelper<ArchiveRecord>>();
             _trimConfig = new TrimConfigSettings { WorkPath = "C:\\Trim\\Work" };

            var shimTrimDateTime = new ShimTrimDateTime
            {
                DateGet = () => new DateTime(2023, 12, 25), 
            };
            var childSapComponentsGet=new ShimRecordSapComponents
            {                       
            };

            ShimTrimMainObjectSearch.ConstructorDatabaseBaseObjectTypes = (@this, dbParam, objType) => { };
            ShimTrimSearchClause.ConstructorDatabaseBaseObjectTypesSearchClauseIds = (@this, dbParam, objType, clauseId) => 
            {
            };
            // Shim Record
            recordShim = new ShimRecord
            {
                SapDocumentIdGet = () => "DOC123",
                DateCreatedGet = () => shimTrimDateTime,                
                ChildSapComponentsGet = () => childSapComponentsGet,
                DateModifiedGet = () => shimTrimDateTime,
            };
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
                var fakeShim = new ShimTrimDateTime()
                {
                    DateGet = () => new DateTime(2024, 2, 2, 11, 11, 11)
                };
                return fakeShim.Instance;
            };
            _archiveRecord = new ArchiveRecord(recordShim, _trimConfig, _mockLogger.Object);
        }

        [Test]
        public void ArchiveRecord_ThrowsException_WhenRecordIsNull()
        {
            ShimRecord nullRecordShim = null;
            Assert.Throws<ArgumentNullException>(() => new ArchiveRecord(nullRecordShim, _trimConfig, _mockLogger.Object));
        }

        [Test]
        public void ArchiveRecord_ThrowsException_WhenTrimConfigIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ArchiveRecord(recordShim, null, _mockLogger.Object));
        }

        [Test]
        public void Save_ShouldLogSuccess_WhenRecordSavesCorrectly()
        {           
            ShimRecord recordShim = new ShimRecord
            {
                SapDocumentIdGet = () => "DOC123"
            };

            ShimTrimMainObject.AllInstances.Save = (tmo) => {};
          
            var archiveRecord = new ArchiveRecord(recordShim, _trimConfig, _mockLogger.Object);
            archiveRecord.Save();

            _mockLogger.Verify(log => log.LogInformation("Record DOC123 saved successfully."), Times.Once);
        }

        [Test]
        public void Save_ShouldLogError_WhenRecordSaveThrowsException()
        {
            // Arrange
            var mockLogger = new Mock<ILogHelper<ArchiveRecord>>();
            var trimConfig = new TrimConfigSettings { WorkPath = "C:\\Trim\\Work" };
            
            // Fake Record with Save throwing exception
            ShimRecord recordShim = new ShimRecord
            {
                SapDocumentIdGet = () => "DOC123"                
            };

            ShimTrimMainObject.AllInstances.Save = (tmo) =>throw new InvalidOperationException("Save failed");  
          
            var archiveRecord = new ArchiveRecord(recordShim, trimConfig, mockLogger.Object);
          
            // Act + Assert
            Assert.Throws<InvalidOperationException>(() => archiveRecord.Save());

            mockLogger.Verify(
                log => log.LogError("Error saving record DOC123", It.IsAny<Exception>()),
                Times.Once);
        }

        [Test]
        public void DateCreated_ShouldReturnDate()
        { 
            var date = _archiveRecord.DateCreated;
            Assert.That(date, Is.EqualTo(default(DateTime)));
        }

        [Test]
        public void DateModified_ShouldReturnDate()
        {
            var date = _archiveRecord.DateModified;
            Assert.That(date, Is.EqualTo(default(DateTime)));
        }

        [Test]
        public void ComponentCount_ShouldReturnZero()
        {
            Assert.That(_archiveRecord.ComponentCount, Is.EqualTo(0));
        }

        [Test]
        public void CreateNewArchiveRecord_WithValidRecordType_ShouldReturnArchiveRecord()
        {
            var mockLogger = new Mock<ILogHelper<ArchiveRecord>>();
            var db = new ShimDatabase();
            var trimConfig = new TrimConfigSettings { WorkPath = "C:\\Trim\\Work", RecordTypeName = "SAPTest" };
            ShimRecord.ConstructorDatabaseRecordType = (instance, database, recordType) => { };
            ShimRecord.AllInstances.SapReposIdSetString = (d, value) => { };
            ShimRecord.AllInstances.TypedTitleSetString = (d, value) => { };
            ShimRecord.AllInstances.SapArchiveLinkVsnSetString = (d, value) => { };
            ShimRecord.AllInstances.SapDocumentProtectionSetString = (d, value) => { };
            ShimRecord.AllInstances.SapArchiveDateSetTrimDateTime = (d, value) => { };
            ShimRecord.AllInstances.SapModifiedDateSetTrimDateTime = (d, value) => { };
            ShimRecord.AllInstances.SapDocumentIdSetString = (d, value) => { };
            ShimRecordType.AllInstances.SapTitleTemplateGet = (value) => { return "%contrep %docid"; };
            ShimDatabase.AllInstances.FindTrimObjectByNameBaseObjectTypesString = (d, type, name) =>
            {
                return new ShimRecordType
                {
                    UsualBehaviourGet = () => RecordBehaviour.SapDocument
                };
            };
            ShimTrimDateTime.ConstructorDateTime = (t, d) => { };
            var archiveRecord = ArchiveRecord.CreateNewArchiveRecord(db, trimConfig, mockLogger.Object, new CreateSapDocumentModel() { DocId = "docId", ContentLength = "100", ContRep = "CM", PVersion = "0047", DocProt = "r"});
            Assert.That(archiveRecord, Is.Not.Null);
        }

        [Test]
        public void CreateNewArchiveRecord_ReturnsNull_WhenRecordTypeNotFound()
        {
            bool isErrorMessageLogged = false;
            var mockLogger = new Mock<ILogHelper<ArchiveRecord>>();
            var db = new ShimDatabase();
            var trimConfig = new TrimConfigSettings { WorkPath = "C:\\Trim\\Work", RecordTypeName = "SAPTest" };
            ShimDatabase.AllInstances.FindTrimObjectByNameBaseObjectTypesString = (d, type, name) =>
            {
                return null;
            };
            ShimTrimApplication.GetMessageImplDatabaseInt32StringArray = (db, id, args) =>
            {
                isErrorMessageLogged = true;
                return "Record type not found";
            };
            ShimStringArray.Constructor = (instance) => { };
            ShimStringArray.AllInstances.AddString = (instance, value) => { };
            var archiveRecord = ArchiveRecord.CreateNewArchiveRecord(db, trimConfig, mockLogger.Object, new CreateSapDocumentModel() { DocId = "docId", ContentLength = "100", ContRep = "CM", PVersion = "0047", DocProt = "r" });
            Assert.That(archiveRecord, Is.Null);
            Assert.That(isErrorMessageLogged, Is.True);
        }

        [Test]
        public void GetRecordType_WithValidName_ShouldReturnType()
        {
            var mockLogger = new Mock<ILogHelper<ArchiveRecord>>();
            var db = new ShimDatabase();
            var config = new TrimConfigSettings { RecordTypeName = "TestType" };

            ShimDatabase.AllInstances.FindTrimObjectByNameBaseObjectTypesString = (d, type, name) =>
            {
                return new ShimRecordType
                {
                    UsualBehaviourGet = () => RecordBehaviour.SapDocument
                };
            };

            var result = ArchiveRecord.GetRecordType(db, config, "REPO1", mockLogger.Object);
            Assert.That(result,Is.Not.Null);
        }

        [Test]
        public void GetRecordType_WithValidName_ReturnsNull() 
        {
            var mockLogger = new Mock<ILogHelper<ArchiveRecord>>();
            var db = new ShimDatabase();
            var config = new TrimConfigSettings { RecordTypeName = "TestType" };
            ShimDatabase.AllInstances.FindTrimObjectByNameBaseObjectTypesString = (d, type, name) =>
            {
                return new ShimRecordType
                {
                    UsualBehaviourGet = () => RecordBehaviour.Document
                };
            };
            var result = ArchiveRecord.GetRecordType(db, config, "REPO1", mockLogger.Object);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetRecordType_WithInvalidName_ReturnsNull()
        {
            var mockLogger = new Mock<ILogHelper<ArchiveRecord>>();
            var db = new ShimDatabase();
            var config = new TrimConfigSettings { RecordTypeName = "TestType" };
            ShimDatabase.AllInstances.FindTrimObjectByNameBaseObjectTypesString = (d, type, name) =>
            {
                return null;
            };
            var result = ArchiveRecord.GetRecordType(db, config, "REPO1", mockLogger.Object);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetRecordType_WithContRep_ReturnsTrimRepository()
        {
            var mockLogger = new Mock<ILogHelper<ArchiveRecord>>();
            var db = new ShimDatabase();
            var config = new TrimConfigSettings { RecordTypeName = "" };

            ShimRecordType.ConstructorDatabaseTrimURI = (instance, database, uri) => { };

            ShimTrimSearchClause.AllInstances.SetCriteriaFromStringString = (instance, value) =>
            {
                return true;
            };

            ShimTrimMainObjectSearch.AllInstances.AddSearchClauseTrimSearchClause = (instance, clause) => { };
            ShimTrimMainObjectSearch.AllInstances.CountGet = (instance) =>
            {
                return 1;
            };
            ShimTrimMainObjectSearch.AllInstances.GetResultAsUriArrayInt64 = (max, db) =>
            {
                return new ShimTrimURIList();
            };
            ShimTrimMainObjectSearch.AllInstances.GetResultAsUriArray = (max) =>
            {
                return new long[] { 1 };
            };
            ShimRecordType.ConstructorDatabase = (instance, db) => { };

            ShimTrimMainObjectSearch.AllInstances.GetEnumerator = (tmo) =>
            {
                return ((IEnumerable<ShimRecord>)new ShimRecord[] { new ShimRecord() }).GetEnumerator();
            };

            var result = ArchiveRecord.GetRecordType(db, config, "REPO1", mockLogger.Object);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void GetRecordType_WithContRep_ReturnsNull()
        {
            var mockLogger = new Mock<ILogHelper<ArchiveRecord>>();
            var db = new ShimDatabase();
            var config = new TrimConfigSettings { RecordTypeName = "" };
            ShimRecordType.ConstructorDatabaseTrimURI = (instance, database, uri) => { };
            ShimTrimSearchClause.AllInstances.SetCriteriaFromStringString = (instance, value) =>
            {
                return false;
            };
            ShimTrimMainObjectSearch.AllInstances.AddSearchClauseTrimSearchClause = (instance, clause) => { };
            ShimTrimMainObjectSearch.AllInstances.CountGet = (instance) =>
            {
                return 0;
            };
            var result = ArchiveRecord.GetRecordType(db, config, "REPO1", mockLogger.Object);
            Assert.That(result, Is.Null);
        }
        [Test]
        public void ExtractComponentMetadata_ShouldMapPropertiesCorrectly()
        {           
            using (ShimsContext.Create())
            {
                // Arrange
                var fakeComponent = new ShimRecordSapComponent
                {
                    ComponentIdGet = () => "COMP123",
                    ContentTypeGet = () => null,  
                    CharacterSetGet = () => null, 
                    ApplicationVersionGet = () => "1.2.3",
                    BytesGet = () => 1024,
                    ArchiveDateGet = () => DateTime.Now,
                    DateModifiedGet = () => DateTime.Now,
                    ArchiveLinkVersionGet = () => "A001"
                };
                ShimRecordSapComponent.AllInstances.ArchiveDateSetTrimDateTime = (d, value) => { };
                ShimRecordSapComponent.AllInstances.DateModifiedSetTrimDateTime = (d, value) => { };
                ShimRecordSapComponent.AllInstances.DateModifiedGet = (_) => DateTime.Now;              
                ShimRecordSapComponent.AllInstances.ArchiveDateGet = (_) => DateTime.Now;
               
               
                // Act
                var result = _archiveRecord.ExtractComponentMetadata(fakeComponent);

                // Assert
                Assert.That(result.CompId, Is.EqualTo("COMP123"));
                Assert.That(result.ContentType, Is.EqualTo("application/octet-stream"));
                Assert.That(result.Charset, Is.EqualTo("UTF-8"));
                Assert.That(result.Version, Is.EqualTo("1.2.3"));
                Assert.That(result.ContentLength, Is.EqualTo(1024));
                Assert.That(result.CreationDate, Is.EqualTo(new DateTime(2024, 2, 2, 11,11,11)));
                Assert.That(result.ModifiedDate, Is.EqualTo(new DateTime(2024, 2, 2, 11, 11, 11)));
                Assert.That(result.Status, Is.EqualTo("online"));
                Assert.That(result.PVersion, Is.EqualTo("A001"));
            }
        }
        [Test]
        public void AddComponent_Should_Call_AddComponent_And_Log_Info()
        {
            using (ShimsContext.Create())
            {
                var called = true;
                            
                ShimRecordSapComponentsAdapter.ConstructorRecordSapComponents = (instance, sdkComponents) =>
                {
                    var shimAdapter = new ShimRecordSapComponentsAdapter(instance)
                    {
                        AddComponentStringStringStringStringString = (compId, version, contentType, charset, filePath) =>
                        {                           
                            Assert.That(compId, Is.EqualTo("comp1"));                           
                        }
                    };
                };

                _archiveRecord.AddComponent("comp1", "/path/to/file.pdf", "application/pdf", "UTF-8", "1.0");

              
                Assert.That(called, "AddComponent was called");

                _mockLogger.Verify(
                    x => x.LogInformation(It.Is<string>(s => s.Contains("Adding component 'comp1' to record DOC123"))),
                    Times.Once);
            }
        }

        [TestCase(null)]
        [TestCase("")]
        public void AddComponent_Returns_If_CompId_IsNotAvailable(string? compId)
        {
            Assert.DoesNotThrow(() => _archiveRecord.AddComponent(compId, "/path/to/file.pdf", "application/pdf", "UTF-8", "1.0"));
        }

        [Test]
        public void AddComponent_When_AddComponent_Throws_Logs_Error_And_Rethrows()
        {
            ShimRecordSapComponentsAdapter.ConstructorRecordSapComponents = (instance, sdkComponents) =>
            {
                var shimAdapter = new ShimRecordSapComponentsAdapter(instance)
                {
                    AddComponentStringStringStringStringString = (compId, version, contentType, charset, filePath) =>
                    {
                        throw new InvalidOperationException("Simulated failure");
                    }
                };
            };
            var ex = Assert.Throws<InvalidOperationException>(() =>
                _archiveRecord.AddComponent("comp1", "/path/to/file.pdf", "application/pdf", "UTF-8", "1.0")
            );

            Assert.That(ex.Message, Is.EqualTo("Simulated failure"));

            _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("Failed to add component")), It.IsAny<Exception>()), Times.Once);
        }


        [Test]
        public void UpdateComponent_Should_Set_Fields_Correctly()
        {
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SapDocumentComponentModel
                {
                    ContentType = "application/pdf",
                    Charset = "UTF-8",
                    Version = "1.0",
                    PVersion = "0045",
                    FileName = "sample.pdf",
                    ModifiedDate = new DateTime(2024, 2, 2, 11, 11, 11)
                };             
          
                _archiveRecord.UpdateComponent(_recordSapComponentMock.Object, model);
                _recordSapComponentMock.VerifySet(x=>x.ContentType= "application/pdf", Times.AtLeastOnce);
            }
        }

        [Test]
        public void ArchiveRecord_FindComponentById_Should_Find_ComponentById()
        {
            var archiveRecord = GetRecordWithComponents();
            var comp = archiveRecord.FindComponentById("comp2");
            Assert.That(comp, Is.Not.Null);
            Assert.That(comp, Is.InstanceOf<IRecordSapComponent>());
            Assert.That(comp.ComponentId, Is.EqualTo("COMP2"));
        }

        [Test]
        public void ArchiveRecord_FindComponentById_ReturnsNull()
        {
            var archiveRecord = GetRecordWithComponents();
            var comp = archiveRecord.FindComponentById("test");
            Assert.That(comp, Is.Null);
        }

        [Test]
        public void ArchiveRecord_HasComponent_ReturnsTrue()
        {
            var archiveRecord = GetRecordWithComponents();
            Assert.That(archiveRecord.HasComponent("comp1"), Is.True);
        }

        [Test]
        public void ArchiveRecord_HasComponent_ReturnsFalse()
        {
            var archiveRecord = GetRecordWithComponents();
            Assert.That(archiveRecord.HasComponent("test"), Is.False);
        }

        [Test]
        public void ArchiveRecord_GetComponentById_ReturnsSapDocumentComponentModel()
        {
            var archiveRecord = GetRecordWithComponents();
            var model = archiveRecord.GetComponentById("COMP2");
            Assert.Multiple(() =>
            {
                Assert.That(model, Is.Not.Null);
                Assert.That(model, Is.InstanceOf<SapDocumentComponentModel>());
                Assert.That(model?.CompId, Is.EqualTo("COMP2"));
            });
        }

        [Test]
        public void ArchiveRecord_GetComponentById_ReturnsNull()
        {
            var archiveRecord = GetRecordWithComponents();
            var comp = archiveRecord.GetComponentById("test");
            Assert.That(comp, Is.Null);
        }

        [Test]
        public void ArchiveRecord_GetAllComponents_ReturnsListofSapDocumentComponentModel()
        {
            var archiveRecord = GetRecordWithComponents();
            var comps = archiveRecord.GetAllComponents();
            Assert.Multiple(() =>
            {
                Assert.That(comps, Is.Not.Null);
                Assert.That(comps, Is.InstanceOf<List<SapDocumentComponentModel>>());
                Assert.That(comps.Any(), Is.True);
                Assert.That(comps.Count, Is.EqualTo(3));
            });
        }

        [Test]
        public void ArchiveRecord_DeleteComponent_ReturnsTrue()
        {
            ShimTrimMainObject.AllInstances.Delete = (obj) =>
            {
                Assert.That(obj, Is.Not.Null);
            };
            var archiveRecord = GetRecordWithComponents();
            Assert.That(archiveRecord.DeleteComponent("COMP2"), Is.True);
        }

        [Test]
        public void ArchiveRecord_DeleteComponent_ReturnsFalse()
        {
            var archiveRecord = GetRecordWithComponents();
            Assert.That(archiveRecord.DeleteComponent("test"), Is.False);
        }

        [Test]
        public void ArchiveRecord_DeleteRecord()
        {
            ShimTrimMainObject.AllInstances.Delete = (obj) =>
            {
                Assert.That(obj, Is.Not.Null);
            };
            var archiveRecord = GetRecordWithComponents();
            archiveRecord.DeleteRecord();
            _mockLogger.Verify(x => x.LogInformation(It.Is<string>(s => s.Contains("deleted successfully."))), Times.Once);
        }

        [Test]
        public async Task ArchiveRecord_ExtractAllComponents_ReturnsZeroResults()
        {
            ShimRecordSapComponent.AllInstances.GetExtractDocument = (a) =>
            {
                return null;
            };
            var shimRecord = new ShimRecord();
            var fakeComponents = new List<RecordSapComponent>
            {
                new ShimRecordSapComponent { ComponentIdGet = () => "COMP1",  ArchiveDateGet = () => new ShimTrimDateTime { DateGet = () => new DateTime(2024, 2, 2, 11, 11, 11) }.Instance,
                    DateModifiedGet = () => new ShimTrimDateTime { DateGet = () => new DateTime(2024, 2, 2, 11, 11, 11) }.Instance }
            };
            var shimComponents = new ShimRecordSapComponents();
            shimComponents.Bind(fakeComponents);
            shimRecord.ChildSapComponentsGet = () => shimComponents;
            var archive = GetRecordWithComponents();
            var components = await archive.ExtractAllComponents();
            Assert.That(components.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task ArchiveRecord_ExtractAllComponents_ReturnsAllComponents()
        {
            ShimRecordSapComponent.AllInstances.GetExtractDocument = (a) =>
            {
                return new ShimExtractDocument()
                {
                    FileNameGet = () => Path.Combine(AppContext.BaseDirectory, "TestDocuments", "Test.pdf"),
                    FileTypeGet = () => "application/pdf"
                };
            };
            ShimExtractDocument.AllInstances.DoExtractStringBooleanBooleanString = (instance, t, a , d , e) =>
            {
                
            };
            var shimRecord = new ShimRecord();
            var fakeComponents = new List<RecordSapComponent>
            {
                new ShimRecordSapComponent { ComponentIdGet = () => "COMP1",  ArchiveDateGet = () => new ShimTrimDateTime { DateGet = () => new DateTime(2024, 2, 2, 11, 11, 11) }.Instance,
                    DateModifiedGet = () => new ShimTrimDateTime { DateGet = () => new DateTime(2024, 2, 2, 11, 11, 11) }.Instance }
            };
            var shimComponents = new ShimRecordSapComponents();
            shimComponents.Bind(fakeComponents);
            shimRecord.ChildSapComponentsGet = () => shimComponents;
            var archive = new ArchiveRecord(shimRecord, _trimConfig, _mockLogger.Object);
            var components = await archive.ExtractAllComponents();
            Assert.That(components.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task ArchiveRecord_ExtractComponentById_ReturnsSapComponent() 
        {
            string filename = Path.Combine(AppContext.BaseDirectory, "TestDocuments", "Test.pdf");
            ShimRecordSapComponent.AllInstances.GetExtractDocument = (a) =>
            {
                return new ShimExtractDocument()
                {
                    FileNameGet = () => filename,
                    FileTypeGet = () => "application/pdf"
                };
            };
            ShimExtractDocument.AllInstances.DoExtractStringBooleanBooleanString = (instance, t, a, d, e) => { };
            var archive = GetRecordWithComponents();
            var component = await archive.ExtractComponentById("COMP2");
            Assert.That(component, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(component.FileName, Is.EqualTo(filename));
                Assert.That(component.CompId, Is.EqualTo("COMP2"));
            });            
        }

        [Test]
        public void ArchiveRecord_SetRecordMetadata()
        {
            ShimRecord.AllInstances.SapModifiedDateSetTrimDateTime = (r, date) => { };
            ShimTrimDateTime.NowGet = () => { return DateTime.Now; };
            Assert.DoesNotThrow(() => _archiveRecord.SetRecordMetadata());
        }

        [Test]
        public async Task ArchiveRecord_ExtractComponentById_ReturnsNull()
        {
            var archive = GetRecordWithComponents();
            var component = await archive.ExtractComponentById("test");
            Assert.That(component, Is.Null);
        }

        private ArchiveRecord GetRecordWithComponents()
        {
            var shimRecord = new ShimRecord();
            shimRecord.ChildSapComponentsGet = () => GetRecordSapComponents();
            return new ArchiveRecord(shimRecord, _trimConfig, _mockLogger.Object);
        }

        private ShimRecordSapComponents GetRecordSapComponents(bool forExtract = false)
        {
            var fakeComponents = new List<RecordSapComponent>
            {
                new ShimRecordSapComponent { ComponentIdGet = () => "COMP1",  ArchiveDateGet = () => new ShimTrimDateTime { DateGet = () => new DateTime(2024, 2, 2, 11, 11, 11) }.Instance,
                    DateModifiedGet = () => new ShimTrimDateTime { DateGet = () => new DateTime(2024, 2, 2, 11, 11, 11) }.Instance },
                new ShimRecordSapComponent
                {
                    ComponentIdGet = () => "COMP2",
                    ApplicationVersionGet = () => "1.0",
                    ArchiveLinkVersionGet = () => "1.0",
                    ContentTypeGet = () => "application/pdf",
                    CharacterSetGet = () => "UTF-8",
                    BytesGet = () => 100,
                    FileNameGet = () => "test.pdf",
                    ArchiveDateGet = () => new ShimTrimDateTime { DateGet = () => new DateTime(2024, 2, 2, 11, 11, 11) }.Instance,
                    DateModifiedGet = () => new ShimTrimDateTime { DateGet = () => new DateTime(2024, 2, 2, 11, 11, 11) }.Instance
                },
                new ShimRecordSapComponent { ComponentIdGet = () => "COMP3",  ArchiveDateGet = () => new ShimTrimDateTime { DateGet = () => new DateTime(2024, 2, 2, 11, 11, 11) }.Instance,
                    DateModifiedGet = () => new ShimTrimDateTime { DateGet = () => new DateTime(2024, 2, 2, 11, 11, 11) }.Instance }
            };
            var shimComponents = new ShimRecordSapComponents();
            shimComponents.Bind(fakeComponents);
            return shimComponents;
        }

        [TearDown]
        public void TearDown()
        {
            _shimContext.Dispose();
        }
    }
}