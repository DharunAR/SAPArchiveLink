using Microsoft.AspNetCore.Http;
using Moq;

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
        //private Mock<IBaseServices> _service;
        private BaseServices _service;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogHelper<BaseServices>>();
            _responseFactoryMock = new Mock<ICommandResponseFactory>();
            _dbConnectionMock = new Mock<IDatabaseConnection>();
            _trimRepoMock = new Mock<ITrimRepository>();
            _downloadFileHandlerMock = new Mock<IDownloadFileHandler>();

            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(_trimRepoMock.Object);

            _service = new BaseServices(
                _loggerMock.Object,
                _responseFactoryMock.Object,
                _dbConnectionMock.Object,
                _downloadFileHandlerMock.Object);
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

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError("Record not found", StatusCodes.Status404NotFound)).Returns(errorResponse);

            var result = await _service.DocGetSapComponents(sapDoc);

            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task DocGet_ReturnsError_WhenComponentNotFound()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc1", ContRep = "rep1", PVersion = "v1", CompId = "compX" };
            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.HasComponent("compX")).Returns(false);

            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError("Component 'compX' not found", StatusCodes.Status404NotFound)).Returns(errorResponse);


            var result = await _service.DocGetSapComponents(sapDoc);

            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task DocGet_ReturnsSingleComponentResponse_WhenCompIdProvidedAndExists()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc1", ContRep = "rep1", PVersion = "v1", CompId = "comp1" };
            var component = new SapDocumentComponent
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
            recordMock.Setup(r => r.HasComponent("comp1")).Returns(true);
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
            var components = new List<SapDocumentComponent>
            {
                new SapDocumentComponent { CompId = "comp1", ContentType = "application/pdf", Data = new MemoryStream() }
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
            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>())).Returns(errorResponse);

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
            recordMock.Setup(r => r.HasComponent("compX")).Returns(false);
            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError("Component 'compX' not found", StatusCodes.Status404NotFound)).Returns(errorResponse);

            var result = await _service.GetSapDocument(sapDoc);

            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task GetSapDocument_ReturnsError_WhenComponentCouldNotBeLoaded()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc", ContRep = "rep", PVersion = "1", CompId = "comp1" };
            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.HasComponent("comp1")).Returns(true);
            recordMock.Setup(r => r.ExtractComponentById("comp1")).ReturnsAsync((SapDocumentComponent)null);
            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError("Component could not be loaded", StatusCodes.Status500InternalServerError)).Returns(errorResponse);

            var result = await _service.GetSapDocument(sapDoc);

            Assert.That(result, Is.EqualTo(errorResponse));
        }


        [Test]
        public async Task GetSapDocument_ReturnsDocumentContent_OnSuccess()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc", ContRep = "rep", PVersion = "1", CompId = "comp1", FromOffset = 0, ToOffset = 0 };
            var component = new SapDocumentComponent
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
            var component = new SapDocumentComponent
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
            var component = new SapDocumentComponent
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
            var component = new SapDocumentComponent
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
            var component = new SapDocumentComponent
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


        #endregion

        [Test]
        public async Task CreateRecord_ReturnsError_WhenValidationFails()
        {
            var model = new CreateSapDocumentModel { DocId = null, ContRep = null, CompId = null, PVersion = null, ContentLength = null };
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _service.CreateRecord(model);

            _responseFactoryMock.Verify(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
            Assert.IsNotNull(result);
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
                Components = new List<SapDocumentComponent>
                    {
                        new SapDocumentComponent { CompId = "comp", FileName = "file.txt", Data = new MemoryStream(new byte[] { 1, 2 }) }
                    }
            };
            var repoMock = new Mock<ITrimRepository>();
            var recordMock = new Mock<IArchiveRecord>();
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns((IArchiveRecord)null);
            repoMock.Setup(r => r.CreateRecord(It.IsAny<CreateSapDocumentModel>())).Returns(recordMock.Object);
            _downloadFileHandlerMock.Setup(h => h.DownloadDocument(It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync("file.txt");
            _responseFactoryMock.Setup(f => f.CreateProtocolText(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _service.CreateRecord(model);

            _responseFactoryMock.Verify(f => f.CreateProtocolText(It.Is<string>(s => s.Contains("Component(s) created successfully.")), StatusCodes.Status201Created, "UTF-8"), Times.Once);
            Assert.IsNotNull(result);
        }
    }
}