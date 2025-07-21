using Microsoft.AspNetCore.Http;
using Moq;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class ServerInfoCommandHandlerTests
    {
        private Mock<ICommandResponseFactory> _responseFactoryMock;
        private Mock<IBaseServices> _baseServicesMock;
        private Mock<ICommand> _commandMock;
        private Mock<ICommandRequestContext> _contextMock;
        private ServerInfoCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _responseFactoryMock = new Mock<ICommandResponseFactory>();
            _baseServicesMock = new Mock<IBaseServices>();
            _commandMock = new Mock<ICommand>();
            _contextMock = new Mock<ICommandRequestContext>();

            _handler = new ServerInfoCommandHandler(_responseFactoryMock.Object, _baseServicesMock.Object);
        }

        [Test]
        public void CommandTemplate_Returns_SERVERINFO()
        {
            Assert.That(_handler.CommandTemplate, Is.EqualTo(ALCommandTemplate.SERVERINFO));
        }


        [Test]
        public async Task HandleAsync_ReturnsServerInfoResponse_WhenSuccessful()
        {
            var expectedResponse = Mock.Of<ICommandResponse>();
            _commandMock.Setup(c => c.GetValue(ALParameter.VarContRep)).Returns("contRep");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("pVersion");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarResultAs)).Returns("resultAs");

            _baseServicesMock
                .Setup(s => s.GetServerInfo("contRep", "pVersion", "resultAs"))
                .ReturnsAsync(expectedResponse);

            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            Assert.That(result, Is.EqualTo(expectedResponse));
        }

        [Test]
        public async Task HandleAsync_ReturnsErrorResponse_WhenExceptionThrown()
        {
            var exceptionMessage = "Something went wrong";
            _commandMock.Setup(c => c.GetValue(It.IsAny<string>())).Throws(new Exception(exceptionMessage));

            var errorResponse = Mock.Of<ICommandResponse>();
            _responseFactoryMock
                .Setup(f => f.CreateError(It.Is<string>(msg => msg.Contains(exceptionMessage)), StatusCodes.Status500InternalServerError))
                .Returns(errorResponse);

            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            Assert.That(result, Is.EqualTo(errorResponse));
        }

    }
}
