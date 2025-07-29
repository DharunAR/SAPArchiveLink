using Microsoft.AspNetCore.Http;
using Moq;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class UpdatePostCommandHandlerTests
    {
        private Mock<ICommandResponseFactory> _responseFactoryMock;
        private Mock<IBaseServices> _baseServiceMock;
        private Mock<IDownloadFileHandler> _downloadFileHandlerMock;
        private UpdatePostCommandHandler _handler;
        private Mock<ICommand> _commandMock;
        private Mock<ICommandRequestContext> _contextMock;
        private Mock<HttpRequest> _httpRequestMock;

        [SetUp]
        public void SetUp()
        {
            _responseFactoryMock = new Mock<ICommandResponseFactory>();
            _baseServiceMock = new Mock<IBaseServices>();
            _downloadFileHandlerMock = new Mock<IDownloadFileHandler>();
            _handler = new UpdatePostCommandHandler(_responseFactoryMock.Object, _baseServiceMock.Object, _downloadFileHandlerMock.Object);
            _commandMock = new Mock<ICommand>();
            _contextMock = new Mock<ICommandRequestContext>();
            _httpRequestMock = new Mock<HttpRequest>();
        }

        [Test]
        public void CommandTemplate_Returns_UPDATE_POST()
        {
            Assert.That(_handler.CommandTemplate, Is.EqualTo(ALCommandTemplate.UPDATE_POST));
        }

        [Test]
        public async Task HandleAsync_ReturnsError_WhenContentTypeIsMissing()
        {
            _httpRequestMock.Setup(r => r.ContentType).Returns((string)null);
            _contextMock.Setup(c => c.GetRequest()).Returns(_httpRequestMock.Object);
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status400BadRequest))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(msg => msg.Contains("Content-Type")), StatusCodes.Status400BadRequest), Times.Once);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task HandleAsync_ReturnsBaseServiceResult_WhenContentTypeIsPresent()
        {
            var docId = "123";
            var contentType = "application/pdf";
            var bodyStream = new MemoryStream();
            var sapComponents = new List<SapDocumentComponentModel>();
            var commandResponse = Mock.Of<ICommandResponse>();

            _httpRequestMock.Setup(r => r.ContentType).Returns(contentType);
            _httpRequestMock.Setup(r => r.Body).Returns(bodyStream);
            _httpRequestMock.Setup(r => r.ContentLength).Returns(100);
            _httpRequestMock.Setup(r => r.Headers).Returns(new HeaderDictionary
            {
                { "charset", "UTF-8" },
                { "version", "1.0" },
                { "docprot", "prot" }
            });

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
            _baseServiceMock.Setup(s => s.UpdateRecord(It.IsAny<CreateSapDocumentModel>(), true))
                .ReturnsAsync(commandResponse);

            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            _baseServiceMock.Verify(s => s.UpdateRecord(It.Is<CreateSapDocumentModel>(m =>
                m.DocId == docId &&
                m.ContentType == contentType &&
                m.Components == sapComponents
            ), true), Times.Once);
            Assert.That(commandResponse, Is.EqualTo(result));
        }

        [Test]
        public async Task HandleAsync_ReturnsError_OnException()
        {
            _httpRequestMock.Setup(r => r.ContentType).Returns("application/pdf");
            _contextMock.Setup(c => c.GetRequest()).Returns(_httpRequestMock.Object);
            _commandMock.Setup(c => c.GetValue(ALParameter.VarDocId)).Throws(new Exception("Test exception"));
            _responseFactoryMock.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status500InternalServerError))
                .Returns(Mock.Of<ICommandResponse>());

            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(msg => msg.Contains("Test exception")), StatusCodes.Status500InternalServerError), Times.Once);
            Assert.That(result, Is.Not.Null);
        }
    }
}
