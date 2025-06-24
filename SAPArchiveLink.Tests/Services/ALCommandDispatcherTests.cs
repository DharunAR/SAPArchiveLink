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
        private ALCommandDispatcher _dispatcher;

        [SetUp]
        public void Setup()
        {
            _mockRegistry = new Mock<ICommandHandlerRegistry>();
            _mockResponseFactory = new Mock<ICommandResponseFactory>();
            _dispatcher = new ALCommandDispatcher(_mockRegistry.Object, _mockResponseFactory.Object);
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
            Assert.IsInstanceOf<ArchiveLinkResult>(result);
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

            Assert.IsInstanceOf<ArchiveLinkResult>(result);
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

            Assert.IsInstanceOf<ArchiveLinkResult>(result);
        }
    }

}
