using Microsoft.AspNetCore.Http;
using Moq;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class UpdateCommandHandlerTests
    {
        private Mock<ICommandResponseFactory> _responseFactoryMock;
        private Mock<IBaseServices> _baseServiceMock;
        private Mock<IDownloadFileHandler> _downloadFileHandlerMock;
        private UpdateCommandHandler _handler;
        private Mock<ICommand> _commandMock;
        private Mock<ICommandRequestContext> _contextMock;
        private Mock<HttpRequest> _httpRequestMock;

        [SetUp]
        public void SetUp()
        {
            _responseFactoryMock = new Mock<ICommandResponseFactory>();
            _baseServiceMock = new Mock<IBaseServices>();
            _downloadFileHandlerMock = new Mock<IDownloadFileHandler>();
            _handler = new UpdateCommandHandler(_responseFactoryMock.Object, _baseServiceMock.Object, _downloadFileHandlerMock.Object);
            _commandMock = new Mock<ICommand>();
            _contextMock = new Mock<ICommandRequestContext>();
            _httpRequestMock = new Mock<HttpRequest>();
        }

        [Test]
        public async Task HandleAsync_ReturnsError_WhenContentTypeIsMissing()
        {
            _httpRequestMock.Setup(r => r.ContentType).Returns((string)null);
            _contextMock.Setup(c => c.GetRequest()).Returns(_httpRequestMock.Object);
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status400BadRequest))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(s => s.Contains("Content-Type")), StatusCodes.Status400BadRequest), Times.Once);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task HandleAsync_ReturnsBaseServiceResult_WhenValidRequest()
        {
            var docId = "123";
            var contentType = "application/pdf";
            var bodyStream = new MemoryStream();
            var sapComponents = new List<SapDocumentComponentModel> { new SapDocumentComponentModel() };
            var headers = new HeaderDictionary
            {
                { "charset", "UTF-8" },
                { "version", "1.0" },
                { "docprot", "prot" }
            };

            _httpRequestMock.Setup(r => r.ContentType).Returns(contentType);
            _httpRequestMock.Setup(r => r.Body).Returns(bodyStream);
            _httpRequestMock.Setup(r => r.ContentLength).Returns(100);
            _httpRequestMock.Setup(r => r.Headers).Returns(headers);
            _contextMock.Setup(c => c.GetRequest()).Returns(_httpRequestMock.Object);
            _commandMock.Setup(c => c.GetValue(ALParameter.VarDocId)).Returns(docId);
            _commandMock.Setup(c => c.GetValue(ALParameter.VarContRep)).Returns("contRep");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarCompId)).Returns("compId");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("pVersion");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarSecKey)).Returns("secKey");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAccessMode)).Returns("accessMode");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAuthId)).Returns("authId");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarExpiration)).Returns("expiration");

            _downloadFileHandlerMock.Setup(h => h.HandleRequestAsync(contentType, bodyStream, docId))
                .ReturnsAsync(sapComponents);

            var expectedResponse = Mock.Of<ICommandResponse>();
            _baseServiceMock.Setup(s => s.UpdateRecord(It.IsAny<CreateSapDocumentModel>(), false))
                .ReturnsAsync(expectedResponse);

            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            _baseServiceMock.Verify(s => s.UpdateRecord(It.IsAny<CreateSapDocumentModel>(), false), Times.Once);
            Assert.That(expectedResponse, Is.EqualTo(result));
        }

        [Test]
        public async Task HandleAsync_ReturnsError_WhenExceptionThrown()
        {
            _httpRequestMock.Setup(r => r.ContentType).Returns("application/pdf");
            _contextMock.Setup(c => c.GetRequest()).Returns(_httpRequestMock.Object);
            _commandMock.Setup(c => c.GetValue(ALParameter.VarDocId)).Throws(new Exception("Test exception"));
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status500InternalServerError))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(s => s.Contains("Test exception")), StatusCodes.Status500InternalServerError), Times.Once);
            Assert.That(result, Is.Not.Null);
        }
    }
}
