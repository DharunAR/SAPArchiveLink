using Moq;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class PutCertCommandHandlerTests
    {
        private Mock<ILogHelper<PutCertCommandHandler>> _mockLogger;
        private Mock<ICommandResponseFactory> _mockResponseFactory;
        private Mock<IBaseServices> _mockBaseServices;
        private Mock<ICommand> _mockCommand;
        private Mock<ICommandRequestContext> _mockContext;
        private PutCertCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogHelper<PutCertCommandHandler>>();
            _mockResponseFactory = new Mock<ICommandResponseFactory>();
            _mockBaseServices = new Mock<IBaseServices>();
            _mockCommand = new Mock<ICommand>();
            _mockContext = new Mock<ICommandRequestContext>();

            _handler = new PutCertCommandHandler(
                _mockLogger.Object,
                _mockResponseFactory.Object,
                _mockBaseServices.Object
            );
        }

        [Test]
        public async Task HandleAsync_Success_ReturnsCommandResponse()
        {
            // Arrange
            var contRep = "CONTREP";
            var authId = "AUTHID";
            var permissions = "PERM";
            var fakeStream = new MemoryStream();
            var expectedResponse = Mock.Of<ICommandResponse>();

            var putCertificateModel = new PutCertificateModel
            {
                AuthId = authId,
                ContRep = contRep,
                Permissions = permissions,
                PVersion = "1",
                Stream = fakeStream
            };

            _mockCommand.Setup(c => c.GetValue(ALParameter.VarContRep)).Returns(contRep);
            _mockCommand.Setup(c => c.GetValue(ALParameter.VarAuthId)).Returns(authId);
            _mockCommand.Setup(c => c.GetValue(ALParameter.VarPermissions)).Returns(permissions);
            _mockContext.Setup(c => c.GetInputStream()).Returns(fakeStream);
            _mockBaseServices.Setup(s => s.PutCert(putCertificateModel))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _handler.HandleAsync(_mockCommand.Object, _mockContext.Object);

            // Assert
            Assert.That(result, Is.SameAs(expectedResponse));
            _mockLogger.Verify(l => l.LogInformation(It.Is<string>(s => s.Contains("Start processing"))), Times.Once);
        }

        [Test]
        public async Task HandleAsync_WhenBaseServicesThrows_ReturnsErrorResponse()
        {
            var contRep = "CONTREP";
            var authId = "AUTHID";
            var permissions = "PERM";
            var fakeStream = new MemoryStream();
            var errorMessage = "fail";
            var errorResponse = Mock.Of<ICommandResponse>();

            var putCertificateModel = new PutCertificateModel
            {
                AuthId = authId,
                ContRep = contRep,
                Permissions = permissions,
                PVersion = "1",
                Stream = null
            };

            _mockCommand.Setup(c => c.GetValue(ALParameter.VarContRep)).Returns(contRep);
            _mockCommand.Setup(c => c.GetValue(ALParameter.VarAuthId)).Returns(authId);
            _mockCommand.Setup(c => c.GetValue(ALParameter.VarPermissions)).Returns(permissions);
            _mockContext.Setup(c => c.GetInputStream()).Returns(fakeStream);
            _mockBaseServices.Setup(s => s.PutCert(putCertificateModel))
                .ThrowsAsync(new InvalidOperationException(errorMessage));
            _mockResponseFactory.Setup(f => f.CreateError(errorMessage, It.IsAny<int>())).Returns(errorResponse);

            var result = await _handler.HandleAsync(_mockCommand.Object, _mockContext.Object);

            Assert.That(result, Is.SameAs(errorResponse));
            _mockResponseFactory.Verify(f => f.CreateError(errorMessage, It.IsAny<int>()), Times.Once);
        }
    }
}