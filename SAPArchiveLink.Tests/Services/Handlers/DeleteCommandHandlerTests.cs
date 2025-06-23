using Microsoft.AspNetCore.Http;
using Moq;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class DeleteCommandHandlerTests
    {
        private Mock<ICommandResponseFactory> _responseFactoryMock;
        private Mock<IBaseServices> _baseServicesMock;
        private Mock<ICommand> _commandMock;
        private Mock<ICommandRequestContext> _contextMock;
        private DeleteCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _responseFactoryMock = new Mock<ICommandResponseFactory>();
            _baseServicesMock = new Mock<IBaseServices>();
            _commandMock = new Mock<ICommand>();
            _contextMock = new Mock<ICommandRequestContext>();

            _handler = new DeleteCommandHandler(_responseFactoryMock.Object, _baseServicesMock.Object);
        }

        [Test]
        public void CommandTemplate_Returns_DELETE()
        {
            Assert.That(_handler.CommandTemplate, Is.EqualTo(ALCommandTemplate.DELETE));
        }

        [Test]
        public async Task HandleAsync_ReturnsServiceResult()
        {
            var expectedResponse = Mock.Of<ICommandResponse>();
            _baseServicesMock.Setup(s => s.DeleteSapDocument(It.IsAny<SapDocumentRequest>()))
                .ReturnsAsync(expectedResponse);

            _commandMock.Setup(c => c.GetValue(ALParameter.VarDocId)).Returns("doc1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarContRep)).Returns("rep1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarCompId)).Returns("comp1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("v1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarSecKey)).Returns("sec");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAccessMode)).Returns("crud");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAuthId)).Returns("auth");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarExpiration)).Returns("exp");

            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            Assert.That(result, Is.EqualTo(expectedResponse));
            _baseServicesMock.Verify(s => s.DeleteSapDocument(It.Is<SapDocumentRequest>(r =>
                r.DocId == "doc1" &&
                r.ContRep == "rep1" &&
                r.CompId == "comp1" &&
                r.PVersion == "v1" &&
                r.SecKey == "sec" &&
                r.AccessMode == "crud" &&
                r.AuthId == "auth" &&
                r.Expiration == "exp"
            )), Times.Once);
        }

        [Test]
        public async Task HandleAsync_Returns500Error_WhenServiceThrows()
        {
            var errorResponse = Mock.Of<ICommandResponse>();
            _baseServicesMock.Setup(s => s.DeleteSapDocument(It.IsAny<SapDocumentRequest>()))
                .ThrowsAsync(new Exception("unexpected error"));
            _responseFactoryMock.Setup(f => f.CreateError("unexpected error", StatusCodes.Status500InternalServerError))
                .Returns(errorResponse);

            _commandMock.Setup(c => c.GetValue(It.IsAny<string>())).Returns((string)null);

            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            Assert.That(result, Is.EqualTo(errorResponse));
            _responseFactoryMock.Verify(f => f.CreateError("unexpected error", StatusCodes.Status500InternalServerError), Times.Once);
        }
    }
}