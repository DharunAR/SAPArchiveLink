using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SAPArchiveLink.Controllers;

namespace SAPArchiveLink.Tests
{

    [TestFixture]
    public class ContentServerControllerTests
    {
        private Mock<ICommandDispatcherService> _dispatcherMock;
        private Mock<ContentServerRequestAuthenticator> _authenticatorMock;
        private Mock<ICommandResponseFactory> _responseFactoryMock;
        private ContentServerController _controller;
        private DefaultHttpContext _httpContext;

        [SetUp]
        public void SetUp()
        {
            _dispatcherMock = new Mock<ICommandDispatcherService>();
            _responseFactoryMock = new Mock<ICommandResponseFactory>();
            var verifierMock = new Mock<IVerifier>();
            var loggerMock = new Mock<ILogHelper<ContentServerRequestAuthenticator>>();
            _authenticatorMock = new Mock<ContentServerRequestAuthenticator>(
                verifierMock.Object, loggerMock.Object, _responseFactoryMock.Object);

            _controller = new ContentServerController(_dispatcherMock.Object, _authenticatorMock.Object);
            _httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
        }

        [Test]
        public async Task Handle_ReturnsOk_WhenQueryStringIsEmpty()
        {
            _httpContext.Request.QueryString = QueryString.Empty;

            var result = await _controller.Handle();

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            var response = okResult.Value as ArchiveLinkStatusResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Message, Is.EqualTo("Content Manager SAP ArchiveLink service is running."));
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Version, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task Handle_UsesDefaultCharset_WhenContentTypeIsNull()
        {
            _httpContext.Request.QueryString = new QueryString("?param=value");
            _httpContext.Request.ContentType = null;
            _httpContext.Request.Method = "GET";

            _dispatcherMock.Setup(d => d.RunRequest(It.IsAny<CommandRequest>(), It.IsAny<ContentServerRequestAuthenticator>()))
                .ReturnsAsync(new OkResult());

            var result = await _controller.Handle();

            Assert.That(result, Is.TypeOf<OkResult>());
            _dispatcherMock.Verify(d => d.RunRequest(
                It.Is<CommandRequest>(c => c.Charset == "UTF-8"),
                It.IsAny<ContentServerRequestAuthenticator>()), Times.Once);
        }

        [Test]
        public async Task Handle_ParsesCharsetFromContentType()
        {
            _httpContext.Request.QueryString = new QueryString("?param=value");
            _httpContext.Request.ContentType = "application/json; charset=ISO-8859-1";
            _httpContext.Request.Method = "POST";

            _dispatcherMock.Setup(d => d.RunRequest(It.IsAny<CommandRequest>(), It.IsAny<ContentServerRequestAuthenticator>()))
                .ReturnsAsync(new OkResult());

            var result = await _controller.Handle();

            Assert.That(result, Is.TypeOf<OkResult>());
            _dispatcherMock.Verify(d => d.RunRequest(
                It.Is<CommandRequest>(c => c.Charset == "ISO-8859-1"),
                It.IsAny<ContentServerRequestAuthenticator>()), Times.Once);
        }

        [Test]
        public async Task Handle_ParsesCharsetWithQuotesAndWhitespace()
        {
            _httpContext.Request.QueryString = new QueryString("?param=value");
            _httpContext.Request.ContentType = "application/json; charset=\"UTF-16\" ";
            _httpContext.Request.Method = "POST";

            _dispatcherMock.Setup(d => d.RunRequest(It.IsAny<CommandRequest>(), It.IsAny<ContentServerRequestAuthenticator>()))
                .ReturnsAsync(new OkResult());

            var result = await _controller.Handle();

            Assert.That(result, Is.TypeOf<OkResult>());
            _dispatcherMock.Verify(d => d.RunRequest(
                It.Is<CommandRequest>(c => c.Charset == "\"UTF-16\""),
                It.IsAny<ContentServerRequestAuthenticator>()), Times.Once);
        }

        [Test]
        public async Task Handle_ReturnsFormattedErrorResponse_WhenExceptionIsThrown()
        {
            _httpContext.Request.QueryString = new QueryString("?param=value");
            _httpContext.Request.ContentType = "application/json";
            _httpContext.Request.Method = "GET";

            var exceptionMessage = "Simulated failure";
            _dispatcherMock.Setup(d => d.RunRequest(It.IsAny<CommandRequest>(), It.IsAny<ContentServerRequestAuthenticator>()))
                .ThrowsAsync(new Exception(exceptionMessage));

            var result = await _controller.Handle();

            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));

            var response = objectResult.Value as ArchiveLinkStatusResponse;
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Status, Is.EqualTo("Internal Server Error"));
            Assert.That(response.Message, Does.Contain(exceptionMessage));
        }


        [TestCase("GET")]
        [TestCase("POST")]
        [TestCase("PUT")]
        [TestCase("DELETE")]
        public async Task Handle_SupportsAllHttpMethods(string method)
        {
            _httpContext.Request.QueryString = new QueryString("?param=value");
            _httpContext.Request.ContentType = "application/json";
            _httpContext.Request.Method = method;

            _dispatcherMock.Setup(d => d.RunRequest(It.IsAny<CommandRequest>(), It.IsAny<ContentServerRequestAuthenticator>()))
                .ReturnsAsync(new OkResult());

            var result = await _controller.Handle();

            Assert.That(result, Is.TypeOf<OkResult>());
        }
    }

}