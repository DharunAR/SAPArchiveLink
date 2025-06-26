using Microsoft.AspNetCore.Http;
using Moq;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class InfoCommandHandlerTests
    {
        private Mock<IBaseServices> _baseServiceMock;
        private Mock<ICommandResponseFactory> _responseFactoryMock;
        private Mock<ICommand> _commandMock;
        private Mock<ICommandRequestContext> _contextMock;
        private InfoCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _baseServiceMock = new Mock<IBaseServices>();
            _responseFactoryMock = new Mock<ICommandResponseFactory>();
            _commandMock = new Mock<ICommand>();
            _contextMock = new Mock<ICommandRequestContext>();
            _handler = new InfoCommandHandler(_responseFactoryMock.Object, _baseServiceMock.Object);
        }

        [Test]
        public void CommandTemplate_ReturnsINFO()
        {
            Assert.AreEqual(ALCommandTemplate.INFO, _handler.CommandTemplate);
        }

        [Test]
        public async Task HandleAsync_ReturnsBaseServiceResult_OnSuccess()
        {
            _commandMock.Setup(c => c.GetValue(ALParameter.VarDocId)).Returns("doc1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarContRep)).Returns("rep1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarCompId)).Returns("comp1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("v1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarResultAs)).Returns("ascii");

            var expectedResponse = Mock.Of<ICommandResponse>();
            _baseServiceMock
                .Setup(s => s.GetDocumentInfo(It.IsAny<SapDocumentRequest>()))
                .ReturnsAsync(expectedResponse);

            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            Assert.AreEqual(expectedResponse, result);
            _baseServiceMock.Verify(s => s.GetDocumentInfo(It.Is<SapDocumentRequest>(r =>
                r.DocId == "doc1" &&
                r.ContRep == "rep1" &&
                r.CompId == "comp1" &&
                r.PVersion == "v1" &&
                r.ResultAs == "ascii"
            )), Times.Once);
        }

        [Test]
        public async Task HandleAsync_ReturnsErrorResponse_OnException()
        {
            var exception = new Exception("fail!");
            _baseServiceMock
                .Setup(s => s.GetDocumentInfo(It.IsAny<SapDocumentRequest>()))
                .ThrowsAsync(exception);

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock
                .Setup(f => f.CreateError(It.Is<string>(msg => msg.Contains("fail!")), StatusCodes.Status500InternalServerError))
                .Returns(errorResponse);
            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            Assert.That(result, Is.SameAs(errorResponse));
            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(msg => msg.Contains("fail!")), StatusCodes.Status500InternalServerError), Times.Once);
        }
    }
}
