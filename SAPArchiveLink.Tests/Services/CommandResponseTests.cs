using NUnit.Framework;
using System.IO;
using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Http;
using SAPArchiveLink;
using System.Collections.Generic;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class CommandResponseTests
    {
        [Test]
        public void ForProtocolText_ShouldReturnTextContentResponse()
        {
            var response = CommandResponse.ForProtocolText("Test Response", StatusCodes.Status202Accepted);

            Assert.That(response.TextContent, Is.EqualTo("Test Response"));
            Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status202Accepted));
            Assert.That(response.ContentType, Is.EqualTo("text/plain; charset=UTF-8"));
            Assert.That(response.IsStream, Is.False);
        }

        [Test]
        public void ForHtmlReport_ShouldReturnHtmlContentResponse()
        {
            var response = CommandResponse.ForHtmlReport("<html>Test</html>");

            Assert.That(response.TextContent, Is.EqualTo("<html>Test</html>"));
            Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            Assert.That(response.ContentType, Is.EqualTo("text/html; charset=UTF-8"));
            Assert.That(response.IsStream, Is.False);
        }

        [Test]
        public void ForDocumentContent_ShouldReturnStreamContentResponse()
        {
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("binary data"));
            var response = CommandResponse.ForDocumentContent(memoryStream, MediaTypeNames.Application.Pdf, StatusCodes.Status206PartialContent);

            Assert.That(response.StreamContent, Is.EqualTo(memoryStream));
            Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status206PartialContent));
            Assert.That(response.ContentType, Is.EqualTo(MediaTypeNames.Application.Pdf));
            Assert.That(response.IsStream, Is.True);
        }

        [Test]
        public void ForMultipartDocument_ShouldReturnMultipartResponse()
        {
            var components = new List<SapDocumentComponentModel> { new SapDocumentComponentModel() };
            var response = CommandResponse.ForMultipartDocument(components);

            Assert.That(response.Components, Is.EqualTo(components));
            Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            Assert.That(response.ContentType, Does.StartWith("multipart/form-data; boundary="));
            Assert.That(response.IsStream, Is.True);
            Assert.That(response.Boundary, Is.Not.Empty);
        }

        [Test]
        public void ForInfoMetadata_ShouldReturnMultipartInfoResponse()
        {
            var components = new List<SapDocumentComponentModel> { new SapDocumentComponentModel() };
            var response = CommandResponse.ForInfoMetadata(components);

            Assert.That(response.Components, Is.EqualTo(components));
            Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            Assert.That(response.ContentType, Does.StartWith("multipart/form-data; boundary="));
            Assert.That(response.IsStream, Is.False);
            Assert.That(response.Boundary, Is.Not.Empty);
        }

        [Test]
        public void ForError_ShouldReturnErrorTextContentResponse()
        {
            var response = CommandResponse.ForError("Something went wrong", StatusCodes.Status500InternalServerError);

            Assert.That(response.ErrorContent, Is.EqualTo("ErrorMessage=Something went wrong"));
            Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
            Assert.That(response.ContentType, Is.EqualTo("text/plain; charset=UTF-8"));
            Assert.That(response.IsStream, Is.False);
            Assert.That(response.Headers.ContainsKey("X-ErrorDescription"), Is.True);
        }

        [Test]
        public void AddHeader_ShouldAddCustomHeader()
        {
            var response = CommandResponse.ForProtocolText();
            response.AddHeader("X-Test", "123");

            Assert.That(response.Headers.ContainsKey("X-Test"), Is.True);
            Assert.That(response.Headers["X-Test"], Is.EqualTo("123"));
        }
    }
}
