using Moq;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class GetCommandHandlerTests
    {
        private Mock<IBaseServices> _baseServiceMock;
        private Mock<ICommandResponseFactory> _responseFactoryMock;
        private Mock<ICommand> _commandMock;
        private Mock<ICommandRequestContext> _contextMock;
        private GetCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _baseServiceMock = new Mock<IBaseServices>();
            _responseFactoryMock = new Mock<ICommandResponseFactory>();
            _commandMock = new Mock<ICommand>();
            _contextMock = new Mock<ICommandRequestContext>();
            _handler = new GetCommandHandler(_baseServiceMock.Object, _responseFactoryMock.Object);
        }

        [Test]
        public void CommandTemplate_ReturnsGET()
        {
            Assert.AreEqual(ALCommandTemplate.GET, _handler.CommandTemplate);
        }

        [Test]
        public async Task HandleAsync_ReturnsBaseServiceResult_OnSuccess()
        {
            _commandMock.Setup(c => c.GetValue(ALParameter.VarFromOffset)).Returns("10");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarToOffset)).Returns("20");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarDocId)).Returns("doc1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarContRep)).Returns("rep1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarCompId)).Returns("comp1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("v1");

            var expectedResponse = Mock.Of<ICommandResponse>();
            _baseServiceMock
                .Setup(s => s.GetSapDocument(It.IsAny<SapDocumentRequest>()))
                .ReturnsAsync(expectedResponse);

            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            Assert.AreEqual(expectedResponse, result);
            _baseServiceMock.Verify(s => s.GetSapDocument(It.Is<SapDocumentRequest>(r =>
                r.DocId == "doc1" &&
                r.ContRep == "rep1" &&
                r.CompId == "comp1" &&
                r.PVersion == "v1" &&
                r.FromOffset == 10 &&
                r.ToOffset == 20
            )), Times.Once);
        }

        [Test]
        public async Task HandleAsync_ParsesOffsetsAsZero_WhenInvalid()
        {
            _commandMock.Setup(c => c.GetValue(ALParameter.VarFromOffset)).Returns("notanumber");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarToOffset)).Returns((string)null);
            _commandMock.Setup(c => c.GetValue(ALParameter.VarDocId)).Returns("doc1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarContRep)).Returns("rep1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarCompId)).Returns("comp1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("v1");

            var expectedResponse = Mock.Of<ICommandResponse>();
            _baseServiceMock
                .Setup(s => s.GetSapDocument(It.IsAny<SapDocumentRequest>()))
                .ReturnsAsync(expectedResponse);

            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            Assert.AreEqual(expectedResponse, result);
            _baseServiceMock.Verify(s => s.GetSapDocument(It.Is<SapDocumentRequest>(r =>
                r.FromOffset == 0 && r.ToOffset == 0
            )), Times.Once);
        }

        [Test]
        public async Task HandleAsync_ReturnsErrorResponse_OnException()
        {
            _commandMock.Setup(c => c.GetValue(It.IsAny<string>())).Throws(new Exception("fail"));
            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError(It.Is<string>(msg => msg == "fail"), It.Is<int>(code => code == 400)))
                                .Returns(errorResponse);

            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            Assert.AreEqual(errorResponse, result);
            _responseFactoryMock.Verify(f => f.CreateError(It.Is<string>(msg => msg == "fail"), It.Is<int>(code => code == 400)), Times.Once);
        }
    }
}