using Microsoft.AspNetCore.Http;
using Moq;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class AppendCommandHandlerTests
    {
        private Mock<ICommandResponseFactory> _responseFactoryMock;
        private Mock<IBaseServices> _baseServiceMock;
        private Mock<ICommand> _commandMock;
        private Mock<ICommandRequestContext> _contextMock;
        private AppendCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _responseFactoryMock = new Mock<ICommandResponseFactory>();
            _baseServiceMock = new Mock<IBaseServices>();
            _commandMock = new Mock<ICommand>();
            _contextMock = new Mock<ICommandRequestContext>();
            _handler = new AppendCommandHandler(_responseFactoryMock.Object, _baseServiceMock.Object);
        }

        [Test]
        public async Task HandleAsync_ReturnsResponseFromBaseService_OnSuccess()
        {
            // Arrange
            var expectedResponse = Mock.Of<ICommandResponse>();
            var requestMock = new Mock<HttpRequest>();
            var stream = new MemoryStream();
            requestMock.Setup(r => r.Body).Returns(stream);
            _contextMock.Setup(c => c.GetRequest()).Returns(requestMock.Object);

            _commandMock.Setup(c => c.GetValue(ALParameter.VarDocId)).Returns("docId");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarContRep)).Returns("contRep");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarCompId)).Returns("compId");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("pVersion");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarSecKey)).Returns("secKey");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAccessMode)).Returns("accessMode");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAuthId)).Returns("authId");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarExpiration)).Returns("expiration");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarScanned)).Returns("scanned");

            _baseServiceMock
                .Setup(s => s.AppendDocument(It.IsAny<AppendSapDocCompModel>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            // Assert
            Assert.That(expectedResponse,Is.EqualTo(result));
            _baseServiceMock.Verify(s => s.AppendDocument(It.Is<AppendSapDocCompModel>(m =>
                m.DocId == "docId" &&
                m.ContRep == "contRep" &&
                m.CompId == "compId" &&
                m.PVersion == "pVersion" &&
                m.SecKey == "secKey" &&
                m.AccessMode == "accessMode" &&
                m.AuthId == "authId" &&
                m.Expiration == "expiration" &&
                m.StreamData == stream &&
                m.ScanPerformed == "scanned"
            )), Times.Once);
        }

        [Test]
        public async Task HandleAsync_ReturnsErrorResponse_OnException()
        {
            // Arrange
            var exception = new Exception("Test exception");
            _contextMock.Setup(c => c.GetRequest()).Throws(exception);
            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock
                .Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status500InternalServerError))
                .Returns(errorResponse);

            // Act
            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            // Assert
            Assert.That(errorResponse, Is.EqualTo(result));         
            _responseFactoryMock.Verify(f =>
                f.CreateError(It.Is<string>(msg => msg.Contains("Test exception")), StatusCodes.Status500InternalServerError),
                Times.Once);
        }
    }
}
