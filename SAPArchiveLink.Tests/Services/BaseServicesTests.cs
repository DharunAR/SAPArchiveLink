using Microsoft.AspNetCore.Http;
using Moq;
using System.Reflection.Emit;
using System.Text;
using TRIM.SDK;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class BaseServicesTests
    {
        private Mock<ILogHelper<BaseServices>> _loggerMock;
        private Mock<ILogHelper<CounterService>> _counterLoggerMock;
        private Mock<ICommandResponseFactory> _responseFactoryMock;
        private Mock<IDatabaseConnection> _dbConnectionMock;
        private Mock<IDownloadFileHandler> _downloadFileHandlerMock;
        private Mock<ITrimRepository> _trimRepoMock;
        private Mock<IArchiveRecord> _archiveRecordMock;
        private Mock<IRecordSapComponent> _recordSapComponentMock;
        private Mock<ISdkMessageProvider> _messageProviderMock;
        private Mock<ICertificateFactory> _certificateFactoryMock;
        private Mock<ICounterCache> _counterCacheMock;
        private CounterService _counterService;
        private BaseServices _service;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogHelper<BaseServices>>();
            _counterLoggerMock = new Mock<ILogHelper<CounterService>>();
            _responseFactoryMock = new Mock<ICommandResponseFactory>();
            _dbConnectionMock = new Mock<IDatabaseConnection>();
            _trimRepoMock = new Mock<ITrimRepository>();
            _downloadFileHandlerMock = new Mock<IDownloadFileHandler>();
            _archiveRecordMock = new Mock<IArchiveRecord>();
            _recordSapComponentMock = new Mock<IRecordSapComponent>();
            _messageProviderMock = new Mock<ISdkMessageProvider>();
            _certificateFactoryMock = new Mock<ICertificateFactory>();
            _counterCacheMock = new Mock<ICounterCache>();
            _counterService = new CounterService(_counterCacheMock.Object, _counterLoggerMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(_trimRepoMock.Object);
            _counterCacheMock.Setup(c => c.GetOrCreate(It.IsAny<string>())).Returns(new ArchiveCounter());

            _service = new BaseServices(
                _loggerMock.Object,
                _responseFactoryMock.Object,
                _dbConnectionMock.Object,
                _downloadFileHandlerMock.Object,
                _messageProviderMock.Object,
                _certificateFactoryMock.Object, 
                _counterService
            );
        }

        #region PutCert Service tests

        [Test]
        public async Task PutCert_ReturnsError_WhenModelIsInvalid()
        {
            var model = new PutCertificateModel
            {
                AuthId = null, // Required, so invalid
                ContRep = "contRep",
                PVersion = "1.0",
                Stream = new MemoryStream()
            };
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>())).Returns(Mock.Of<ICommandResponse>());
             await _service.PutCert(model);
            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(msg => msg.Contains("authId is required.")), StatusCodes.Status400BadRequest), Times.Once);
        }

        [Test]
        public async Task PutCert_ReturnsError_WhenCertificateCannotBeRecognized()
        {
            var model = new PutCertificateModel
            {
                AuthId = "auth",
                ContRep = "contRep",
                PVersion = "1.0",
                Stream = new MemoryStream(new byte[0])
            };
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>())).Returns(Mock.Of<ICommandResponse>());
            await _service.PutCert(model);
            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(msg => msg.Contains("Certificate cannot be recognized")), StatusCodes.Status406NotAcceptable), Times.AtLeastOnce);
        }

        [Test]
        public async Task PutCert_WithValiddata__ReturnsError_WhenCertificateCannotBeRecognized()
        {
            var model = new PutCertificateModel
            {
                AuthId = "auth",
                ContRep = "contRep",
                PVersion = "1.0",
                Stream = new MemoryStream(new byte[] { 1, 2, 3 })
            };
            _trimRepoMock.Setup(f => f.SaveCertificate(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IArchiveCertificate>(), It.IsAny<string>())).Throws(new InvalidOperationException("An error occurred while saving certificate."));

            await _service.PutCert(model);
            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(msg => msg.Contains("An error occurred while saving certificate.")), StatusCodes.Status500InternalServerError), Times.AtLeastOnce);
        }
        [Test]
        public async Task PutCert_Success_ReturnsProtocolText()
        {
            var model = new PutCertificateModel
            {
                AuthId = "auth",
                ContRep = "contRep",
                Permissions="crud",
                PVersion = "1.0",
                Stream = new MemoryStream(new byte[] { 1, 2, 3 })
            };

            var mockFactory = new Mock<ICertificateFactory>();
            mockFactory.Setup(f => f.FromByteArray(It.IsAny<byte[]>()))
                       .Returns(Mock.Of<IArchiveCertificate>(c =>
                           c.getIssuerName() == "Mock Issuer" &&
                           c.ValidTill() == "2030-01-01"));

            _responseFactoryMock.Setup(f => f.CreateProtocolText(It.IsAny<string>(), 200, It.IsAny<string>())).Returns(Mock.Of<ICommandResponse>());
            _trimRepoMock.Setup(r => r.SaveCertificate(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<ArchiveCertificate>(), It.IsAny<string>()));
            await _service.PutCert(model);
            _responseFactoryMock.Verify(f => f.CreateProtocolText("Certificate published", 200, It.IsAny<string>()), Times.Once);
        }

        #endregion

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
            recordMock.Setup(r => r.ExtractComponentById("compX", true)).ReturnsAsync((SapDocumentComponentModel)null);

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
                PVersion = "0045",
                Data = new MemoryStream(),
                FileName = "file.pdf"
            };

            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.ExtractComponentById("comp1", true)).ReturnsAsync(component);

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
            recordMock.Setup(r => r.ExtractAllComponents(true)).ReturnsAsync(components);
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
            recordMock.Setup(r => r.ExtractComponentById("compX", true)).ReturnsAsync((SapDocumentComponentModel)null);
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
            recordMock.Setup(r => r.ExtractComponentById("comp1", true)).ReturnsAsync((SapDocumentComponentModel)null);
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
            recordMock.Setup(r => r.ExtractComponentById("comp1", true)).ReturnsAsync(component);
            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);

            var expectedResponse = new Mock<ICommandResponse>();
            expectedResponse.SetupProperty(r => r.ContentType, "application/pdf");
            _responseFactoryMock.Setup(f => f.CreateDocumentContent(It.IsAny<Stream>(), component.ContentType, StatusCodes.Status200OK, component.FileName))
                .Returns(expectedResponse.Object);

            var result = await _service.GetSapDocument(sapDoc);

            Assert.That(result, Is.EqualTo(expectedResponse.Object));
            _counterCacheMock.Verify(c => c.GetOrCreate("rep"), Times.AtLeastOnce);

            expectedResponse.Verify(r => r.AddHeader("Content-Disposition", "inline; filename=\"file.pdf\""), Times.Once);
        }

        [Test]
        public async Task GetSapDocument_ReturnsDocumentContent_OnSuccess_WithAttachmentDisposition()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc", ContRep = "rep", PVersion = "1", CompId = "comp1", FromOffset = 0, ToOffset = 0 };
            var component = new SapDocumentComponentModel
            {
                CompId = "comp1",
                ContentType = "text/plain",
                ContentLength = 100,
                Data = new MemoryStream(new byte[] { 1, 2, 3 }),
                FileName = "file.txt",
                Charset = "utf-8",
                Version = "1.0"
            };
            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.ExtractComponentById("comp1", true)).ReturnsAsync(component);
            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);

            var expectedResponse = new Mock<ICommandResponse>();
            expectedResponse.SetupProperty(r => r.ContentType, "text/plain");
            _responseFactoryMock.Setup(f => f.CreateDocumentContent(It.IsAny<Stream>(), component.ContentType, StatusCodes.Status200OK, component.FileName))
                .Returns(expectedResponse.Object);

            var result = await _service.GetSapDocument(sapDoc);

            Assert.That(result, Is.EqualTo(expectedResponse.Object));
            _counterCacheMock.Verify(c => c.GetOrCreate("rep"), Times.AtLeastOnce);

            expectedResponse.Verify(r => r.AddHeader("Content-Disposition", "attachment; filename=\"file.txt\""), Times.Once);
            expectedResponse.Verify(r => r.AddHeader("X-Content-Type-Options", "nosniff"), Times.Once);
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
            recordMock.Setup(r => r.ExtractComponentById("comp1", true)).ReturnsAsync(component);
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
            recordMock.Setup(r => r.ExtractComponentById("comp1", true)).ReturnsAsync(component);
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
            recordMock.Setup(r => r.ExtractComponentById("comp1", true)).ReturnsAsync(component);
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
            recordMock.Setup(r => r.ExtractComponentById("comp1", true)).ReturnsAsync(component);
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
            var component = new SapDocumentComponentModel { CompId = "data", Data = new MemoryStream(), FileName = "test.txt" };
            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.HasComponent("data")).Returns(true);
            recordMock.Setup(r => r.ExtractComponentById("data", true)).ReturnsAsync(component);
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
            var component = new SapDocumentComponentModel { CompId = "data1", Data = new MemoryStream(), FileName = "test.txt" };
            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.HasComponent("data")).Returns(false);
            recordMock.Setup(r => r.HasComponent("data1")).Returns(true);
            recordMock.Setup(r => r.ExtractComponentById("data1", true)).ReturnsAsync(component);
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

        [Test]
        public async Task GetSapDocument_CounterServiceThrows_DoesNotAffectResponse()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc", ContRep = "rep", PVersion = "1", CompId = "comp1" };
            var component = new SapDocumentComponentModel
            {
                CompId = "comp1",
                ContentType = "application/pdf",
                ContentLength = 100,
                Data = new MemoryStream(new byte[] { 1, 2, 3 }),
                FileName = "file.pdf"
            };
            var recordMock = new Mock<IArchiveRecord>();
            recordMock.Setup(r => r.ExtractComponentById("comp1", true)).ReturnsAsync(component);
            var repoMock = new Mock<ITrimRepository>();
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);

            var expectedResponse = new Mock<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateDocumentContent(It.IsAny<Stream>(), component.ContentType, StatusCodes.Status200OK, component.FileName))
                .Returns(expectedResponse.Object);

            _counterCacheMock.Setup(c => c.GetOrCreate("rep")).Throws(new Exception("Counter error"));

            var result = await _service.GetSapDocument(sapDoc);

            Assert.That(result, Is.EqualTo(expectedResponse.Object));
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
            _counterCacheMock.Verify(c => c.GetOrCreate("rep"), Times.AtLeastOnce);
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
            _responseFactoryMock.Setup(f => f.CreateError("Component ID was not specified", StatusCodes.Status400BadRequest))
                .Returns(errorResponse);

            var result = await _service.CreateRecord(model, true);

            _responseFactoryMock.Verify(f => f.CreateError("Component ID was not specified", StatusCodes.Status400BadRequest), Times.Once);
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
            Assert.That(result, Is.Not.Null);
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
           Assert.That(result, Is.Not.Null);
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
           Assert.That(result, Is.Not.Null);
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
           Assert.That(result, Is.Not.Null);
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
           Assert.That(result, Is.Not.Null);
            _responseFactoryMock.Verify(x => x.CreateError(It.Is<string>(s => s.Contains("Failed to save component file")), It.IsAny<int>()), Times.Once);
        }

        #endregion

        #region Delete ServiceTests

        [Test]
        public async Task DeleteSapDocument_ReturnsError_WhenValidationFails()
        {
            var sapDoc = new SapDocumentRequest { DocId = null, ContRep = null, PVersion = null };
            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>())).Returns(errorResponse);

            var result = await _service.DeleteSapDocument(sapDoc);

            _responseFactoryMock.Verify(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task DeleteSapDocument_ReturnsError_WhenRecordNotFound()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc1", ContRep = "rep1", PVersion = "v1" };
            _trimRepoMock.Setup(r => r.GetRecord("doc1", "rep1")).Returns((IArchiveRecord)null);

            var errorMessage = "Document doc1 not found";
            var errorResponse = Mock.Of<ICommandResponse>();
            _messageProviderMock.Setup(m => m.GetMessage(MessageIds.sap_documentNotFound, It.IsAny<string[]>()))
                .Returns(errorMessage);
            _responseFactoryMock.Setup(f => f.CreateError(errorMessage, StatusCodes.Status404NotFound)).Returns(errorResponse);

            var result = await _service.DeleteSapDocument(sapDoc);

            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task DeleteSapDocument_DeletesRecord_WhenNoCompId()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc1", ContRep = "rep1", PVersion = "v1" };
            _trimRepoMock.Setup(r => r.GetRecord("doc1", "rep1")).Returns(_archiveRecordMock.Object);

            var successResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateProtocolText(It.Is<string>(s => s.Contains("deleted successfully")), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(successResponse);

            var result = await _service.DeleteSapDocument(sapDoc);

            _archiveRecordMock.Verify(r => r.DeleteRecord(), Times.Once);
            Assert.That(result, Is.EqualTo(successResponse));
        }

        [Test]
        public async Task DeleteSapDocument_DeletesComponent_WhenCompIdProvided_AndExists()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc1", ContRep = "rep1", PVersion = "v1", CompId = "comp1" };
            _trimRepoMock.Setup(r => r.GetRecord("doc1", "rep1")).Returns(_archiveRecordMock.Object);
            _archiveRecordMock.Setup(r => r.DeleteComponent("comp1")).Returns(true);

            var successResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateProtocolText(It.Is<string>(s => s.Contains("deleted successfully")), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(successResponse);

            var result = await _service.DeleteSapDocument(sapDoc);

            _archiveRecordMock.Verify(r => r.DeleteComponent("comp1"), Times.Once);
            _archiveRecordMock.Verify(r => r.SetRecordMetadata(), Times.Once);
            _archiveRecordMock.Verify(r => r.Save(), Times.Once);
            Assert.That(result, Is.EqualTo(successResponse));
        }

        [Test]
        public async Task DeleteSapDocument_ReturnsError_WhenComponentNotFound()
        {
            var sapDoc = new SapDocumentRequest { DocId = "doc1", ContRep = "rep1", PVersion = "v1", CompId = "compX" };
            _trimRepoMock.Setup(r => r.GetRecord("doc1", "rep1")).Returns(_archiveRecordMock.Object);
            _archiveRecordMock.Setup(r => r.DeleteComponent("compX")).Returns(false);

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError(It.Is<string>(s => s.Contains("not found")), StatusCodes.Status404NotFound))
                .Returns(errorResponse);

            var result = await _service.DeleteSapDocument(sapDoc);

            _archiveRecordMock.Verify(r => r.DeleteComponent("compX"), Times.Once);
            Assert.That(result, Is.EqualTo(errorResponse));
        }

        #endregion

        #region Info ServiceTests

        [Test]
        public async Task GetDocumentInfo_InvalidModel_ReturnsValidationError()
        {
            var request = new SapDocumentRequest()
            {
                DocId = null,
                CompId = "123",
                PVersion = "0045",
                ContRep = null
            };
            var expectedResponseMock = new Mock<ICommandResponse>();

            _responseFactoryMock
                .Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status400BadRequest))
                .Returns(expectedResponseMock.Object);

            // Act
            var result = await _service.GetDocumentInfo(request);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(expectedResponseMock.Object));
        }


        [Test]
        public async Task GetDocumentInfo_DocumentNotFound_Returns404()
        {
            var request = GetValidRequest();

            _trimRepoMock.Setup(r => r.GetRecord("DOC123", "CR1")).Returns<IArchiveRecord>(null);

            _messageProviderMock.Setup(m => m.GetMessage(MessageIds.sap_documentNotFound, It.IsAny<string[]>()))
                .Returns("Document not found");

            var expectedResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError("Document not found", StatusCodes.Status404NotFound))
                .Returns(expectedResponse);

            var result = await _service.GetDocumentInfo(request);

            Assert.That(result, Is.EqualTo(expectedResponse));
        }

        [Test]
        public async Task GetDocumentInfo_ComponentNotFound_Returns404()
        {
            var request = GetValidRequest(compId: "C1");

            _trimRepoMock.Setup(r => r.GetRecord("DOC123", "CR1")).Returns(_archiveRecordMock.Object);
            _archiveRecordMock.Setup(r => r.ExtractComponentById("C1", false))
                .ReturnsAsync((SapDocumentComponentModel?)null);

            _messageProviderMock.Setup(m => m.GetMessage(MessageIds.sap_componentNotFound, It.IsAny<string[]>()))
                .Returns("Component not found");

            var expectedResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError("Component not found", StatusCodes.Status404NotFound))
                .Returns(expectedResponse);

            var result = await _service.GetDocumentInfo(request);
            Assert.That(result, Is.EqualTo(expectedResponse));
        }


        [Test]
        public async Task GetDocumentInfo_SingleComponentInfo_ReturnsInfoMetadata()
        {
            var request = GetValidRequest(compId: "C1");
            var component = new SapDocumentComponentModel
            {
                CompId = "C1",
                ContentLength = 123,
                ContentType = "application/pdf",
                CreationDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Status = "active"
            };

            _trimRepoMock.Setup(r => r.GetRecord("DOC123", "CR1")).Returns(_archiveRecordMock.Object);
            _archiveRecordMock.Setup(r => r.ExtractComponentById("C1", false)).ReturnsAsync(component);

            var expectedResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateInfoMetadata(It.Is<List<SapDocumentComponentModel>>(list => list.Count == 1), StatusCodes.Status200OK))
                .Returns(expectedResponse);

            var result = await _service.GetDocumentInfo(request);

            Assert.That(result, Is.EqualTo(expectedResponse));
        }

        [Test]
        public async Task GetDocumentInfo_HtmlSingleComponent_ReturnsHtml()
        {
            var request = GetValidRequest(compId: "C1", resultAs: "html");

            var component = new SapDocumentComponentModel
            {
                CompId = "C1",
                ContentLength = 100,
                ContentType = "application/pdf",
                CreationDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Status = "active"
            };

            _trimRepoMock.Setup(r => r.GetRecord("DOC123", "CR1")).Returns(_archiveRecordMock.Object);
            _archiveRecordMock.Setup(r => r.ExtractComponentById("C1", false)).ReturnsAsync(component);

            var expectedResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateHtmlReport(It.Is<string>(s => s.Contains("<html>")), StatusCodes.Status200OK, "UTF-8"))
                .Returns(expectedResponse);

            var result = await _service.GetDocumentInfo(request);

            Assert.That(result, Is.EqualTo(expectedResponse));
        }

        [Test]
        public async Task GetDocumentInfo_HtmlMultiComponent_ReturnsHtml()
        {
            var request = GetValidRequest(resultAs: "html");

            var components = new List<SapDocumentComponentModel>
    {
        new SapDocumentComponentModel
        {
            CompId = "C1",
            ContentLength = 100,
            ContentType = "application/pdf",
            CreationDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            Status = "active"
        },
        new SapDocumentComponentModel
        {
            CompId = "C2",
            ContentLength = 150,
            ContentType = "image/png",
            CreationDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            Status = "archived"
        }
    };

            _trimRepoMock.Setup(r => r.GetRecord("DOC123", "CR1")).Returns(_archiveRecordMock.Object);
            _archiveRecordMock.Setup(r => r.GetAllComponents()).Returns(components);

            var expectedResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateHtmlReport(It.Is<string>(s => s.Contains("<html>")), StatusCodes.Status200OK, "UTF-8"))
                .Returns(expectedResponse);

            var result = await _service.GetDocumentInfo(request);

            Assert.That(result, Is.EqualTo(expectedResponse));
        }

        [Test]
        public async Task GetDocumentInfo_WithMultipleComponents_ReturnsInfoResponse()
        {
            var docId = "DOC001";
            var contRep = "REP001";

            var sapDoc = new SapDocumentRequest
            {
                DocId = docId,
                ContRep = contRep,
                ResultAs = null, // not "html"
                CompId = null,   // triggers multi-part logic
                PVersion = "0045"
            };

            var component1 = new SapDocumentComponentModel
            {
                CompId = "C1",
                ContentType = "application/pdf",
                ContentLength = 12345,
                Status = "online",
                CreationDate = DateTime.UtcNow.AddDays(-1),
                ModifiedDate = DateTime.UtcNow
            };

            var component2 = new SapDocumentComponentModel
            {
                CompId = "C2",
                ContentType = "image/png",
                ContentLength = 45678,
                Status = "online",
                CreationDate = DateTime.UtcNow.AddDays(-2),
                ModifiedDate = DateTime.UtcNow.AddDays(-1)
            };

            var components = new List<SapDocumentComponentModel> { component1, component2 };

            var expectedResponse = new Mock<ICommandResponse>();

            _archiveRecordMock
                .Setup(r => r.ExtractAllComponents(false))
                .ReturnsAsync(components);

            _archiveRecordMock
                .Setup(r => r.DateCreated)
                .Returns(DateTime.UtcNow.AddDays(-5));
            _archiveRecordMock
                .Setup(r => r.DateModified)
                .Returns(DateTime.UtcNow);
            _archiveRecordMock
                .Setup(r => r.ComponentCount)
                .Returns(2);

            _trimRepoMock
                .Setup(t => t.GetRecord(docId, contRep))
                .Returns(_archiveRecordMock.Object);

            _responseFactoryMock
                .Setup(f => f.CreateInfoMetadata(components, 200))
                .Returns(expectedResponse.Object);

            var result = await _service.GetDocumentInfo(sapDoc);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(expectedResponse.Object));

            _archiveRecordMock.Verify(r => r.ExtractAllComponents(false), Times.Once);
            _responseFactoryMock.Verify(f => f.CreateInfoMetadata(components, 200), Times.Once);
        }
        private SapDocumentRequest GetValidRequest(string? compId = null, string? resultAs = null)
        {
            return new SapDocumentRequest
            {
                DocId = "DOC123",
                ContRep = "CR1",
                CompId = compId,
                ResultAs = resultAs,
                PVersion = "001"
            };
        }

        #endregion

        #region GetSearchResult

        [Test]
        public async Task GetSearchResult_ReturnsError_WhenValidationFails()
        {
            // Arrange
            var request = new SapSearchRequestModel { DocId = "", CompId = "", PVersion = "",ContRep="",Pattern="" };
            _responseFactoryMock
                .Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Mock.Of<ICommandResponse>());

            // Act
            var result = await _service.GetSearchResult(request);

            // Assert
            _responseFactoryMock.Verify(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }

        [Test]
        public async Task GetSearchResult_ReturnsError_WhenRecordNotFound()
        {
            // Arrange
            var request = new SapSearchRequestModel { DocId = "doc1", CompId = "comp1", PVersion = "v1", ContRep = "ST", Pattern = "Test" };
            var repoMock = new Mock<ITrimRepository>();
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns((IArchiveRecord)null);
            _responseFactoryMock
                .Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Mock.Of<ICommandResponse>());

            // Act
            var result = await _service.GetSearchResult(request);

            // Assert
            _responseFactoryMock.Verify(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }

        [Test]
        public async Task GetSearchResult_ReturnsError_WhenComponentNotFound()
        {
            // Arrange
            var request = new SapSearchRequestModel {DocId = "doc1", CompId = "comp1", PVersion = "v1", ContRep = "ST", Pattern = "Test" ,};
            var repoMock = new Mock<ITrimRepository>();
            var recordMock = new Mock<IArchiveRecord>();
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            recordMock.Setup(r => r.ExtractComponentById(It.IsAny<string>(), true)).ReturnsAsync((SapDocumentComponentModel)null);
            _responseFactoryMock
                .Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Mock.Of<ICommandResponse>());

            // Act
            var result = await _service.GetSearchResult(request);

            // Assert
            _responseFactoryMock.Verify(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }

        [Test]
        public async Task GetSearchResult_ReturnsError_WhenExtractorNotFound()
        {
            // Arrange
            var request = new SapSearchRequestModel {DocId = "doc1", CompId = "comp1", PVersion = "v1", ContRep = "ST", Pattern = "Test" };
            var repoMock = new Mock<ITrimRepository>();
            var recordMock = new Mock<IArchiveRecord>();
            var component = new SapDocumentComponentModel
            {
                ContentType = "unknown/type",
                Data = new MemoryStream()
            };
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            recordMock.Setup(r => r.ExtractComponentById(It.IsAny<string>(), true)).ReturnsAsync(component);

            // Act & Assert
            Assert.ThrowsAsync<NotSupportedException>(async () => await _service.GetSearchResult(request));
        }

        [Test]
        public async Task GetSearchResult_ReturnsProtocolText_WhenSearchSucceeds()
        {
            // Arrange
            var request = new SapSearchRequestModel { DocId = "doc1", CompId = "comp1", PVersion = "v1", Pattern = "test",ContRep= "ST"};
            var repoMock = new Mock<ITrimRepository>();
            var recordMock = new Mock<IArchiveRecord>();
            var component = new SapDocumentComponentModel
            {
                ContentType = "text/plain",
                Data = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("this is a test string with test"))
            };
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            recordMock.Setup(r => r.ExtractComponentById(It.IsAny<string>(), true)).ReturnsAsync(component);

            // Patch the TextExtractorFactory for this test
            TextExtractorFactory.Register("text/plain", new PlainTextExtractor());

            var responseMock = new Mock<ICommandResponse>();
            _responseFactoryMock
                .Setup(f => f.CreateProtocolText(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(responseMock.Object);

            // Act
            var result = await _service.GetSearchResult(request);

            // Assert
            _responseFactoryMock.Verify(f => f.CreateProtocolText(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task GetSearchResult_ReturnsProtocolWord_WhenSearchSucceeds()
        {
            // Arrange
            var request = new SapSearchRequestModel { DocId = "doc1", CompId = "comp1", PVersion = "v1", Pattern = "test", ContRep = "ST" };
            var repoMock = new Mock<ITrimRepository>();
            var recordMock = new Mock<IArchiveRecord>();

            string filePath = Path.Combine(AppContext.BaseDirectory, "TestDocuments", "Test.docx");
            await using var fileStream = File.OpenRead(filePath);
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var component = new SapDocumentComponentModel
            {
                ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                Data = memoryStream
            };
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            recordMock.Setup(r => r.ExtractComponentById(It.IsAny<string>(), true)).ReturnsAsync(component);

            // Patch the TextExtractorFactory for this test
            TextExtractorFactory.Register("application/vnd.openxmlformats-officedocument.wordprocessingml.document", new DocxTextExtractor());

            var responseMock = new Mock<ICommandResponse>();
            _responseFactoryMock
                .Setup(f => f.CreateProtocolText(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(responseMock.Object);

            // Act
            var result = await _service.GetSearchResult(request);

            // Assert
            _responseFactoryMock.Verify(f => f.CreateProtocolText(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task GetSearchResult_ReturnsProtocolPDF_WhenSearchSucceeds()
        {
            // Arrange
            var request = new SapSearchRequestModel { DocId = "doc1", CompId = "comp1", PVersion = "v1", Pattern = "test", ContRep = "ST" };
            var repoMock = new Mock<ITrimRepository>();
            var recordMock = new Mock<IArchiveRecord>();

            string filePath = Path.Combine(AppContext.BaseDirectory, "TestDocuments", "Test.pdf");
            await using var fileStream = File.OpenRead(filePath);
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var component = new SapDocumentComponentModel
            {
                ContentType = "application/pdf",
                Data = memoryStream
            };
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            recordMock.Setup(r => r.ExtractComponentById(It.IsAny<string>(), true)).ReturnsAsync(component);

            // Patch the TextExtractorFactory for this test
            TextExtractorFactory.Register("application/pdf", new PdfTextExtractor());

            var responseMock = new Mock<ICommandResponse>();
            _responseFactoryMock
                .Setup(f => f.CreateProtocolText(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(responseMock.Object);

            // Act
            var result = await _service.GetSearchResult(request);

            // Assert
            _responseFactoryMock.Verify(f => f.CreateProtocolText(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task GetSearchResult_ReturnsProtocolEXCEL_WhenSearchSucceeds()
        {
            // Arrange
            var request = new SapSearchRequestModel { DocId = "doc1", CompId = "comp1", PVersion = "v1", Pattern = "test", ContRep = "ST" };
            var repoMock = new Mock<ITrimRepository>();
            var recordMock = new Mock<IArchiveRecord>();

            string filePath = Path.Combine(AppContext.BaseDirectory, "TestDocuments", "Test.xlsx");
            await using var fileStream = File.OpenRead(filePath);
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var component = new SapDocumentComponentModel
            {
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Data = memoryStream
            };
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            recordMock.Setup(r => r.ExtractComponentById(It.IsAny<string>(), true)).ReturnsAsync(component);

            // Patch the TextExtractorFactory for this test
            TextExtractorFactory.Register("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", new ExcelTextExtractor());

            var responseMock = new Mock<ICommandResponse>();
            _responseFactoryMock
                .Setup(f => f.CreateProtocolText(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(responseMock.Object);

            // Act
            var result = await _service.GetSearchResult(request);

            // Assert
            _responseFactoryMock.Verify(f => f.CreateProtocolText(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task GetSearchResult_ReturnsProtocolText_WhenSearchSucceeds_WithFromoffset()
        {
            // Arrange
            var request = new SapSearchRequestModel { DocId = "doc1", CompId = "comp1", PVersion = "v1", Pattern = "test", ContRep = "ST",FromOffset=5 };
            var repoMock = new Mock<ITrimRepository>();
            var recordMock = new Mock<IArchiveRecord>();
            var component = new SapDocumentComponentModel
            {
                ContentType = "text/plain",
                Data = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("this is a test string with test"))
            };
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(repoMock.Object);
            repoMock.Setup(r => r.GetRecord(It.IsAny<string>(), It.IsAny<string>())).Returns(recordMock.Object);
            recordMock.Setup(r => r.ExtractComponentById(It.IsAny<string>(), true)).ReturnsAsync(component);

            // Patch the TextExtractorFactory for this test
            TextExtractorFactory.Register("text/plain", new PlainTextExtractor());

            var responseMock = new Mock<ICommandResponse>();
            _responseFactoryMock
                .Setup(f => f.CreateProtocolText(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(responseMock.Object);

            // Act
            var result = await _service.GetSearchResult(request);

            // Assert
            _responseFactoryMock.Verify(f => f.CreateProtocolText(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region AppendDocument

        [Test]
        public async Task AppendDocument_ReturnsError_WhenModelIsInvalid()
        {
            // Arrange
            var model = new AppendSapDocCompModel
            {
                DocId = null, // Required, so invalid
                ContRep = "rep1",
                CompId = "comp1",
                PVersion = "1.0",
                StreamData = new MemoryStream(new byte[] { 1, 2, 3 })
            };
            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>())).Returns(errorResponse);

            // Act
            var result = await _service.AppendDocument(model);

            // Assert
            _responseFactoryMock.Verify(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task AppendDocument_ReturnsError_WhenRecordNotFound()
        {
            // Arrange
            var model = new AppendSapDocCompModel
            {
                DocId = "doc1",
                ContRep = "rep1",
                CompId = "comp1",
                PVersion = "1.0",
                StreamData = new MemoryStream(new byte[] { 1, 2, 3 })
            };
            _trimRepoMock.Setup(r => r.GetRecord("doc1", "rep1")).Returns((IArchiveRecord)null);
            var errorMessage = "Document doc1 not found";
            _messageProviderMock.Setup(m => m.GetMessage(MessageIds.sap_documentNotFound, It.IsAny<string[]>()))
                .Returns(errorMessage);
            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError(errorMessage, StatusCodes.Status404NotFound)).Returns(errorResponse);

            // Act
            var result = await _service.AppendDocument(model);

            // Assert
            Assert.That(result, Is.EqualTo(errorResponse));
        }

        [Test]
        public async Task AppendDocument_ReturnsError_WhenComponentIdIsNull()
        {            
            var model = new AppendSapDocCompModel
            {
                DocId = "doc1",
                ContRep = "rep1",
                CompId = null,
                PVersion = "1.0",
                StreamData = new MemoryStream(new byte[] { 1, 2, 3 })
            };          

            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>())).Returns(Mock.Of<ICommandResponse>());
            
            var result = await _service.AppendDocument(model);          
            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(msg => msg.Contains("CompId is required.")), StatusCodes.Status400BadRequest), Times.Once);          
        }

        [Test]
        public async Task AppendDocument_ReturnsError_WhenDocIdIsNull()
        {
            var model = new AppendSapDocCompModel
            {
                DocId = null,
                ContRep = "rep1",
                CompId = "compId",
                PVersion = "1.0",
                StreamData = new MemoryStream(new byte[] { 1, 2, 3 })
            };

            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>())).Returns(Mock.Of<ICommandResponse>());

            var result = await _service.AppendDocument(model);
            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(msg => msg.Contains("docId is required.")), StatusCodes.Status400BadRequest), Times.Once);
        }

        [Test]
        public async Task AppendDocument_ReturnsError_WhenContRepIsNull()
        {
            var model = new AppendSapDocCompModel
            {
                DocId = "docId",
                ContRep = null,
                CompId = "compId",
                PVersion = "1.0",
                StreamData = new MemoryStream(new byte[] { 1, 2, 3 })
            };

            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>())).Returns(Mock.Of<ICommandResponse>());

            var result = await _service.AppendDocument(model);
            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(msg => msg.Contains("contRep is required.")), StatusCodes.Status400BadRequest), Times.Once);
        }

        [Test]
        public async Task AppendDocument_AppendsComponent_Successfully()
        {
            // Arrange
            var model = new AppendSapDocCompModel
            {
                DocId = "doc1",
                ContRep = "rep1",
                CompId = "comp1",
                PVersion = "0046",
                StreamData = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("this is to be appended data")),
            };
            var component = new SapDocumentComponentModel
            {
                CompId = "comp1",
                ContentType = "text/plain",
                ContentLength = 123,
                CreationDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Status = "active",
                PVersion = "0045",
                Data = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("this is an original data")),
                FileName = "file.txt",
                RecordSapComponent = _recordSapComponentMock.Object

            };           
            _archiveRecordMock.Setup(r => r.ExtractComponentById("comp1", true)).ReturnsAsync(component);
            _trimRepoMock.Setup(r => r.GetRecord("doc1", "rep1")).Returns(_archiveRecordMock.Object);    
            _responseFactoryMock.Setup(f => f.CreateProtocolText(It.IsAny<string>(), StatusCodes.Status200OK, "UTF-8"))
                .Returns(Mock.Of<ICommandResponse>());
            DocumentAppenderFactory.Register(".txt", new TextDocumentAppender());

            _downloadFileHandlerMock.Setup(h => h.DownloadDocument(It.IsAny<Stream>(), It.IsAny<string>()))
             .ReturnsAsync("file.txt");
            // Act
            var result = await _service.AppendDocument(model);

            // Assert
            Assert.That(result, Is.Not.Null);
            _archiveRecordMock.Verify(r => r.SetRecordMetadata(), Times.Once);
            _archiveRecordMock.Verify(r => r.Save(), Times.Once);            
        }

        [Test]
        public async Task AppendDocument_ReturnsError_WhenFailed_InvalidFileType()
        {
            // Arrange
            var model = new AppendSapDocCompModel
            {
                DocId = "doc1",
                ContRep = "rep1",
                CompId = "comp1",
                PVersion = "0045",
                StreamData = new MemoryStream(new byte[] { 1, 2, 3 })
            };
            var component = new SapDocumentComponentModel
            {
                CompId = "comp1",
                ContentType = "text/bin",
                ContentLength = 123,
                CreationDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Status = "active",
                PVersion = "0045",
                Data = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("this is an original data")),
                FileName = "file.bin",
                RecordSapComponent = _recordSapComponentMock.Object

            };
            _archiveRecordMock.Setup(r => r.ExtractComponentById("comp1", true)).ReturnsAsync(component);
            _trimRepoMock.Setup(r => r.GetRecord("doc1", "rep1")).Returns(_archiveRecordMock.Object);
            var errorResponse = Mock.Of<ICommandResponse>();

            _responseFactoryMock.Setup(f => f.CreateError("Unsupported content type: text/bin", StatusCodes.Status404NotFound))
                .Returns(errorResponse);
         
            var result = await _service.AppendDocument(model);
            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(msg => msg.Contains("Unsupported content type: text/bin")), StatusCodes.Status404NotFound), Times.Once);

        }

        [Test]
        public async Task AppendDocument_ReturnsError_WhenFailedToSaveComponentFile()
        {
            // Arrange
            var model = new AppendSapDocCompModel
            {
                DocId = "doc1",
                ContRep = "rep1",
                CompId = "comp1",
                PVersion = "0045",
                StreamData = new MemoryStream(new byte[] { 1, 2, 3 })
            };
            var component = new SapDocumentComponentModel
            {
                CompId = "comp1",
                ContentType = "text/plain",
                ContentLength = 123,
                CreationDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Status = "active",
                PVersion = "0045",
                Data = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("this is an original data")),
                FileName = "file.txt",
                RecordSapComponent = _recordSapComponentMock.Object

            };
            _archiveRecordMock.Setup(r => r.ExtractComponentById("comp1", true)).ReturnsAsync(component);
            _trimRepoMock.Setup(r => r.GetRecord("doc1", "rep1")).Returns(_archiveRecordMock.Object);
            DocumentAppenderFactory.Register(".txt", new TextDocumentAppender());
            _downloadFileHandlerMock.Setup(h => h.DownloadDocument(model.StreamData, It.IsAny<string>()))
                .ReturnsAsync((string)null);
            var errorResponse = Mock.Of<ICommandResponse>();
            errorResponse.StatusCode = StatusCodes.Status400BadRequest;

            _responseFactoryMock.Setup(f => f.CreateError("Failed to save component file", StatusCodes.Status400BadRequest))
                .Returns(errorResponse);

            var result = await _service.AppendDocument(model);
            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(msg => msg.Contains("Failed to save component file")), StatusCodes.Status400BadRequest), Times.Once);

        }

        [Test]
        public async Task AppendDocument_ReturnsError_WhenFailed_ComponentIsNull()
        {
            // Arrange
            var model = new AppendSapDocCompModel
            {
                DocId = "doc1",
                ContRep = "rep1",
                CompId = "comp1",
                PVersion = "0045",
                StreamData = new MemoryStream(new byte[] { 1, 2, 3 })
            };
            var component = new SapDocumentComponentModel
            {
                CompId = "comp1",
                ContentType = "text/plain",
                ContentLength = 123,
                CreationDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Status = "active",
                PVersion = "0045",
                Data = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("this is an original data")),
                FileName = "file.txt",
                RecordSapComponent = _recordSapComponentMock.Object

            };
            _archiveRecordMock.Setup(r => r.ExtractComponentById("comp1", true)).ReturnsAsync((SapDocumentComponentModel)null);

            // Fix for CS0121: Specify the type explicitly in the ReturnsAsync method to resolve ambiguity.
            _archiveRecordMock.Setup(r => r.ExtractComponentById("comp1", true))
               .ReturnsAsync((SapDocumentComponentModel?)null);
            _trimRepoMock.Setup(r => r.GetRecord("doc1", "rep1")).Returns(_archiveRecordMock.Object);
        
           
            var errorResponse = Mock.Of<ICommandResponse>();
            errorResponse.StatusCode = StatusCodes.Status400BadRequest;

            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>())).Returns(errorResponse);

            var result = await _service.AppendDocument(model);        
                 

            _responseFactoryMock.Verify(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()), Times.Once);           

        }

        #endregion

        #region ServerInfo ServiceTests

        [Test]
        public async Task GetServerInfo_NoRepositoriesFound_ReturnsErrorResponse()
        {
            var contRep = "ABC";
            var version = "0046";
            var resultAs = "text";

            var emptyServerInfo = new ServerInfoModel();

            _trimRepoMock.Setup(repo => repo.GetServerInfo(version, contRep))
                         .Returns(emptyServerInfo);

            var expectedResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock
                .Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status404NotFound))
                .Returns(expectedResponse);

            var result = await _service.GetServerInfo(contRep, version, resultAs);

            _loggerMock.Verify(l => l.LogError(It.IsAny<string>(), null), Times.Once);
            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(msg => msg.Contains(contRep)), StatusCodes.Status404NotFound), Times.Once);
            Assert.That(result, Is.EqualTo(expectedResponse));
        }


        [Test]
        public async Task GetServerInfo_WithHtmlResult_ReturnsHtmlResponse()
        {
            var contRep = "XYZ";
            var version = "0046";
            var resultAs = "html";

            var serverInfo = new ServerInfoModel
            {
                ServerVendorId = "VendorX",
                ServerVersion = "1.0",
                ServerBuild = "123",
                ServerStatusDescription = "Running",
                PVersion = version,
                ContentRepositories = new List<ContentRepositoryInfoModel>
                {
                    new ContentRepositoryInfoModel
                    {
                        ContRep = "XYZ",
                        ContRepDescription = "Main Repo",
                        ContRepStatus = "Online",
                        ContRepStatusDescription = "Available",
                        PVersion = version
                    }
                }
            };

            _trimRepoMock.Setup(repo => repo.GetServerInfo(version, contRep)).Returns(serverInfo);

            var expectedResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock
                .Setup(f => f.CreateHtmlReport(It.Is<string>(html => html.Contains("Content Manager SAP ArchiveLink Status")), StatusCodes.Status200OK, "UTF-8"))
                .Returns(expectedResponse);

            var result = await _service.GetServerInfo(contRep, version, resultAs);

            _responseFactoryMock.Verify(f => f.CreateHtmlReport(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Once);
            Assert.That(result, Is.EqualTo(expectedResponse));
        }


        [Test]
        public async Task GetServerInfo_WithTextResult_ReturnsProtocolTextResponse()
        {
            var contRep = "XYZ";
            var version = "0046";
            string resultAs = null;

            var serverInfo = new ServerInfoModel
            {
                ServerVendorId = "VendorY",
                ServerVersion = "2.1",
                ServerBuild = "321",
                PVersion = version,
                ContentRepositories = new List<ContentRepositoryInfoModel>
                {
                    new ContentRepositoryInfoModel
                    {
                        ContRep = "XYZ",
                        ContRepDescription = "Secondary Repo",
                        ContRepStatus = "Online",
                        ContRepStatusDescription = "Stable",
                        PVersion = version
                    }
                }
            };

            _trimRepoMock.Setup(repo => repo.GetServerInfo(version, contRep)).Returns(serverInfo);

            var expectedResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock
                .Setup(f => f.CreateProtocolText(It.Is<string>(s => s.Contains("serverStatus=") && s.Contains("contRep=")), StatusCodes.Status200OK, "UTF-8"))
                .Returns(expectedResponse);

            var result = await _service.GetServerInfo(contRep, version, resultAs);

            _responseFactoryMock.Verify(f => f.CreateProtocolText(It.IsAny<string>(), StatusCodes.Status200OK, "UTF-8"), Times.Once);
            Assert.That(result, Is.EqualTo(expectedResponse));
        }

        #endregion

        #region AttrSearch ServiceTests

        [Test]
        public async Task GetAttrSearchResult_ShouldReturnMatch_WhenValidPatternProvided()
        {
            var descriptionData = @"
73 138 DAIN00100010147119BrothersInc
211 120 DAIN001000020147129ObelixInc
";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(descriptionData));

            var searchRequest = new SapSearchRequestModel
            {
                ContRep = "ST",
                DocId = "SAP_descr",
                CompId = "descr",
                Pattern = "0+3+001",
                CaseSensitive = false,
                NumResults = 2,
                FromOffset = 0,
                ToOffset = -1,
                PVersion = "0045"
            };
            var component = new SapDocumentComponentModel
            {
                CompId = "descr",
                ContentType = "text/plain",
                ContentLength = 123,
                CreationDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Status = "active",
                PVersion = "0045",
                Data = stream,
                FileName = "file.txt"
            };

            _archiveRecordMock.Setup(r => r.ExtractComponentById("descr", true)).ReturnsAsync(component);

            _trimRepoMock
                .Setup(r => r.GetRecord("SAP_descr", "ST"))
                .Returns(_archiveRecordMock.Object);

            _responseFactoryMock
                .Setup(f => f.CreateProtocolText("2;73;138;211;120;", StatusCodes.Status200OK, "UTF-8"))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _service.GetAttrSearchResult(searchRequest);

            Assert.That(result, Is.Not.Null);
            _responseFactoryMock.Verify(f => f.CreateProtocolText("2;73;138;211;120;", StatusCodes.Status200OK, "UTF-8"), Times.Once);
        }

        [Test]
        public async Task GetAttrSearchResult_ShouldReturnZero_WhenNoMatchingLinesFound()
        {
            var descriptionData = @"
73 138 DAIN99900010147119OtherName
211 120 DAIN999000020147129WrongName
";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(descriptionData));

            var searchRequest = new SapSearchRequestModel
            {
                ContRep = "ST",
                DocId = "SAP_descr",
                CompId = "descr",
                Pattern = "0+3+001",
                CaseSensitive = false,
                NumResults = 1,
                FromOffset = 0,
                ToOffset = -1,
                PVersion = "0045"
            };

            var component = new SapDocumentComponentModel { Data = stream, PVersion = "0045" };

            _archiveRecordMock.Setup(r => r.ExtractComponentById("descr", true)).ReturnsAsync(component);
            _trimRepoMock.Setup(r => r.GetRecord("SAP_descr", "ST")).Returns(_archiveRecordMock.Object);

            var expectedResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock
                .Setup(f => f.CreateProtocolText("0;", StatusCodes.Status200OK, "UTF-8"))
                .Returns(expectedResponse);

            var result = await _service.GetAttrSearchResult(searchRequest);

            Assert.That(result, Is.SameAs(expectedResponse));
        }

        [Test]
        public async Task GetAttrSearchResult_ShouldReturnBadRequest_WhenPatternFormatIsInvalid()
        {
            var invalidPattern = "invalid+pattern+string";
            var searchRequest = new SapSearchRequestModel
            {
                ContRep = "ST",
                DocId = "SAP_descr",
                CompId = "descr",
                Pattern = invalidPattern,
                CaseSensitive = false,
                NumResults = 1,
                FromOffset = 0,
                ToOffset = -1,
                PVersion = "0045"
            };

            var component = new SapDocumentComponentModel
            {
                Data = new MemoryStream(Encoding.UTF8.GetBytes("73 138 DAINbadline")),
                PVersion = "0045"
            };

            _archiveRecordMock.Setup(r => r.ExtractComponentById("descr", true)).ReturnsAsync(component);
            _trimRepoMock.Setup(r => r.GetRecord("SAP_descr", "ST")).Returns(_archiveRecordMock.Object);

            var expectedError = "Invalid pattern format";
            var expectedResponse = Mock.Of<ICommandResponse>();

            _responseFactoryMock
                .Setup(f => f.CreateError(expectedError, StatusCodes.Status400BadRequest))
                .Returns(expectedResponse);

            var result = await _service.GetAttrSearchResult(searchRequest);

            Assert.That(result, Is.SameAs(expectedResponse));
        }

        [Test]
        public async Task GetAttrSearchResult_ShouldReturnNotFound_WhenRecordIsMissing()
        {
            var searchRequest = new SapSearchRequestModel
            {
                ContRep = "ST",
                DocId = "SAP_descr",
                CompId = "descr",
                Pattern = "0+3+001",
                CaseSensitive = false,
                NumResults = 1,
                FromOffset = 0,
                ToOffset = -1,
                PVersion = "0045"
            };

            _trimRepoMock.Setup(r => r.GetRecord("SAP_descr", "ST")).Returns((IArchiveRecord?)null);

            var errorMessage = "Document SAP_descr not found";
            var errorResponse = Mock.Of<ICommandResponse>();

            _messageProviderMock
                .Setup(p => p.GetMessage(MessageIds.sap_documentNotFound, It.IsAny<string[]>()))
                .Returns(errorMessage);

            _responseFactoryMock
                .Setup(f => f.CreateError(errorMessage, StatusCodes.Status404NotFound))
                .Returns(errorResponse);

            var result = await _service.GetAttrSearchResult(searchRequest);

            Assert.That(result, Is.SameAs(errorResponse));
        }

        [Test]
        public async Task GetAttrSearchResult_ShouldReturnNotFound_WhenComponentIsMissing()
        {
            var searchRequest = new SapSearchRequestModel
            {
                ContRep = "ST",
                DocId = "SAP_descr",
                CompId = "descr",
                Pattern = "0+3+001",
                CaseSensitive = false,
                NumResults = 1,
                FromOffset = 0,
                ToOffset = -1,
                PVersion = "0045"
            };

            _trimRepoMock.Setup(r => r.GetRecord("SAP_descr", "ST")).Returns(_archiveRecordMock.Object);
            _archiveRecordMock.Setup(r => r.ExtractComponentById("descr", true)).ReturnsAsync((SapDocumentComponentModel?)null);

            var errorMessage = "Component descr not found for document SAP_descr";
            var errorResponse = Mock.Of<ICommandResponse>();

            _messageProviderMock
                .Setup(p => p.GetMessage(MessageIds.sap_componentNotFound, It.IsAny<string[]>()))
                .Returns(errorMessage);

            _responseFactoryMock
                .Setup(f => f.CreateError(errorMessage, StatusCodes.Status404NotFound))
                .Returns(errorResponse);

            var result = await _service.GetAttrSearchResult(searchRequest);

            Assert.That(result, Is.SameAs(errorResponse));
        }

        #endregion
    }
}