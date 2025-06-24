using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text;

namespace SAPArchiveLink.Tests
{
    public class ArchiveLinkResultTests
    {
        [Test]
        public void Constructor_NullResponse_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ArchiveLinkResult(null));
        }

        [Test]
        public async Task ExecuteResultAsync_TextContent_WritesPlainText()
        {
            // Arrange
            var mockResponse = new Mock<ICommandResponse>();
            mockResponse.Setup(r => r.StatusCode).Returns(200);
            mockResponse.Setup(r => r.ContentType).Returns("text/plain");
            mockResponse.Setup(r => r.IsStream).Returns(false);
            mockResponse.Setup(r => r.TextContent).Returns("Hello ArchiveLink!");
            mockResponse.Setup(r => r.Headers).Returns(new Dictionary<string, string>());

            var context = CreateActionContext(out var bodyStream);
            var result = new ArchiveLinkResult(mockResponse.Object);

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            bodyStream.Seek(0, SeekOrigin.Begin);
            var output = new StreamReader(bodyStream).ReadToEnd();
            Assert.AreEqual("Hello ArchiveLink!", output);
            Assert.AreEqual("text/plain", context.HttpContext.Response.ContentType);
            Assert.AreEqual(200, context.HttpContext.Response.StatusCode);
        }

        [Test]
        public async Task ExecuteResultAsync_StreamContent_WritesBinary()
        {
            var contentBytes = Encoding.UTF8.GetBytes("Binary Data");

            var mockResponse = new Mock<ICommandResponse>();
            mockResponse.Setup(r => r.StatusCode).Returns(200);
            mockResponse.Setup(r => r.ContentType).Returns("application/octet-stream");
            mockResponse.Setup(r => r.IsStream).Returns(true);
            mockResponse.Setup(r => r.StreamContent).Returns(new MemoryStream(contentBytes));
            mockResponse.Setup(r => r.Components).Returns((List<SapDocumentComponentModel>)null);
            mockResponse.Setup(r => r.Headers).Returns(new Dictionary<string, string>());

            var context = CreateActionContext(out var bodyStream);
            var result = new ArchiveLinkResult(mockResponse.Object);

            await result.ExecuteResultAsync(context);

            bodyStream.Seek(0, SeekOrigin.Begin);
            var output = bodyStream.ToArray();
            CollectionAssert.AreEqual(contentBytes, output);
        }

        [Test]
        public async Task ExecuteResultAsync_MultipartComponents_WritesMultipartData()
        {
            // Arrange
            var componentData = Encoding.ASCII.GetBytes("PART DATA");
            var stream = new MemoryStream(componentData);

            var component = new SapDocumentComponentModel
            {
                Data = stream,
                ContentType = "application/pdf",
                Charset = "utf-8",
                Version = "1.0",
                ContentLength = stream.Length,
                CompId = "Comp1",
                CreationDate = new DateTime(2024, 6, 1, 8, 30, 0),
                ModifiedDate = new DateTime(2024, 6, 1, 9, 0, 0),
                Status = "COMPLETE",
                PVersion = "v1"
            };

            var mockResponse = new Mock<ICommandResponse>();
            mockResponse.Setup(r => r.StatusCode).Returns(206);
            mockResponse.Setup(r => r.ContentType).Returns("multipart/form-data; boundary=BOUND123");
            mockResponse.Setup(r => r.IsStream).Returns(true);
            mockResponse.Setup(r => r.StreamContent).Returns((Stream)null);
            mockResponse.Setup(r => r.Components).Returns(new List<SapDocumentComponentModel> { component });
            mockResponse.Setup(r => r.Boundary).Returns("BOUND123");
            mockResponse.Setup(r => r.Headers).Returns(new Dictionary<string, string>());

            var context = CreateActionContext(out var bodyStream);
            var result = new ArchiveLinkResult(mockResponse.Object);

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            bodyStream.Seek(0, SeekOrigin.Begin);
            var output = new StreamReader(bodyStream).ReadToEnd();

            StringAssert.Contains("--BOUND123", output);
            StringAssert.Contains("Content-Type: application/pdf; charset=utf-8; version=1.0", output);
            StringAssert.Contains("X-compId: Comp1", output);
            StringAssert.Contains("PART DATA", output);
            StringAssert.Contains("--BOUND123--", output);
        }

        private ActionContext CreateActionContext(out MemoryStream bodyStream)
        {
            var context = new DefaultHttpContext();
            bodyStream = new MemoryStream();
            context.Response.Body = bodyStream;
            return new ActionContext { HttpContext = context };
        }
    }
}
