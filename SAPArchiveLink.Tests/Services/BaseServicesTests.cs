using Microsoft.AspNetCore.Http;
using Moq;
using System.ComponentModel;
using TRIM.SDK;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class BaseServicesTests
    {
        private Mock<ILogHelper<BaseServices>> _loggerMock;
        private Mock<ICommandResponseFactory> _responseFactoryMock;
        private Mock<IDatabaseConnection> _dbConnectionMock;
        private Mock<IDownloadFileHandler> _downloadFileHandlerMock;
        private Mock<ITrimRepository> _trimRepoMock;
        private Mock<IArchiveRecord> _archiveRecordMock;
        private Mock<IRecordSapComponent> _recordSapComponentMock;
        private Mock<ISdkMessageProvider> _messageProviderMock;
        private BaseServices _service;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogHelper<BaseServices>>();
            _responseFactoryMock = new Mock<ICommandResponseFactory>();
            _dbConnectionMock = new Mock<IDatabaseConnection>();
            _trimRepoMock = new Mock<ITrimRepository>();
            _downloadFileHandlerMock = new Mock<IDownloadFileHandler>();
            _archiveRecordMock = new Mock<IArchiveRecord>();
            _recordSapComponentMock = new Mock<IRecordSapComponent>();
            _messageProviderMock = new Mock<ISdkMessageProvider>();

            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(_trimRepoMock.Object);

            _service = new BaseServices(
                _loggerMock.Object,
                _responseFactoryMock.Object,
                _dbConnectionMock.Object,
                _downloadFileHandlerMock.Object,
                _messageProviderMock.Object
            );
        }

        [Test]
        public async Task PutCert_ReturnsError_WhenAuthIdMissing()
        {
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _service.PutCert(null, new MemoryStream(), "contRep", "crud");

            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(s => s.Contains("authId")), StatusCodes.Status404NotFound), Times.Once);
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task PutCert_ReturnsError_WhenContRepIdMissing()
        {
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _service.PutCert("auth", new MemoryStream(), null, "crud");

            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(s => s.Contains("contRep")), StatusCodes.Status404NotFound), Times.Once);
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task PutCert_ReturnsProtocolText_OnSuccess()
        {
            _responseFactoryMock.Setup(f => f.CreateProtocolText(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _service.PutCert("auth", new MemoryStream(new byte[] { 1, 2, 3 }), "contRep", "crud");

            _responseFactoryMock.Verify(f => f.CreateProtocolText(It.Is<string>(s => s.Contains("Certificate published")), It.IsAny<int>(), It.IsAny<string>()), Times.Once);
            Assert.IsNotNull(result);
        }

        #region DocGet ServiceTests

        [Test]
        public async Task DocGet_ReturnsExpectedError_WhenValidationFails()
        {
            var sapDoc = new SapDocumentRequest { DocId = null, ContRep = null, PVersion = null };
            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>())).Returns(errorResponse);

            var result = await _service.DocGetSapComponents(sapDoc);

            _responseFactoryMock.Verify(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
            Assert.That(result, Is.EqualTo(errorResponse));
        }


        [Test]
        public async Task DocGet_ReturnsError_WhenRecordNotFound()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc1", ContRep = "rep1", PVersion = "v1" };
            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns((IArchiveRecord)null);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);

            var errorMessage = "Document doc1 not found";
            _messageProviderMock.Setup(m => m.GetMessage(MessageIds.sap_documentNotFound, It.IsAny<string[]>()))
                .Returns(errorMessage);

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError(errorMessage, StatusCodes.Status404NotFound)).Returns(errorResponse);

            var result = await _service.DocGetSapComponents(sapDoc);

            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task DocGet_ReturnsError_WhenComponentNotFound()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc1", ContRep = "rep1", PVersion = "v1", CompId = "compX" };
            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.ExtractComponentById("compX")).ReturnsAsync((SapDocumentComponentModel)null);

            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);

            var errorMessage = "Component compX not found in doc1";
            _messageProviderMock.Setup(m => m.GetMessage(MessageIds.sap_componentNotFound, It.IsAny<string[]>()))
                .Returns(errorMessage);

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError(errorMessage, StatusCodes.Status404NotFound)).Returns(errorResponse);

            var result = await _service.DocGetSapComponents(sapDoc);

            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task DocGet_ReturnsSingleComponentResponse_WhenCompIdProvidedAndExists()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc1", ContRep = "rep1", PVersion = "v1", CompId = "comp1" };
            var component = new SapDocumentComponentModel
            {
                CompId = "comp1",
                ContentType = "application/pdf",
                ContentLength = 123,
                CreationDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Status = "active",
                PVersion = "v1",
                Data = new MemoryStream(),
                FileName = "file.pdf"
            };

            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.ExtractComponentById("comp1")).ReturnsAsync(component);

            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);

            var expectedResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateDocumentContent(component.Data, component.ContentType, StatusCodes.Status200OK, component.FileName)).Returns(expectedResponse);
            var result = await _service.DocGetSapComponents(sapDoc);

            Assert.That(result, Is.EqualTo(expectedResponse));
        }


        [Test]
        public async Task DocGet_ReturnsMultipartResponse_WhenNoCompIdProvided()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc1", ContRep = "rep1", PVersion = "v1" };
            var components = new List<SapDocumentComponentModel>
            {
                new SapDocumentComponentModel { CompId = "comp1", ContentType = "application/pdf", Data = new MemoryStream() }
            };

            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.ExtractAllComponents()).ReturnsAsync(components);
            recordMock.SetupGet(r => r.DateCreated).Returns(DateTime.UtcNow);
            recordMock.SetupGet(r => r.DateModified).Returns(DateTime.UtcNow);
            recordMock.SetupGet(r => r.ComponentCount).Returns(components.Count);

            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);

            var expectedResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateMultipartDocument(components, It.IsAny<int>())).Returns(expectedResponse);
            var result = await _service.DocGetSapComponents(sapDoc);

            Assert.That(result, Is.EqualTo(expectedResponse));
        }

        #endregion

        #region GetSapDocument ServiceTests

        [Test]
        public async Task GetSapDocument_ReturnsError_WhenValidationFails()
        {
            var sapDoc = new SapDocumentRequest { DocId = null, ContRep = null, PVersion = null };
            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>())).Returns(errorResponse);

            var result = await _service.GetSapDocument(sapDoc);

            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task GetSapDocument_ReturnsError_WhenRecordNotFound()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc", ContRep = "rep", PVersion = "1" };
            var repoMock = new Mock<ITrimRepository>();
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns((IArchiveRecord)null);

            var errorMessage = "Document doc not found";
            _messageProviderMock.Setup(m => m.GetMessage(MessageIds.sap_documentNotFound, It.IsAny<string[]>()))
                .Returns(errorMessage);

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError(errorMessage, StatusCodes.Status404NotFound)).Returns(errorResponse);

            var result = await _service.GetSapDocument(sapDoc);

            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task GetSapDocument_ReturnsError_WhenNoValidComponentFound()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc", ContRep = "rep", PVersion = "1" };
            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.HasComponent(It.IsAny<string>())).Returns(false);
            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError("No valid component found", StatusCodes.Status404NotFound)).Returns(errorResponse);

            var result = await _service.GetSapDocument(sapDoc);

            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task GetSapDocument_ReturnsError_WhenComponentNotFound()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc", ContRep = "rep", PVersion = "1", CompId = "compX" };
            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.ExtractComponentById("compX")).ReturnsAsync((SapDocumentComponentModel)null);
            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);

            var errorMessage = "Component compX not found in doc";
            _messageProviderMock.Setup(m => m.GetMessage(MessageIds.sap_componentNotFound, It.IsAny<string[]>()))
                .Returns(errorMessage);

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError(errorMessage, StatusCodes.Status404NotFound)).Returns(errorResponse);

            var result = await _service.GetSapDocument(sapDoc);

            Assert.That(result, Is.EqualTo(errorResponse));
        }


        [Test]
        public async Task GetSapDocument_ReturnsError_WhenComponentCouldNotBeLoaded()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc", ContRep = "rep", PVersion = "1", CompId = "comp1" };
            var recordMock = new Mock<IArchiveRecord>();
            // Remove HasComponent setup
            recordMock.Setup(r => r.ExtractComponentById("comp1")).ReturnsAsync((SapDocumentComponentModel)null);
            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            var errorResponse = Mock.Of<ICommandResponse>();
            var errorMessage = "Component 'comp1' not found";
            _messageProviderMock.Setup(m => m.GetMessage(MessageIds.sap_componentNotFound, It.IsAny<string[]>()))
                .Returns(errorMessage);
            _responseFactoryMock.Setup(f => f.CreateError(errorMessage, StatusCodes.Status404NotFound)).Returns(errorResponse);

            var result = await _service.GetSapDocument(sapDoc);

            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task GetSapDocument_ReturnsDocumentContent_OnSuccess()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc", ContRep = "rep", PVersion = "1", CompId = "comp1", FromOffset = 0, ToOffset = 0 };
            var component = new SapDocumentComponentModel
            {
                CompId = "comp1",
                ContentType = "application/pdf",
                ContentLength = 100,
                Data = new MemoryStream(new byte[] { 1, 2, 3 }),
                FileName = "file.pdf",
                Charset = "utf-8",
                Version = "1.0"
            };
            var recordMock = new Mock<IArchiveRecord>();
            // Remove HasComponent setup
            recordMock.Setup(r => r.ExtractComponentById("comp1")).ReturnsAsync(component);
            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);

            var expectedResponse = new Mock<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateDocumentContent(It.IsAny<Stream>(), component.ContentType, StatusCodes.Status200OK, component.FileName))
                .Returns(expectedResponse.Object);

            var result = await _service.GetSapDocument(sapDoc);

            Assert.That(result, Is.EqualTo(expectedResponse.Object));
        }


        [Test]
        public async Task GetSapDocument_ReturnsError_WhenFromOffsetBeyondContentLength()
        {
            var sapDoc = new SapDocumentRequest
            {
                DocId = "doc",
                ContRep = "rep",
                PVersion = "1",
                CompId = "comp1",
                FromOffset = 200,
                ToOffset = 210
            };
            var component = new SapDocumentComponentModel
            {
                CompId = "comp1",
                ContentType = "application/pdf",
                ContentLength = 100,
                Data = new MemoryStream(),
                FileName = "file.pdf"
            };
            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.HasComponent("comp1")).Returns(true);
            recordMock.Setup(r => r.ExtractComponentById("comp1")).ReturnsAsync(component);
            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>())).Returns(errorResponse);

            var result = await _service.GetSapDocument(sapDoc);

            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task GetSapDocument_ReturnsPartialContent_WhenValidRange()
        {
            var sapDoc = new SapDocumentRequest
            {
                DocId = "doc",
                ContRep = "rep",
                PVersion = "1",
                CompId = "comp1",
                FromOffset = 1,
                ToOffset = 4
            };
            var data = new byte[] { 10, 20, 30, 40, 50 };
            var component = new SapDocumentComponentModel
            {
                CompId = "comp1",
                ContentType = "application/pdf",
                ContentLength = data.Length,
                Data = new MemoryStream(data),
                FileName = "file.pdf"
            };
            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.HasComponent("comp1")).Returns(true);
            recordMock.Setup(r => r.ExtractComponentById("comp1")).ReturnsAsync(component);
            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);

            var expectedResponse = new Mock<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateDocumentContent(It.IsAny<Stream>(), component.ContentType, StatusCodes.Status200OK, component.FileName))
                .Returns(expectedResponse.Object);

            var result = await _service.GetSapDocument(sapDoc);

            Assert.That(result, Is.EqualTo(expectedResponse.Object));
        }

        [Test]
        public async Task GetSapDocument_ReturnsError_WhenOffsetsAreNegative()
        {
            var sapDoc = new SapDocumentRequest
            {
                DocId = "doc",
                ContRep = "rep",
                PVersion = "1",
                CompId = "comp1",
                FromOffset = -1,
                ToOffset = 10
            };
            var component = new SapDocumentComponentModel
            {
                CompId = "comp1",
                ContentType = "application/pdf",
                ContentLength = 100,
                Data = new MemoryStream(),
                FileName = "file.pdf"
            };
            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.HasComponent("comp1")).Returns(true);
            recordMock.Setup(r => r.ExtractComponentById("comp1")).ReturnsAsync(component);
            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>())).Returns(errorResponse);

            var result = await _service.GetSapDocument(sapDoc);

            Assert.That(result, Is.EqualTo(errorResponse));
        }


        [Test]
        public async Task GetSapDocument_ReturnsFullContent_WhenNoRangeRequested()
        {
            var sapDoc = new SapDocumentRequest
            {
                DocId = "doc",
                ContRep = "rep",
                PVersion = "1",
                CompId = "comp1",
                FromOffset = 0,
                ToOffset = 0
            };
            var data = new byte[] { 100, 101, 102 };
            var component = new SapDocumentComponentModel
            {
                CompId = "comp1",
                ContentType = "application/pdf",
                ContentLength = data.Length,
                Data = new MemoryStream(data),
                FileName = "file.pdf"
            };
            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.HasComponent("comp1")).Returns(true);
            recordMock.Setup(r => r.ExtractComponentById("comp1")).ReturnsAsync(component);
            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);

            var expectedResponse = new Mock<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateDocumentContent(It.IsAny<Stream>(), component.ContentType, StatusCodes.Status200OK, component.FileName))
                .Returns(expectedResponse.Object);

            var result = await _service.GetSapDocument(sapDoc);

            Assert.That(result, Is.EqualTo(expectedResponse.Object));
        }

        [Test]
        public async Task GetSapDocument_ReturnsData_WhenCompIdIsNullAndDataExists()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc", ContRep = "rep", PVersion = "1", CompId = null };
            var component = new SapDocumentComponentModel { CompId = "data" };
            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.HasComponent("data")).Returns(true);
            recordMock.Setup(r => r.ExtractComponentById("data")).ReturnsAsync(component);
            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            var expectedResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateDocumentContent(It.IsAny<Stream>(), component.ContentType, StatusCodes.Status200OK, component.FileName))
                .Returns(expectedResponse);

            var result = await _service.GetSapDocument(sapDoc);

            Assert.That(result, Is.EqualTo(expectedResponse));
        }

        [Test]
        public async Task GetSapDocument_ReturnsData1_WhenCompIdIsNullAndData1Exists()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc", ContRep = "rep", PVersion = "1", CompId = null };
            var component = new SapDocumentComponentModel { CompId = "data1", /* ... */ };
            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.HasComponent("data")).Returns(false);
            recordMock.Setup(r => r.HasComponent("data1")).Returns(true);
            recordMock.Setup(r => r.ExtractComponentById("data1")).ReturnsAsync(component);
            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            var expectedResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateDocumentContent(It.IsAny<Stream>(), component.ContentType, StatusCodes.Status200OK, component.FileName))
                .Returns(expectedResponse);

            var result = await _service.GetSapDocument(sapDoc);

            Assert.That(result, Is.EqualTo(expectedResponse));
        }
        
        [Test]
        public async Task GetSapDocument_Returns404_WhenCompIdIsNullAndNoDataOrData1()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc", ContRep = "rep", PVersion = "1", CompId = null };
            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.HasComponent("data")).Returns(false);
            recordMock.Setup(r => r.HasComponent("data1")).Returns(false);
            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError("No valid component found", StatusCodes.Status404NotFound)).Returns(errorResponse);

            var result = await _service.GetSapDocument(sapDoc);

            Assert.That(result, Is.EqualTo(errorResponse));
        }

        #endregion

        #region CreateRecord ServiceTests

        [Test]
        public async Task CreateRecord_ReturnsError_WhenValidationFails()
        {
            var model = new CreateSapDocumentModel { DocId = null, ContRep = null, CompId = null, PVersion = null, ContentLength = null };
            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>())).Returns(errorResponse);

            var result = await _service.CreateRecord(model);

            _responseFactoryMock.Verify(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task CreateRecord_ReturnsProtocolText_OnSuccess()
        {
            var model = new CreateSapDocumentModel
            {
                DocId = "doc",
                ContRep = "rep",
                CompId = "comp",
                PVersion = "1",
                ContentLength = "10",
                Components = new List<SapDocumentComponentModel>
        {
            new SapDocumentComponentModel { CompId = "comp", FileName = "file.txt", Data = new MemoryStream(new byte[] { 1, 2 }) }
        }
            };
            var repoMock = new Mock<ITrimRepository>();
            var recordMock = new Mock<IArchiveRecord>();
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns((IArchiveRecord)null);
            repoMock.Setup(r => r.CreateRecord(It.IsAny<CreateSapDocumentModel>())).Returns(recordMock.Object);
            _downloadFileHandlerMock.Setup(h => h.DownloadDocument(It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync("file.txt");
            var expectedResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateProtocolText("Component(s) created successfully.", StatusCodes.Status201Created, "UTF-8"))
                .Returns(expectedResponse);

            var result = await _service.CreateRecord(model);

            _responseFactoryMock.Verify(f => f.CreateProtocolText("Component(s) created successfully.", StatusCodes.Status201Created, "UTF-8"), Times.Once);
            Assert.That(result, Is.EqualTo(expectedResponse));
        }

        [Test]
        public async Task CreateRecord_ReturnsError_WhenRecordisNull()
        {
            var model = new CreateSapDocumentModel
            {
                DocId = "doc",
                ContRep = "rep",
                CompId = "comp",
                PVersion = "1",
                ContentLength = "10",
                Components = new List<SapDocumentComponentModel>
        {
            new SapDocumentComponentModel { CompId = "comp", FileName = "file.txt", Data = new MemoryStream(new byte[] { 1, 2 }) }
        }
            };
            var repoMock = new Mock<ITrimRepository>();
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns((IArchiveRecord)null);
            repoMock.Setup(r => r.CreateRecord(It.IsAny<CreateSapDocumentModel>())).Returns((IArchiveRecord)null);

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError("Failed to create archive record in rep.", It.IsAny<int>()))
                .Returns(errorResponse);

            var result = await _service.CreateRecord(model);

            _responseFactoryMock.Verify(f => f.CreateError("Failed to create archive record in rep.", It.IsAny<int>()), Times.Once);
            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task CreateRecord_ReturnsError_WhenModelComponentIdisNull()
        {
            var model = new CreateSapDocumentModel
            {
                DocId = "doc",
                ContRep = "rep",
                CompId = "comp",
                PVersion = "1",
                ContentLength = "10",
                Components = new List<SapDocumentComponentModel>
        {
            new SapDocumentComponentModel { CompId = null, FileName = "file.txt", Data = new MemoryStream(new byte[] { 1, 2 }) }
        }
            };
            var repoMock = new Mock<ITrimRepository>();
            var recordMock = new Mock<IArchiveRecord>();
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns((IArchiveRecord)null);
            repoMock.Setup(r => r.CreateRecord(It.IsAny<CreateSapDocumentModel>())).Returns(recordMock.Object);

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError("Component ID was not specified", StatusCodes.Status400BadRequest))
                .Returns(errorResponse);

            var result = await _service.CreateRecord(model);

            _responseFactoryMock.Verify(f => f.CreateError("Component ID was not specified", StatusCodes.Status400BadRequest), Times.Once);
            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task CreateRecord_ReturnsError_WhenComponentIdisNull()
        {
            // This is a duplicate of the previous test, so you may remove or merge it.
            var model = new CreateSapDocumentModel
            {
                DocId = "doc",
                ContRep = "rep",
                CompId = null,
                PVersion = "1",
                ContentLength = "10",
                Components = new List<SapDocumentComponentModel>
        {
            new SapDocumentComponentModel { CompId = null, FileName = "file.txt", Data = new MemoryStream(new byte[] { 1, 2 }) }
        }
            };
            var repoMock = new Mock<ITrimRepository>();
            var recordMock = new Mock<IArchiveRecord>();
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns((IArchiveRecord)null);
            repoMock.Setup(r => r.CreateRecord(It.IsAny<CreateSapDocumentModel>())).Returns(recordMock.Object);

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError("CompId is required.", StatusCodes.Status400BadRequest))
                .Returns(errorResponse);

            var result = await _service.CreateRecord(model);

            _responseFactoryMock.Verify(f => f.CreateError("CompId is required.", StatusCodes.Status400BadRequest), Times.Once);
            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task CreateRecord_ReturnsError_WhenModelComponentIdisExists()
        {
            var model = new CreateSapDocumentModel
            {
                DocId = "doc",
                ContRep = "rep",
                CompId = "comp",
                PVersion = "1",
                ContentLength = "10",
                Components = new List<SapDocumentComponentModel>
        {
            new SapDocumentComponentModel { CompId = "comp", FileName = "file.txt", Data = new MemoryStream(new byte[] { 1, 2 }) }
        }
            };
            var repoMock = new Mock<ITrimRepository>();
            var recordMock = new Mock<IArchiveRecord>();
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns((IArchiveRecord)null);
            repoMock.Setup(r => r.CreateRecord(It.IsAny<CreateSapDocumentModel>())).Returns(recordMock.Object);
            recordMock.Setup(r => r.HasComponent("comp")).Returns(true);

            var errorMessage = "Component comp already exists in doc";
            _messageProviderMock.Setup(m => m.GetMessage(MessageIds.sap_componentExists, It.IsAny<string[]>()))
                .Returns(errorMessage);

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError(errorMessage, StatusCodes.Status403Forbidden)).Returns(errorResponse);

            var result = await _service.CreateRecord(model);

            _responseFactoryMock.Verify(f => f.CreateError(errorMessage, StatusCodes.Status403Forbidden), Times.Once);
            Assert.That(result, Is.EqualTo(errorResponse));
        }


        [Test]
        public async Task CreateRecord_ReturnsError_WhenFilePathIsNull()
        {
            var model = new CreateSapDocumentModel
            {
                DocId = "doc",
                ContRep = "rep",
                CompId = "comp",
                PVersion = "1",
                ContentLength = "10",
                Components = new List<SapDocumentComponentModel>
        {
            new SapDocumentComponentModel { CompId = "comp", FileName = "file.txt", Data = new MemoryStream(new byte[] { 1, 2 }) }
        }
            };
            var repoMock = new Mock<ITrimRepository>();
            var recordMock = new Mock<IArchiveRecord>();
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns((IArchiveRecord)null);
            repoMock.Setup(r => r.CreateRecord(It.IsAny<CreateSapDocumentModel>())).Returns(recordMock.Object);
            _downloadFileHandlerMock.Setup(h => h.DownloadDocument(It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync("");

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError("Failed to save component file.", StatusCodes.Status400BadRequest))
                .Returns(errorResponse);

            var result = await _service.CreateRecord(model);

            _responseFactoryMock.Verify(f => f.CreateError("Failed to save component file.", StatusCodes.Status400BadRequest), Times.Once);
            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task CreateRecord_ReturnsProtocolText_WithMultiPart_OnSuccess()
        {
            var model = new CreateSapDocumentModel
            {
                CompId = "comp",
                DocId = "doc",
                ContRep = "rep",
                PVersion = "1",
                ContentLength = "10",
                Components = new List<SapDocumentComponentModel>
        {
            new SapDocumentComponentModel { CompId = "comp", FileName = "file.txt", Data = new MemoryStream(new byte[] { 1, 2 }) },
            new SapDocumentComponentModel { CompId = "comp1", FileName = "file1.txt", Data = new MemoryStream(new byte[] { 1, 2 }) }
        }
            };
            var repoMock = new Mock<ITrimRepository>();
            var recordMock = new Mock<IArchiveRecord>();
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns((IArchiveRecord)null);
            repoMock.Setup(r => r.CreateRecord(It.IsAny<CreateSapDocumentModel>())).Returns(recordMock.Object);
            _downloadFileHandlerMock.Setup(h => h.DownloadDocument(It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync("file.txt");
            var expectedResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateProtocolText("Component(s) created successfully.", StatusCodes.Status201Created, "UTF-8"))
                .Returns(expectedResponse);

            var result = await _service.CreateRecord(model, true);

            _responseFactoryMock.Verify(f => f.CreateProtocolText("Component(s) created successfully.", StatusCodes.Status201Created, "UTF-8"), Times.Once);
            Assert.That(result, Is.EqualTo(expectedResponse));
        }

        [Test]
        public async Task CreateRecord_WithMultiPart_ReturnsError_WhenModelComponentIdisNull()
        {
            var model = new CreateSapDocumentModel
            {
                DocId = "doc",
                ContRep = "rep",
                PVersion = "1",
                ContentLength = "10",
                Components = new List<SapDocumentComponentModel>
        {
            new SapDocumentComponentModel { CompId = null, FileName = "file.txt", Data = new MemoryStream(new byte[] { 1, 2 }) },
            new SapDocumentComponentModel { CompId = "comp1", FileName = "file1.txt", Data = new MemoryStream(new byte[] { 1, 2 }) }
        }
            };
            var repoMock = new Mock<ITrimRepository>();
            var recordMock = new Mock<IArchiveRecord>();
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns((IArchiveRecord)null);
            repoMock.Setup(r => r.CreateRecord(It.IsAny<CreateSapDocumentModel>())).Returns(recordMock.Object);

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError("CompId is required.", StatusCodes.Status400BadRequest))
                .Returns(errorResponse);

            var result = await _service.CreateRecord(model, true);

            _responseFactoryMock.Verify(f => f.CreateError("CompId is required.", StatusCodes.Status400BadRequest), Times.Once);
            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task CreateRecord_ReturnsError_WhenComponentExists()
        {
            var model = new CreateSapDocumentModel
            {
                DocId = "doc",
                ContRep = "rep",
                CompId = "comp",
                PVersion = "1",
                ContentLength = "10",
                Components = new List<SapDocumentComponentModel>
        {
            new SapDocumentComponentModel { CompId = "comp", FileName = "file.txt", Data = new MemoryStream(new byte[] { 1, 2 }) }
        }
            };
            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.HasComponent("comp")).Returns(true);
            _trimRepoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);

            var errorMessage = "Component comp already exists in doc";
            _messageProviderMock.Setup(m => m.GetMessage(MessageIds.sap_componentExists, It.IsAny<string[]>()))
                .Returns(errorMessage);

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError(errorMessage, StatusCodes.Status403Forbidden)).Returns(errorResponse);

            var result = await _service.CreateRecord(model);

            _responseFactoryMock.Verify(f => f.CreateError(errorMessage, StatusCodes.Status403Forbidden), Times.Once);
            Assert.That(result, Is.EqualTo(errorResponse));
        }

        #endregion


        #region UpdateRecord ServiceTests

        [Test]
        public async Task UpdateRecord_SuccessfulUpdate_ReturnsSuccessResponse()
        {
            // Arrange
            var compId = "comp1";
            var docId = "doc1";
            var contRep = "rep1";
            var fileName = "file1.txt";
            var filePath = "temp/file1.txt";
            var component = new SapDocumentComponentModel
            {
                CompId = compId,
                FileName = fileName,
                Data = new MemoryStream()
            };
            var model = new CreateSapDocumentModel
            {
                DocId = docId,
                ContRep = contRep,
                CompId = compId,
                PVersion = "001",
                ContentLength = "100",
                Components = new List<SapDocumentComponentModel> { component }
            };

            _trimRepoMock.Setup(x => x.GetRecord(docId, contRep)).Returns(_archiveRecordMock.Object);
            _archiveRecordMock.Setup(x => x.FindComponentById(compId)).Returns(_recordSapComponentMock.Object);
            _downloadFileHandlerMock.Setup(x => x.DownloadDocument(component.Data, fileName)).ReturnsAsync(filePath);
            _responseFactoryMock.Setup(x => x.CreateProtocolText(It.IsAny<string>(), 200, It.IsAny<string>()))
                .Returns(Mock.Of<ICommandResponse>());

            // Act
            var result = await _service.UpdateRecord(model, false);

            // Assert
            Assert.IsNotNull(result);
            _archiveRecordMock.Verify(x => x.UpdateComponent(_recordSapComponentMock.Object, component), Times.Once);
            _archiveRecordMock.Verify(x => x.SetRecordMetadata(), Times.Once);
            _archiveRecordMock.Verify(x => x.Save(), Times.Once);
            _downloadFileHandlerMock.Verify(x => x.DeleteFile(fileName), Times.Once);
        }

        [Test]
        public async Task UpdateRecord_RecordNotFound_ReturnsError()
        {
            // Arrange
            var model = new CreateSapDocumentModel
            {
                DocId = "doc1",
                ContRep = "rep1",
                CompId = "comp1",
                PVersion = "001",
                ContentLength = "100",
                Components = new List<SapDocumentComponentModel>()
            };
            _trimRepoMock.Setup(x => x.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns((IArchiveRecord)null);

            var errorMessage = "Document doc1 not found";
            _messageProviderMock.Setup(m => m.GetMessage(MessageIds.sap_documentNotFound, It.IsAny<string[]>()))
                .Returns(errorMessage);

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(x => x.CreateError(errorMessage, It.IsAny<int>()))
                .Returns(errorResponse);

            // Act
            var result = await _service.UpdateRecord(model, false);

            // Assert
            Assert.IsNotNull(result);
            Assert.That(result, Is.EqualTo(errorResponse));
        }


        [Test]
        public async Task UpdateRecord_ComponentNotFound_ReturnsError()
        {
            // Arrange
            var compId = "comp1";
            var docId = "doc1";
            var contRep = "rep1";
            var component = new SapDocumentComponentModel
            {
                CompId = compId,
                FileName = "file1.txt",
                Data = new MemoryStream()
            };
            var model = new CreateSapDocumentModel
            {
                DocId = docId,
                ContRep = contRep,
                CompId = compId,
                PVersion = "001",
                ContentLength = "100",
                Components = new List<SapDocumentComponentModel> { component }
            };

            _trimRepoMock.Setup(x => x.GetRecord(docId, contRep)).Returns(_archiveRecordMock.Object);
            _archiveRecordMock.Setup(x => x.FindComponentById(compId)).Returns((IRecordSapComponent)null);

            var errorMessage = "Component comp1 not found in doc1";
            _messageProviderMock.Setup(m => m.GetMessage(MessageIds.sap_componentNotFound, It.IsAny<string[]>()))
                .Returns(errorMessage);

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(x => x.CreateError(errorMessage, It.IsAny<int>()))
                .Returns(errorResponse);

            // Act
            var result = await _service.UpdateRecord(model, false);

            // Assert
            Assert.IsNotNull(result);
            Assert.That(result, Is.EqualTo(errorResponse));
        }


        [Test]
        public async Task UpdateRecord_MissingComponentId_ReturnsError()
        {
            // Arrange
            var component = new SapDocumentComponentModel
            {
                CompId = null,
                FileName = "file1.txt",
                Data = new MemoryStream()
            };
            var model = new CreateSapDocumentModel
            {
                DocId = "doc1",
                ContRep = "rep1",
                CompId = null,
                PVersion = "001",
                ContentLength = "100",
                Components = new List<SapDocumentComponentModel> { component }
            };

            _trimRepoMock.Setup(x => x.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(_archiveRecordMock.Object);
            _responseFactoryMock.Setup(x => x.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Mock.Of<ICommandResponse>());

            // Act
            var result = await _service.UpdateRecord(model, false);

            // Assert
            Assert.IsNotNull(result);
            _responseFactoryMock.Verify(x => x.CreateError(It.Is<string>(s => s.Contains("CompId is required.")), It.IsAny<int>()), Times.Once);
        }

        [Test]
        public async Task UpdateRecord_ReturnsError_WhenComponentCompIdIsNull()
        {
            // Arrange
            var component = new SapDocumentComponentModel
            {
                CompId = null,
                FileName = "file1.txt",
                Data = new MemoryStream()
            };
            var model = new CreateSapDocumentModel
            {
                DocId = "doc1",
                ContRep = "rep1",
                CompId = "comp1",
                PVersion = "001",
                ContentLength = "100",
                Components = new List<SapDocumentComponentModel> { component }
            };

            _trimRepoMock.Setup(x => x.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(_archiveRecordMock.Object);
            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(x => x.CreateError("Component ID was not specified", StatusCodes.Status400BadRequest))
                .Returns(errorResponse);

            var result = await _service.UpdateRecord(model, false);

            Assert.That(result, Is.EqualTo(errorResponse));
            _responseFactoryMock.Verify(x => x.CreateError("Component ID was not specified", StatusCodes.Status400BadRequest), Times.Once);
        }


        [Test]
        public async Task UpdateRecord_FailedToSaveComponentFile_ReturnsError()
        {
            // Arrange
            var compId = "comp1";
            var docId = "doc1";
            var contRep = "rep1";
            var fileName = "file1.txt";
            var component = new SapDocumentComponentModel
            {
                CompId = compId,
                FileName = fileName,
                Data = new MemoryStream()
            };
            var model = new CreateSapDocumentModel
            {
                DocId = docId,
                ContRep = contRep,
                CompId = compId,
                PVersion = "001",
                ContentLength = "100",
                Components = new List<SapDocumentComponentModel> { component }
            };

            _trimRepoMock.Setup(x => x.GetRecord(docId, contRep)).Returns(_archiveRecordMock.Object);
            _archiveRecordMock.Setup(x => x.FindComponentById(compId)).Returns(_recordSapComponentMock.Object);
            _downloadFileHandlerMock.Setup(x => x.DownloadDocument(component.Data, fileName)).ReturnsAsync((string)null);
            _responseFactoryMock.Setup(x => x.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Mock.Of<ICommandResponse>());

            // Act
            var result = await _service.UpdateRecord(model, false);

            // Assert
            Assert.IsNotNull(result);
            _responseFactoryMock.Verify(x => x.CreateError(It.Is<string>(s => s.Contains("Failed to save component file")), It.IsAny<int>()), Times.Once);
        }

        #endregion
    }
}