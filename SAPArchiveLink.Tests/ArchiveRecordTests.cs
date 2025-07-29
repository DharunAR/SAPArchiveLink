using NUnit.Framework;
using TRIM.SDK;
using TRIM.SDK.Fakes;
using SAPArchiveLink;
using System;
using Microsoft.QualityTools.Testing.Fakes;
using Moq;
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

        [SetUp]
        public void Setup()
        {           
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
           

            // Shim Record
            recordShim = new ShimRecord
            {
                SapDocumentIdGet = () => "DOC123",
                DateCreatedGet = () => shimTrimDateTime,                
                ChildSapComponentsGet = () => childSapComponentsGet,
                DateModifiedGet = () => shimTrimDateTime,
            };

            _archiveRecord = new ArchiveRecord(recordShim, _trimConfig, _mockLogger.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _shimContext.Dispose();
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

            ShimTrimMainObject.AllInstances.Save = (tmo) =>
            {
            };
          
            var archiveRecord = new ArchiveRecord(recordShim, _trimConfig, _mockLogger.Object);
            archiveRecord.Save();

            // Assert
            _mockLogger.Verify(
                log => log.LogInformation("Record DOC123 saved successfully."),
                Times.Once);
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
    }

}