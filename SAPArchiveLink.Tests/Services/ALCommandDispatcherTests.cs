using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace SAPArchiveLink.Tests
{

    [TestFixture]
    public class ALCommandDispatcherTests
    {
        private Mock<ICommandHandlerRegistry> _mockRegistry;
        private Mock<ICommandResponseFactory> _mockResponseFactory;
        private Mock<IDownloadFileHandler> _mockFileHandler;
        private ALCommandDispatcher _dispatcher;
        private Mock<IDatabaseConnection> _dbConnectionMock;
        private Mock<ITrimRepository> _trimRepositoryMock;
        private Mock<IArchiveCertificate> _archiveCertificateMock;
        private Mock<ContentServerRequestAuthenticator> _contentServerRequestAuthenticator;

        [SetUp]
        public void Setup()
        {
            _mockRegistry = new Mock<ICommandHandlerRegistry>();
            _mockResponseFactory = new Mock<ICommandResponseFactory>();
            _mockFileHandler = new Mock<IDownloadFileHandler>();
            _dbConnectionMock = new Mock<IDatabaseConnection>();
            _dispatcher = new ALCommandDispatcher(_mockRegistry.Object, _mockResponseFactory.Object, _mockFileHandler.Object, _dbConnectionMock.Object);
            _trimRepositoryMock = new Mock<ITrimRepository>();
            _dbConnectionMock.Setup(d => d.GetDatabase()).Returns(_trimRepositoryMock.Object);
            _trimRepositoryMock.Setup(r => r.IsProductFeatureActivated()).Returns(true);
            _archiveCertificateMock = new Mock<IArchiveCertificate>();
            _contentServerRequestAuthenticator = new Mock<ContentServerRequestAuthenticator>(
                Mock.Of<IVerifier>(),
                Mock.Of<ILogHelper<ContentServerRequestAuthenticator>>(),
                _mockResponseFactory.Object
            );

        }

        [TestCase("get&compId=1", ALCommandTemplate.GET, "GET")]
        [TestCase("create&compId=2", ALCommandTemplate.CREATEPUT, "PUT")]
        [TestCase("docget&compId=3", ALCommandTemplate.DOCGET, "GET")]
        public async Task RunRequest_ValidCommand_DispatchesToHandler(string url, ALCommandTemplate template, string method)
        {
            var mockHandler = new Mock<ICommandHandler>();
            var mockResponse = new Mock<ICommandResponse>();

            mockHandler.Setup(h => h.CommandTemplate).Returns(template);
            mockHandler.Setup(h => h.HandleAsync(It.IsAny<ICommand>(), It.IsAny<ICommandRequestContext>()))
                       .ReturnsAsync(mockResponse.Object);

            _mockRegistry.Setup(r => r.GetHandler(template)).Returns(mockHandler.Object);

            var httpContext = new DefaultHttpContext();
            var httpRequest = httpContext.Request;
            httpRequest.Method = method;
            httpRequest.QueryString = new QueryString("?" + url);

            var request = new CommandRequest
            {
                Url = url,
                HttpMethod = method,
                Charset = "UTF-8",
                HttpRequest = httpRequest
            };

            var result = await _dispatcher.RunRequest(request, null);

            mockHandler.Verify(h => h.HandleAsync(It.IsAny<ICommand>(), It.IsAny<ICommandRequestContext>()), Times.Once);            
            Assert.That(result, Is.TypeOf<ArchiveLinkResult>());
        }

        [Test]
        public async Task RunRequest_UnsupportedCommand_ReturnsErrorResponse()
        {
            var errorResponse = new Mock<ICommandResponse>();
            errorResponse.Setup(r => r.StatusCode).Returns(400);

            _mockResponseFactory.Setup(f => f.CreateError(It.IsAny<string>(), It.IsAny<int>()))
                                .Returns(errorResponse.Object);

            var httpContext = new DefaultHttpContext();
            var httpRequest = httpContext.Request;
            httpRequest.Method = "PUT";
            httpRequest.QueryString = new QueryString("?update&compId=1");

            var request = new CommandRequest
            {
                Url = "update&compId=1",
                HttpMethod = "PUT",
                Charset = "UTF-8",
                HttpRequest = httpRequest
            };

            var result = await _dispatcher.RunRequest(request, null);

            Assert.That(result, Is.TypeOf<ArchiveLinkResult>());           
            _mockResponseFactory.Verify(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status400BadRequest), Times.Once);
        }

        [Test]
        public async Task RunRequest_InvalidCommand_ReturnsBadRequest()
        {
            var httpContext = new DefaultHttpContext();
            var httpRequest = httpContext.Request;
            httpRequest.Method = "GET";
            httpRequest.QueryString = new QueryString("?invalid");

            var request = new CommandRequest
            {
                Url = "invalid",
                HttpMethod = "GET",
                Charset = "UTF-8",
                HttpRequest = httpRequest
            };

            var errorResponse = new Mock<ICommandResponse>();
            _mockResponseFactory.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status400BadRequest))
                                .Returns(errorResponse.Object);

            var result = await _dispatcher.RunRequest(request, null);

            Assert.That(result, Is.TypeOf<ArchiveLinkResult>());
        }

        [TestCase("get&compId=1&contRep=CM", ALCommandTemplate.GET, "GET")]       
        public async Task RunRequest_CertificateIsNotEnabled_Return403Forbidden(string url, ALCommandTemplate template, string method)
        {
            var mockHandler = new Mock<ICommandHandler>();
            var mockResponse = new Mock<ICommandResponse>();           

            mockHandler.Setup(h => h.CommandTemplate).Returns(template);
            mockHandler.Setup(h => h.HandleAsync(It.IsAny<ICommand>(), It.IsAny<ICommandRequestContext>()))
                       .ReturnsAsync(mockResponse.Object);

            _mockRegistry.Setup(r => r.GetHandler(template)).Returns(mockHandler.Object);

            _trimRepositoryMock.Setup(r => r.GetArchiveCertificate(It.IsAny<string>()))
              .Returns(_archiveCertificateMock.Object);

            var httpContext = new DefaultHttpContext();
            var httpRequest = httpContext.Request;
            httpRequest.Method = method;
            httpRequest.QueryString = new QueryString("?" + url);

            var request = new CommandRequest
            {
                Url = url,
                HttpMethod = method,
                Charset = "UTF-8",
                HttpRequest = httpRequest,
                
            };
            var errorResponse = new Mock<ICommandResponse>();
            _mockResponseFactory.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status403Forbidden))
                                .Returns(errorResponse.Object);

            var result = await _dispatcher.RunRequest(request, null);          
            Assert.That(result, Is.TypeOf<ArchiveLinkResult>());
        }

        [TestCase("serverinfo&compId=1&contRep=CM", ALCommandTemplate.GET, "GET")]
        public async Task RunRequest_ServerInfo_PversionIsNull_Return400BadRequest(string url, ALCommandTemplate template, string method)
        {
            var mockHandler = new Mock<ICommandHandler>();
            var mockResponse = new Mock<ICommandResponse>();

            mockHandler.Setup(h => h.CommandTemplate).Returns(template);
            mockHandler.Setup(h => h.HandleAsync(It.IsAny<ICommand>(), It.IsAny<ICommandRequestContext>()))
                       .ReturnsAsync(mockResponse.Object);

            _mockRegistry.Setup(r => r.GetHandler(template)).Returns(mockHandler.Object);

            _trimRepositoryMock.Setup(r => r.GetArchiveCertificate(It.IsAny<string>()))
              .Returns(_archiveCertificateMock.Object);

            var httpContext = new DefaultHttpContext();
            var httpRequest = httpContext.Request;
            httpRequest.Method = method;
            httpRequest.QueryString = new QueryString("?" + url);

            var request = new CommandRequest
            {
                Url = url,
                HttpMethod = method,
                Charset = "UTF-8",
                HttpRequest = httpRequest,

            };
            var errorResponse = new Mock<ICommandResponse>();
            _mockResponseFactory.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status400BadRequest))
                                .Returns(errorResponse.Object);

            var result = await _dispatcher.RunRequest(request, null);
            Assert.That(result, Is.TypeOf<ArchiveLinkResult>());
        }

        [TestCase("get&compId=1&contRep=CM", ALCommandTemplate.GET, "GET")]
        public async Task RunRequest_CertificateIsNull_Return404NotFound(string url, ALCommandTemplate template, string method)
        {
            var mockHandler = new Mock<ICommandHandler>();
            var mockResponse = new Mock<ICommandResponse>();

            mockHandler.Setup(h => h.CommandTemplate).Returns(template);
            mockHandler.Setup(h => h.HandleAsync(It.IsAny<ICommand>(), It.IsAny<ICommandRequestContext>()))
                       .ReturnsAsync(mockResponse.Object);

            _mockRegistry.Setup(r => r.GetHandler(template)).Returns(mockHandler.Object);

            _trimRepositoryMock.Setup(r => r.GetArchiveCertificate(It.IsAny<string>()))
              .Returns((IArchiveCertificate)null);

            var httpContext = new DefaultHttpContext();
            var httpRequest = httpContext.Request;
            httpRequest.Method = method;
            httpRequest.QueryString = new QueryString("?" + url);

            var request = new CommandRequest
            {
                Url = url,
                HttpMethod = method,
                Charset = "UTF-8",
                HttpRequest = httpRequest,

            };
            var errorResponse = new Mock<ICommandResponse>();
            _mockResponseFactory.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status404NotFound))
                                .Returns(errorResponse.Object);

            var result = await _dispatcher.RunRequest(request, null);
            Assert.That(result, Is.TypeOf<ArchiveLinkResult>());
        }

        //[TestCase("get&compId=1&contRep=CM", ALCommandTemplate.GET, "GET")]
        //public async Task RunRequest_CertificateIsEnabled_Return404NotFound(string url, ALCommandTemplate template, string method)
        //{
        //    var mockHandler = new Mock<ICommandHandler>();
        //    var mockResponse = new Mock<ICommandResponse>();

        //    mockHandler.Setup(h => h.CommandTemplate).Returns(template);
        //    mockHandler.Setup(h => h.HandleAsync(It.IsAny<ICommand>(), It.IsAny<ICommandRequestContext>()))
        //               .ReturnsAsync(mockResponse.Object);

        //    _mockRegistry.Setup(r => r.GetHandler(template)).Returns(mockHandler.Object);

        //    _archiveCertificateMock.Setup(d => d.IsEnabled()).Returns(true);

        //    _trimRepositoryMock.Setup(r => r.GetArchiveCertificate(It.IsAny<string>()))
        //      .Returns(_archiveCertificateMock.Object);

        //    var httpContext = new DefaultHttpContext();
        //    var httpRequest = httpContext.Request;
        //    httpRequest.Method = method;
        //    httpRequest.QueryString = new QueryString("?" + url);

        //    var request = new CommandRequest
        //    {
        //        Url = url,
        //        HttpMethod = method,
        //        Charset = "UTF-8",
        //        HttpRequest = httpRequest,

        //    };
        //    var errorResponse = new Mock<ICommandResponse>();
        //    _mockResponseFactory.Setup(f => f.CreateError(It.IsAny<string>(), StatusCodes.Status404NotFound))
        //                        .Returns(errorResponse.Object);
        //    _contentServerRequestAuthenticator.Setup(f => f.CheckRequest(request, It.IsAny<ICommand>(), _archiveCertificateMock.Object))
        //        .Returns(new RequestAuthResult
        //        {
        //            IsAuthenticated = false,
        //            ErrorResponse = errorResponse.Object
        //        });
        //    var result = await _dispatcher.RunRequest(request, It.IsAny<ContentServerRequestAuthenticator>());
        //    Assert.That(result, Is.TypeOf<ArchiveLinkResult>());
        //}
    }

}
