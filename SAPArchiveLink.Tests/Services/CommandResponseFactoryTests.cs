using NUnit.Framework;
using SAPArchiveLink;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class CommandResponseFactoryTests
    {
        private CommandResponseFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = new CommandResponseFactory();
        }

        [Test]
        public void CreateProtocolText_ShouldReturnExpectedCommandResponse()
        {
            var result = _factory.CreateProtocolText("Test Protocol", StatusCodes.Status202Accepted);

            Assert.That(result.TextContent, Is.EqualTo("Test Protocol"));
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status202Accepted));
            Assert.That(result.ContentType, Is.EqualTo("text/plain; charset=UTF-8"));
            Assert.That(result.IsStream, Is.False);
        }

        [Test]
        public void CreateHtmlReport_ShouldReturnExpectedHtmlResponse()
        {
            var result = _factory.CreateHtmlReport("<html>Hello</html>");

            Assert.That(result.TextContent, Is.EqualTo("<html>Hello</html>"));
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            Assert.That(result.ContentType, Is.EqualTo("text/html; charset=UTF-8"));
            Assert.That(result.IsStream, Is.False);
        }

        [Test]
        public void CreateDocumentContent_ShouldReturnExpectedStreamResponse()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("PDF content"));
            var result = _factory.CreateDocumentContent(stream, "application/pdf", StatusCodes.Status206PartialContent, "report.pdf");

            Assert.That(result.StreamContent, Is.EqualTo(stream));
            Assert.That(result.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status206PartialContent));
            Assert.That(result.IsStream, Is.True);
        }

        [Test]
        public void CreateMultipartDocument_ShouldReturnExpectedMultipartResponse()
        {
            var components = new List<SapDocumentComponentModel> { new SapDocumentComponentModel() };
            var result = _factory.CreateMultipartDocument(components);

            Assert.That(result.Components, Is.EqualTo(components));
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            Assert.That(result.ContentType, Does.StartWith("multipart/form-data; boundary="));
            Assert.That(result.Boundary, Is.Not.Null.And.Not.Empty);
            Assert.That(result.IsStream, Is.True);
        }

        [Test]
        public void CreateInfoMetadata_ShouldReturnExpectedInfoMetadataResponse()
        {
            var components = new List<SapDocumentComponentModel> { new SapDocumentComponentModel() };
            var result = _factory.CreateInfoMetadata(components);

            Assert.That(result.Components, Is.EqualTo(components));
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            Assert.That(result.ContentType, Does.StartWith("multipart/form-data; boundary="));
            Assert.That(result.Boundary, Is.Not.Null.And.Not.Empty);
            Assert.That(result.IsStream, Is.False);
        }

        [Test]
        public void CreateError_ShouldReturnExpectedErrorResponse()
        {
            var result = _factory.CreateError("Something failed", StatusCodes.Status500InternalServerError);

            Assert.That(result.TextContent, Is.EqualTo("ErrorMessage=Something failed"));
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
            Assert.That(result.ContentType, Is.EqualTo("text/plain; charset=UTF-8"));
            Assert.That(result.IsStream, Is.False);
            Assert.That(result.Headers, Does.ContainKey("X-ErrorDescription"));
        }
    }
}
