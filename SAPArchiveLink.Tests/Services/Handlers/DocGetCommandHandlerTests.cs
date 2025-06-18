using Microsoft.AspNetCore.Http;
using Moq;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class DocGetCommandHandlerTests
    {
        private Mock<ICommandResponseFactory> _responseFactoryMock;
        private Mock<IBaseServices> _baseServiceMock;
        private Mock<ICommand> _commandMock;
        private Mock<ICommandRequestContext> _contextMock;
        private DocGetCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _responseFactoryMock = new Mock<ICommandResponseFactory>();
            _baseServiceMock = new Mock<IBaseServices>();
            _commandMock = new Mock<ICommand>();
            _contextMock = new Mock<ICommandRequestContext>();
            _handler = new DocGetCommandHandler(_responseFactoryMock.Object, _baseServiceMock.Object);
        }

        [Test]
        public void CommandTemplate_Returns_DOCGET()
        {
            Assert.That(_handler.CommandTemplate, Is.EqualTo(ALCommandTemplate.DOCGET));
        }

        [Test]
        public async Task HandleAsync_MapsParametersAndDelegatesToBaseService()
        {
            var expectedResponse = Mock.Of<ICommandResponse>();
            _commandMock.Setup(c => c.GetValue(ALParameter.VarDocId)).Returns("doc1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarContRep)).Returns("rep1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarCompId)).Returns("comp1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("v1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarSecKey)).Returns("sec1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAccessMode)).Returns("read");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAuthId)).Returns("auth1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarExpiration)).Returns("2025-01-01");

            _baseServiceMock
                .Setup(s => s.DocGetSapComponents(It.IsAny<SapDocumentRequest>()))
                .ReturnsAsync(expectedResponse);
            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);
            Assert.That(result, Is.SameAs(expectedResponse));
            _baseServiceMock.Verify(s => s.DocGetSapComponents(It.Is<SapDocumentRequest>(r =>
                r.DocId == "doc1" &&
                r.ContRep == "rep1" &&
                r.CompId == "comp1" &&
                r.PVersion == "v1" &&
                r.SecKey == "sec1" &&
                r.AccessMode == "read" &&
                r.AuthId == "auth1" &&
                r.Expiration == "2025-01-01"
            )), Times.Once);
        }

        [Test]
        public async Task HandleAsync_ReturnsErrorResponse_OnException()
        {
            var exception = new Exception("fail!");
            _baseServiceMock
                .Setup(s => s.DocGetSapComponents(It.IsAny<SapDocumentRequest>()))
                .ThrowsAsync(exception);

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock
                .Setup(f => f.CreateError(It.Is<string>(msg => msg.Contains("fail!")), StatusCodes.Status500InternalServerError))
                .Returns(errorResponse);
            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            Assert.That(result, Is.SameAs(errorResponse));
            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(msg => msg.Contains("fail!")), StatusCodes.Status500InternalServerError), Times.Once);
        }

        [Test]
        public async Task HandleAsync_MapsNullParameters()
        {
            var expectedResponse = Mock.Of<ICommandResponse>();
            _commandMock.Setup(c => c.GetValue(It.IsAny<string>())).Returns((string)null);

            _baseServiceMock
                .Setup(s => s.DocGetSapComponents(It.IsAny<SapDocumentRequest>()))
                .ReturnsAsync(expectedResponse);
            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            Assert.That(result, Is.SameAs(expectedResponse));
            _baseServiceMock.Verify(s => s.DocGetSapComponents(It.Is<SapDocumentRequest>(r =>
                r.DocId == null &&
                r.ContRep == null &&
                r.CompId == null &&
                r.PVersion == null &&
                r.SecKey == null &&
                r.AccessMode == null &&
                r.AuthId == null &&
                r.Expiration == null
            )), Times.Once);
        }
    }
}