using Microsoft.AspNetCore.Http;
using Moq;


namespace SAPArchiveLink.Tests
{

    // The error is likely because Moq cannot mock non-virtual or non-abstract methods unless they are marked as virtual or the class is abstract/interface.
    // DownloadFileHandler.HandleRequestAsync is not virtual, so Moq cannot override it by default.
    // To fix this, you can either:
    // 1. Make HandleRequestAsync virtual in DownloadFileHandler, or
    // 2. Use a test double/fake instead of Moq for DownloadFileHandler, or
    // 3. Use Moq's .As<T>() for interfaces if DownloadFileHandler implements one.

    // Example fix: Make HandleRequestAsync virtual in DownloadFileHandler
    // public virtual async Task<List<SapDocumentComponentModel>> HandleRequestAsync(string contentType, Stream body, string docId);

    // OR: Use a stub class for testing
    //public class TestDownloadFileHandler : DownloadFileHandler
    //{
    //    private readonly List<SapDocumentComponentModel> _componentsToReturn;
    //    public TestDownloadFileHandler(string saveDirectory, List<SapDocumentComponentModel> componentsToReturn)
    //        : base(saveDirectory)
    //    {
    //        _componentsToReturn = componentsToReturn;
    //    }

    //    public override async Task<List<SapDocumentComponentModel>> HandleRequestAsync(string contentType, Stream body, string docId)
    //    {
    //        return await Task.FromResult(_componentsToReturn);
    //    }
     // }
    [TestFixture]
    public class CreatePutCommandHandlerTests
    {
        private Mock<ICommandResponseFactory> _responseFactoryMock;
        private Mock<IBaseServices> _baseServiceMock;
        private Mock<IDownloadFileHandler> _downloadFileHandlerMock;
        private CreatePutCommandHandler _handler;
        private Mock<ICommand> _commandMock;
        private Mock<ICommandRequestContext> _contextMock;
        private DefaultHttpContext _httpContext;
        private Mock<ICommandResponse> _commandResponseMock;

        [SetUp]
        public void SetUp()
        {
            _responseFactoryMock = new Mock<ICommandResponseFactory>();
            _baseServiceMock = new Mock<IBaseServices>();
            _downloadFileHandlerMock = new Mock<IDownloadFileHandler>();
            _handler = new CreatePutCommandHandler(_responseFactoryMock.Object, _baseServiceMock.Object, _downloadFileHandlerMock.Object);
            _commandMock = new Mock<ICommand>();
            _contextMock = new Mock<ICommandRequestContext>();
            _httpContext = new DefaultHttpContext();
            _commandResponseMock = new Mock<ICommandResponse>();
        }

        [Test]
        public void CommandTemplate_Returns_CREATEPUT()
        {
            Assert.That(_handler.CommandTemplate, Is.EqualTo(ALCommandTemplate.CREATEPUT));
        }

        [Test]
        public async Task HandleAsync_ReturnsError_WhenContentTypeIsMissing()
        {
            _contextMock.Setup(c => c.GetRequest()).Returns(_httpContext.Request);
            _httpContext.Request.ContentType = null;
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status400BadRequest))
                .Returns(_commandResponseMock.Object);

            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            Assert.That(result, Is.EqualTo(_commandResponseMock.Object));
            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(s => s.Contains("Content-Type")), StatusCodes.Status400BadRequest), Times.Once);
        }

        [Test]
        public async Task HandleAsync_CallsCreateRecord_WhenContentTypeIsPresent()
        {
            // Arrange
            var docId = "123";
            var contentType = "application/pdf";
            var compId = "comp1";
            var charset = "utf-8";
            var version = "1.0";
            var docprot = "prot";
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var sapComponent = new SapDocumentComponentModel();

            _contextMock.Setup(c => c.GetRequest()).Returns(_httpContext.Request);
            _httpContext.Request.ContentType = contentType;
            _httpContext.Request.Body = stream;
            _httpContext.Request.Headers["charset"] = charset;
            _httpContext.Request.Headers["version"] = version;
            _httpContext.Request.ContentLength = 3;

            _commandMock.Setup(c => c.GetValue(ALParameter.VarDocId)).Returns(docId);
            _commandMock.Setup(c => c.GetValue(ALParameter.VarContRep)).Returns("contRep");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarCompId)).Returns(compId);
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("pver");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarSecKey)).Returns("seckey");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAccessMode)).Returns("access");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAuthId)).Returns("authid");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarExpiration)).Returns("exp");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarDocProt)).Returns(docprot);

            _downloadFileHandlerMock
    .Setup(d => d.HandleRequestAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
    .ReturnsAsync(new List<SapDocumentComponentModel> { sapComponent });

            _baseServiceMock.Setup(b => b.CreateRecord(It.IsAny<CreateSapDocumentModel>(), false))
                .ReturnsAsync(_commandResponseMock.Object);

            // Act
            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            // Assert
            Assert.That(result, Is.EqualTo(_commandResponseMock.Object));
            _baseServiceMock.Verify(b => b.CreateRecord(It.Is<CreateSapDocumentModel>(m =>
                m.DocId == docId &&
                m.CompId == compId &&
                m.Charset == charset &&
                m.Version == version &&
                m.DocProt == docprot &&
                m.ContentType == contentType &&
                m.Stream == stream &&
                m.Components != null &&
                m.Components.First() == sapComponent
            ), false), Times.Once);
        }

        [Test]
        public async Task HandleAsync_ReturnsError_OnException()
        {
            _contextMock.Setup(c => c.GetRequest()).Throws(new Exception("fail"));
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status500InternalServerError))
                .Returns(_commandResponseMock.Object);

            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            Assert.That(result, Is.EqualTo(_commandResponseMock.Object));
            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(s => s.Contains("Internal server error")), StatusCodes.Status500InternalServerError), Times.Once);
        }
    }
}