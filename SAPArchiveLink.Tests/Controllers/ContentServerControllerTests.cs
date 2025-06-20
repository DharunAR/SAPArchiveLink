using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SAPArchiveLink.Controllers;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class ContentServerControllerTests
    {
        private Mock<ICommandDispatcherService> _dispatcherMock;
        private Mock<ContentServerRequestAuthenticator> _authenticatorMock;
        private ContentServerController _controller;
        private DefaultHttpContext _httpContext;

        [SetUp]
        public void SetUp()
        {
            _dispatcherMock = new Mock<ICommandDispatcherService>();
            var verifierMock = new Mock<IVerifier>();
            var loggerMock = new Mock<ILogger<ContentServerRequestAuthenticator>>();
            _authenticatorMock = new Mock<ContentServerRequestAuthenticator>(verifierMock.Object, loggerMock.Object);
            _controller = new ContentServerController(_dispatcherMock.Object, _authenticatorMock.Object);
            _httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
        }

        [Test]
        public async Task Handle_ReturnsBadRequest_WhenQueryStringIsEmpty()
        {
            _httpContext.Request.QueryString = QueryString.Empty;
            var result = await _controller.Handle();
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequest = (BadRequestObjectResult)result;
            Assert.That(badRequest.Value, Is.EqualTo("Query string is required."));
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
            Assert.IsInstanceOf<OkResult>(result);
            _dispatcherMock.Verify(d => d.RunRequest(It.Is<CommandRequest>(c => c.Charset == "UTF-8"), It.IsAny<ContentServerRequestAuthenticator>()), Times.Once);
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
            Assert.IsInstanceOf<OkResult>(result);
            _dispatcherMock.Verify(d => d.RunRequest(It.Is<CommandRequest>(c => c.Charset == "ISO-8859-1"), It.IsAny<ContentServerRequestAuthenticator>()), Times.Once);
        }

        [Test]
        public async Task Handle_Returns400_WhenALExceptionThrown()
        {
            _httpContext.Request.QueryString = new QueryString("?param=value");
            _httpContext.Request.ContentType = "application/json";
            _httpContext.Request.Method = "GET";
            _dispatcherMock.Setup(d => d.RunRequest(It.IsAny<CommandRequest>(), It.IsAny<ContentServerRequestAuthenticator>()))
                .ThrowsAsync(new ALException("Test error"));

            var result = await _controller.Handle();
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var objectResult = (ObjectResult)result;
            Assert.That(objectResult.StatusCode, Is.EqualTo(500));
            Assert.That(objectResult.Value.ToString(), Does.Contain("Test error"));
        }

        [Test]
        public async Task Handle_Returns500_WhenUnexpectedExceptionThrown()
        {
            _httpContext.Request.QueryString = new QueryString("?param=value");
            _httpContext.Request.ContentType = "application/json";
            _httpContext.Request.Method = "GET";
            _dispatcherMock.Setup(d => d.RunRequest(It.IsAny<CommandRequest>(), It.IsAny<ContentServerRequestAuthenticator>()))
                .ThrowsAsync(new Exception("Unexpected"));

            var result = await _controller.Handle();
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var objectResult = (ObjectResult)result;
            Assert.That(objectResult.StatusCode, Is.EqualTo(500));
            Assert.That(objectResult.Value.ToString(), Does.Contain("An unexpected error occurred."));
        }
    }

    public class ALException : Exception
    {
        public ALException(string message) : base(message) { }
    }
}