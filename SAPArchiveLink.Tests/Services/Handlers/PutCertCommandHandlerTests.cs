using Moq;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class PutCertCommandHandlerTests
    {
        private Mock<ILogHelper<PutCertCommandHandler>> _loggerMock;
        private Mock<ICommandResponseFactory> _responseFactoryMock;
        private Mock<IBaseServices> _baseServicesMock;
        private Mock<ICommand> _commandMock;
        private Mock<ICommandRequestContext> _contextMock;
        private PutCertCommandHandler _handler;
        private Mock<ICommandResponse> _commandResponseMock;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogHelper<PutCertCommandHandler>>();
            _responseFactoryMock = new Mock<ICommandResponseFactory>();
            _baseServicesMock = new Mock<IBaseServices>();
            _commandMock = new Mock<ICommand>();
            _contextMock = new Mock<ICommandRequestContext>();
            _commandResponseMock = new Mock<ICommandResponse>();

            _handler = new PutCertCommandHandler(
                _loggerMock.Object,
                _responseFactoryMock.Object,
                _baseServicesMock.Object
            );
        }

        [Test]
        public async Task HandleAsync_ReturnsResponse_WhenPutCertSucceeds()
        {
            // Arrange
            _commandMock.Setup(c => c.GetValue(ALParameter.VarContRep)).Returns("contRep1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarAuthId)).Returns("authId1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPermissions)).Returns("perm1");
            _commandMock.Setup(c => c.GetValue(ALParameter.VarPVersion)).Returns("v1");
            var stream = new MemoryStream();
            _contextMock.Setup(c => c.GetInputStream()).Returns(stream);
            _baseServicesMock.Setup(b => b.PutCert(It.IsAny<PutCertificateModel>()))
                .ReturnsAsync(_commandResponseMock.Object);

            // Act
            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            // Assert
            Assert.That(result, Is.SameAs(_commandResponseMock.Object));
            _loggerMock.Verify(l => l.LogInformation(It.Is<string>(s => s.Contains("Start processing"))), Times.Once);
            _baseServicesMock.Verify(b => b.PutCert(It.Is<PutCertificateModel>(m =>
                m.ContRep == "contRep1" &&
                m.AuthId == "authId1" &&
                m.Permissions == "perm1" &&
                m.PVersion == "v1" &&
                m.Stream == stream
            )), Times.Once);
        }

        [Test]
        public async Task HandleAsync_ReturnsErrorResponse_WhenPutCertThrowsException()
        {
            // Arrange
            _commandMock.Setup(c => c.GetValue(It.IsAny<string>())).Returns(string.Empty);
            _contextMock.Setup(c => c.GetInputStream()).Returns(Stream.Null);
            var exception = new Exception("fail!");
            _baseServicesMock.Setup(b => b.PutCert(It.IsAny<PutCertificateModel>()))
                .ThrowsAsync(exception);
            var errorResponseMock = new Mock<ICommandResponse>();
            _responseFactoryMock.Setup(f => f.CreateError("fail!", It.IsAny<int>())).Returns(errorResponseMock.Object);

            // Act
            var result = await _handler.HandleAsync(_commandMock.Object, _contextMock.Object);

            // Assert
            Assert.That(result, Is.SameAs(errorResponseMock.Object));
            _responseFactoryMock.Verify(f => f.CreateError("fail!", It.IsAny<int>()), Times.Once);
        }
    }
}