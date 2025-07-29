using Microsoft.AspNetCore.Http;
using Moq;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class CreatePostCommandHandlerTests
    {
        private Mock<ICommandResponseFactory> _responseFactoryMock;
        private Mock<IBaseServices> _baseServiceMock;
        private Mock<IDownloadFileHandler> _downloadFileHandlerMock;
        private Mock<ICommand> _commandMock;
        private Mock<ICommandRequestContext> _contextMock;
        private Mock<HttpRequest> _httpRequestMock;
        private CreatePostCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _responseFactoryMock = new Mock<ICommandResponseFactory>();
            _baseServiceMock = new Mock<IBaseServices>();
            _downloadFileHandlerMock = new Mock<IDownloadFileHandler>();
            _commandMock = new Mock<ICommand>();
            _contextMock = new Mock<ICommandRequestContext>();
            _httpRequestMock = new Mock<HttpRequest>();

            _handler = new CreatePostCommandHandler(
                _responseFactoryMock.Object,
                _baseServiceMock.Object,
                _downloadFileHandlerMock.Object
            );
        }

        [Test]
        public void CommandTemplate_Returns_CREATEPOST()
        {
            Assert.That(_handler.CommandTemplate, Is.EqualTo(ALCommandTemplate.CREATEPOST));
        }

        [Test]
        public async Task HandleAsync_ReturnsBaseServiceResponse_OnSuccess()
        {
            // Arrange
            var docId = "123";
            var contentType = "application/pdf";
            var bodyStream = new MemoryStream();
            var sapComponents = new List<SapDocumentComponentModel>();
            var headers = new HeaderDictionary
            {
                { "charset", "UTF-8" },
                { "version", "1.0" },
                { "docprot", "prot" }
            };

            _commandMock.Setup(c => c.GetValue(ALParameter.VarDocId)).Returns(docId);
            _commandMock.Setup(c => c.GetValue(ALParameter.VarContRep)).Returns("ContRep");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarCompId)).Returns("CompId");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("PVersion");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarSecKey)).Returns("SecKey");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAccessMode)).Returns("AccessMode");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAuthId)).Returns("AuthId");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarExpiration)).Returns("Expiration");

            _httpRequestMock.Setup(r => r.ContentType).Returns(contentType);
            _httpRequestMock.Setup(r => r.Body).Returns(bodyStream);
            _httpRequestMock.Setup(r => r.ContentLength).Returns(100);
            _httpRequestMock.Setup(r => r.Headers).Returns(headers);

            _contextMock.Setup(c => c.GetRequest()).Returns(_httpRequestMock.Object);

            _downloadFileHandlerMock.Setup(h => h.HandleRequestAsync(contentType, bodyStream, docId))
                .ReturnsAsync(sapComponents);

            var expectedResponse = Mock.Of<ICommandResponse>();
            _baseServiceMock.Setup(s => s.CreateRecord(It.IsAny<CreateSapDocumentModel>(), true))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            // Assert
            Assert.That(expectedResponse, Is.EqualTo(result));
            _baseServiceMock.Verify(s => s.CreateRecord(It.IsAny<CreateSapDocumentModel>(), true), Times.Once);
        }

        [Test]
        public async Task HandleAsync_ReturnsErrorResponse_OnException()
        {
            // Arrange
            var exceptionMessage = "Test exception";
            _contextMock.Setup(c => c.GetRequest()).Throws(new Exception(exceptionMessage));

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError(It.Is<string>(msg => msg.Contains(exceptionMessage)), StatusCodes.Status500InternalServerError))
                .Returns(errorResponse);

            // Act
            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            // Assert
            Assert.That(errorResponse, Is.EqualTo(result));
            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(msg => msg.Contains(exceptionMessage)), StatusCodes.Status500InternalServerError), Times.Once);
        }
    }
}
