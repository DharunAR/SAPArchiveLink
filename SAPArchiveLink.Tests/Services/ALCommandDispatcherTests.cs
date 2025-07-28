using Microsoft.AspNetCore.Http;
using Moq;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class ALCommandDispatcherTests
    {
        private Mock<ICommandHandlerRegistry> _mockRegistry;
        private Mock<ICommandResponseFactory> _mockResponseFactory;
        private Mock<IDownloadFileHandler> _mockFileHandler;
        private Mock<IDatabaseConnection> _dbConnectionMock;
        private Mock<ITrimRepository> _trimRepositoryMock;
        private Mock<IArchiveCertificate> _archiveCertificateMock;
        private ContentServerRequestAuthenticator _authenticator;
        private ALCommandDispatcher _dispatcher;

        [SetUp]
        public void Setup()
        {
            _mockRegistry = new Mock<ICommandHandlerRegistry>();
            _mockResponseFactory = new Mock<ICommandResponseFactory>();
            _mockFileHandler = new Mock<IDownloadFileHandler>();
            _dbConnectionMock = new Mock<IDatabaseConnection>();
            _trimRepositoryMock = new Mock<ITrimRepository>();
            _archiveCertificateMock = new Mock<IArchiveCertificate>();

            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(_trimRepositoryMock.Object);
            _trimRepositoryMock.Setup(r => r.IsProductFeatureActivated()).Returns(true);
            _archiveCertificateMock = new Mock<IArchiveCertificate>();

            var verifier = new Mock<IVerifier>();
            var logger = new Mock<ILogHelper<ContentServerRequestAuthenticator>>();

            _authenticator = new ContentServerRequestAuthenticator(verifier.Object, logger.Object, _mockResponseFactory.Object);

            _dispatcher = new ALCommandDispatcher(
                _mockRegistry.Object,
                _mockResponseFactory.Object,
                _mockFileHandler.Object,
                _dbConnectionMock.Object, new Mock<ILogHelper<ALCommandDispatcher>>().Object
            );
        }

        [TestCase("get&compId=1&contRep=CM&pVersion=0045", ALCommandTemplate.GET, "GET")]
        [TestCase("create&compId=2&contRep=CM&pVersion=0045", ALCommandTemplate.CREATEPUT, "PUT")]
        [TestCase("docget&compId=3&contRep=CM&pVersion=0045", ALCommandTemplate.DOCGET, "GET")]
        public async Task RunRequest_ValidCommand_DispatchesToHandler(string url, ALCommandTemplate template, string method)
        {
            var mockHandler = new Mock<ICommandHandler>();
            var mockResponse = new Mock<ICommandResponse>();

            mockHandler.Setup(h => h.CommandTemplate).Returns(template);
            mockHandler.Setup(h => h.HandleAsync(It.IsAny<ICommand>(), It.IsAny<ICommandRequestContext>()))
                       .ReturnsAsync(mockResponse.Object);

            _mockRegistry.Setup(r => r.GetHandler(template)).Returns(mockHandler.Object);
            _trimRepositoryMock.Setup(r => r.GetArchiveCertificate(It.IsAny<string>())).Returns(_archiveCertificateMock.Object);
            _archiveCertificateMock.Setup(c => c.IsEnabled()).Returns(true);

            var result = await _dispatcher.RunRequest(CreateCommandRequest(url, method), _authenticator);

            mockHandler.Verify(h => h.HandleAsync(It.IsAny<ICommand>(), It.IsAny<ICommandRequestContext>()), Times.Once);
            Assert.That(result, Is.TypeOf<ArchiveLinkResult>());
        }

        [Test]
        public async Task RunRequest_UnsupportedCommand_ReturnsErrorResponse()
        {
            var errorResponse = new Mock<ICommandResponse>();
            errorResponse.Setup(r => r.StatusCode).Returns(400);

            _mockResponseFactory.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status400BadRequest))
                                .Returns(errorResponse.Object);

            var result = await _dispatcher.RunRequest(CreateCommandRequest("update&compId=1", "PUT"), _authenticator);

            Assert.That(result, Is.TypeOf<ArchiveLinkResult>());
            _mockResponseFactory.Verify(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status400BadRequest), Times.Once);
        }

        [Test]
        public async Task RunRequest_InvalidCommand_ReturnsBadRequest()
        {
            var errorResponse = new Mock<ICommandResponse>();
            _mockResponseFactory.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status400BadRequest))
                                .Returns(errorResponse.Object);

            var result = await _dispatcher.RunRequest(CreateCommandRequest("invalid", "GET"), _authenticator);

            Assert.That(result, Is.TypeOf<ArchiveLinkResult>());
        }

        [TestCase("get&compId=1&contRep=CM", ALCommandTemplate.GET, "GET")]
        public async Task RunRequest_CertificateIsNotEnabled_Returns403(string url, ALCommandTemplate template, string method)
        {
            _trimRepositoryMock.Setup(r => r.GetArchiveCertificate(It.IsAny<string>())).Returns(_archiveCertificateMock.Object);
            _archiveCertificateMock.Setup(c => c.IsEnabled()).Returns(false);

            var errorResponse = new Mock<ICommandResponse>();
            _mockResponseFactory.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status403Forbidden))
                                .Returns(errorResponse.Object);

            var result = await _dispatcher.RunRequest(CreateCommandRequest(url, method), _authenticator);

            Assert.That(result, Is.TypeOf<ArchiveLinkResult>());
        }

        [TestCase("serverinfo&compId=1&contRep=CM", ALCommandTemplate.SERVERINFO, "GET")]
        public async Task RunRequest_ServerInfo_PversionIsNull_Returns400(string url, ALCommandTemplate template, string method)
        {
            var errorResponse = new Mock<ICommandResponse>();
            _mockResponseFactory.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status400BadRequest))
                                .Returns(errorResponse.Object);

            var result = await _dispatcher.RunRequest(CreateCommandRequest(url, method), _authenticator);

            Assert.That(result, Is.TypeOf<ArchiveLinkResult>());
        }

        [TestCase("get&compId=1&contRep=CM", ALCommandTemplate.GET, "GET")]
        public async Task RunRequest_CertificateIsNull_Returns404(string url, ALCommandTemplate template, string method)
        {
            _trimRepositoryMock.Setup(r => r.GetArchiveCertificate(It.IsAny<string>())).Returns((IArchiveCertificate)null);

            var errorResponse = new Mock<ICommandResponse>();
            _mockResponseFactory.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status404NotFound))
                                .Returns(errorResponse.Object);

            var result = await _dispatcher.RunRequest(CreateCommandRequest(url, method), _authenticator);

            Assert.That(result, Is.TypeOf<ArchiveLinkResult>());
        }

        [TestCase("serverInfo&pVersion=0047", ALCommandTemplate.GET, "GET")]
        public async Task RunRequest_SAPLicenseDisabled_Returns403(string url, ALCommandTemplate template, string method)
        {
            _trimRepositoryMock.Setup(r => r.IsProductFeatureActivated()).Returns(false);

            var errorResponse = new Mock<ICommandResponse>();
            _mockResponseFactory.Setup(f => f.CreateError("SAP integration license/feature is not enabled.", StatusCodes.Status403Forbidden))
                                .Returns(errorResponse.Object);

            var result = await _dispatcher.RunRequest(CreateCommandRequest(url, method), _authenticator);

            Assert.That(result, Is.TypeOf<ArchiveLinkResult>());
            _mockResponseFactory.Verify(f => f.CreateError("SAP integration license/feature is not enabled.", StatusCodes.Status403Forbidden), Times.Once);
        }

        [TestCase("get&compId=1&contRep=CM", ALCommandTemplate.GET, "GET")]
        public async Task RunRequest_AuthenticationFails_ReturnsErrorResponse(string url, ALCommandTemplate template, string method)
        {
            var mockHandler = new Mock<ICommandHandler>();
            var errorResponse = new Mock<ICommandResponse>();

            _mockRegistry.Setup(r => r.GetHandler(template)).Returns(mockHandler.Object);
            _trimRepositoryMock.Setup(r => r.GetArchiveCertificate(It.IsAny<string>())).Returns(_archiveCertificateMock.Object);
            _archiveCertificateMock.Setup(c => c.IsEnabled()).Returns(true);

            _mockResponseFactory.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                                .Returns(errorResponse.Object);

            var verifier = new Mock<IVerifier>();
            var logger = new Mock<ILogHelper<ContentServerRequestAuthenticator>>();
            var authenticator = new ContentServerRequestAuthenticator(verifier.Object, logger.Object, _mockResponseFactory.Object);

            var request = CreateCommandRequest(url, method);

            var result = await _dispatcher.RunRequest(request, authenticator);

            Assert.That(result, Is.TypeOf<ArchiveLinkResult>());
            _mockResponseFactory.Verify(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status400BadRequest), Times.Once);
        }

        [TestCase("get&compId=1&contRep=CM&pVersion=0047", ALCommandTemplate.GET, "GET")]
        public async Task RunRequest_ExecuteAsync_ReturnsErrorResponse(string url, ALCommandTemplate template, string method)
        {
            var mockHandler = new Mock<ICommandHandler>();
            var mockResponse = new Mock<ICommandResponse>();

            mockHandler.Setup(h => h.CommandTemplate).Returns(template);
            mockHandler.Setup(h => h.HandleAsync(It.IsAny<ICommand>(), It.IsAny<ICommandRequestContext>()))
                       .ReturnsAsync(mockResponse.Object);
            _trimRepositoryMock.Setup(r => r.GetArchiveCertificate(It.IsAny<string>())).Returns(_archiveCertificateMock.Object);
            _archiveCertificateMock.Setup(c => c.IsEnabled()).Returns(true);
            var errorResponse = new Mock<ICommandResponse>();
            _mockRegistry.Setup(f => f.GetHandler(template)).Throws(new Exception("Handler throws exception"));

            _mockResponseFactory.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                                .Returns(errorResponse.Object);

            var verifier = new Mock<IVerifier>();
            var logger = new Mock<ILogHelper<ContentServerRequestAuthenticator>>();
            var authenticator = new ContentServerRequestAuthenticator(verifier.Object, logger.Object, _mockResponseFactory.Object);

            var request = CreateCommandRequest(url, method);

            var result = await _dispatcher.RunRequest(request, authenticator);

            Assert.That(result, Is.TypeOf<ArchiveLinkResult>());
            _mockResponseFactory.Verify(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status500InternalServerError), Times.Once);
        }

        private CommandRequest CreateCommandRequest(string url, string method)
        {
            var httpContext = new DefaultHttpContext();
            var httpRequest = httpContext.Request;
            httpRequest.Method = method;
            httpRequest.QueryString = new QueryString("?" + url);

            if (httpRequest.Method == "PUT" || httpRequest.Method == "POST")
            {
                httpRequest.ContentType = "application/x-www-form-urlencoded";
                httpRequest.ContentLength = 100;
            }

            return new CommandRequest
            {
                Url = url,
                HttpMethod = method,
                Charset = "UTF-8",
                HttpRequest = httpRequest
            };
        }
    }

}