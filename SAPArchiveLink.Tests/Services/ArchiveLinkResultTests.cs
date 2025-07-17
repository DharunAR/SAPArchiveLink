using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework.Legacy;
using System.Text;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class ArchiveLinkResultTests
    {
        private Mock<ICommandResponse> _mockResponse;
        private MemoryStream _bodyStream;
        private ActionContext _context;

        [SetUp]
        public void SetUp()
        {
            _mockResponse = new Mock<ICommandResponse>();
            _context = CreateActionContext(out _bodyStream);
        }

        [Test]
        public void Constructor_NullResponse_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ArchiveLinkResult(null!));
        }

        [Test]
        public async Task ExecuteResultAsync_TextContent_WritesPlainText()
        {
            _mockResponse.Setup(r => r.StatusCode).Returns(200);
            _mockResponse.Setup(r => r.ContentType).Returns("text/plain");
            _mockResponse.Setup(r => r.IsStream).Returns(false);
            _mockResponse.Setup(r => r.TextContent).Returns("Hello ArchiveLink!");
            _mockResponse.Setup(r => r.Headers).Returns(new Dictionary<string, string>());

            var result = new ArchiveLinkResult(_mockResponse.Object);
            await result.ExecuteResultAsync(_context);

            AssertResponse(200, "text/plain", "Hello ArchiveLink!");
        }

        [Test]
        public async Task ExecuteResultAsync_TextContent_WithHeaders_AddsHeaders()
        {
            var headers = new Dictionary<string, string> { { "X-Test", "Value" } };

            _mockResponse.Setup(r => r.StatusCode).Returns(200);
            _mockResponse.Setup(r => r.ContentType).Returns("text/html");
            _mockResponse.Setup(r => r.IsStream).Returns(false);
            _mockResponse.Setup(r => r.TextContent).Returns("<html>Test</html>");
            _mockResponse.Setup(r => r.Headers).Returns(headers);

            var result = new ArchiveLinkResult(_mockResponse.Object);
            await result.ExecuteResultAsync(_context);

            Assert.That(_context.HttpContext.Response.Headers["X-Test"], Is.EqualTo("Value"));
        }

        [Test]
        public async Task ExecuteResultAsync_StreamContent_WritesBinary()
        {
            var contentBytes = Encoding.UTF8.GetBytes("Binary Data");

            _mockResponse.Setup(r => r.StatusCode).Returns(200);
            _mockResponse.Setup(r => r.ContentType).Returns("application/octet-stream");
            _mockResponse.Setup(r => r.IsStream).Returns(true);
            _mockResponse.Setup(r => r.StreamContent).Returns(new MemoryStream(contentBytes));
            _mockResponse.Setup(r => r.Components).Returns((List<SapDocumentComponentModel>)null!);
            _mockResponse.Setup(r => r.Headers).Returns(new Dictionary<string, string>());

            var result = new ArchiveLinkResult(_mockResponse.Object);
            await result.ExecuteResultAsync(_context);

            var output = GetOutputBytes();
            Assert.That(output, Is.EqualTo(contentBytes));
        }

        [Test]
        public async Task ExecuteResultAsync_MultipartComponents_WritesMultipartData()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes("PART DATA"));
            var component = CreateTestComponent(stream);

            _mockResponse.Setup(r => r.StatusCode).Returns(206);
            _mockResponse.Setup(r => r.ContentType).Returns("multipart/form-data; boundary=BOUND123");
            _mockResponse.Setup(r => r.IsStream).Returns(true);
            _mockResponse.Setup(r => r.StreamContent).Returns((Stream)null!);
            _mockResponse.Setup(r => r.Components).Returns(new List<SapDocumentComponentModel> { component });
            _mockResponse.Setup(r => r.Boundary).Returns("BOUND123");
            _mockResponse.Setup(r => r.Headers).Returns(new Dictionary<string, string>());

            var result = new ArchiveLinkResult(_mockResponse.Object);
            await result.ExecuteResultAsync(_context);

            var output = GetOutputText();
            Assert.That(output, Does.Contain("--BOUND123"));
            Assert.That(output, Does.Contain("Content-Type: application/pdf; charset=utf-8; version=1.0"));
            Assert.That(output, Does.Contain("X-compId: Comp1"));
            Assert.That(output, Does.Contain("PART DATA"));
            Assert.That(output, Does.Contain("--BOUND123--"));
        }

        [Test]
        public async Task ExecuteResultAsync_MetadataOnlyInfoCommand_WritesHeadersOnly()
        {
            var component = CreateTestComponent(null);
            component.Data = null;
            component.ContentLength = 0;

            _mockResponse.Setup(r => r.StatusCode).Returns(200);
            _mockResponse.Setup(r => r.ContentType).Returns("multipart/form-data; boundary=BOUND999");
            _mockResponse.Setup(r => r.IsStream).Returns(false);
            _mockResponse.Setup(r => r.StreamContent).Returns((Stream)null!);
            _mockResponse.Setup(r => r.Components).Returns(new List<SapDocumentComponentModel> { component });
            _mockResponse.Setup(r => r.Boundary).Returns("BOUND999");
            _mockResponse.Setup(r => r.Headers).Returns(new Dictionary<string, string>());

            var result = new ArchiveLinkResult(_mockResponse.Object);
            await result.ExecuteResultAsync(_context);

            var output = GetOutputText();
            Assert.That(output, Does.Contain("--BOUND999"));
            Assert.That(output, Does.Not.Contain("PART DATA")); // No content expected
        }

        [Test]
        public async Task ExecuteResultAsync_CallsClearExtractedFiles_IfFileExists()
        {
            var testFile = Path.GetTempFileName();
            File.WriteAllText(testFile, "temp");
            try
            {
                var stream = new MemoryStream(Encoding.ASCII.GetBytes("PART"));
                var component = CreateTestComponent(stream);
                component.FileName = testFile;

                var mockHandler = new Mock<IDownloadFileHandler>();
                mockHandler.Setup(h => h.DeleteFile(It.IsAny<string>())).Verifiable();

                _mockResponse.Setup(r => r.StatusCode).Returns(206);
                _mockResponse.Setup(r => r.ContentType).Returns("multipart/form-data; boundary=TEST");
                _mockResponse.Setup(r => r.IsStream).Returns(true);
                _mockResponse.Setup(r => r.StreamContent).Returns((Stream)null!);
                _mockResponse.Setup(r => r.Components).Returns(new List<SapDocumentComponentModel> { component });
                _mockResponse.Setup(r => r.Boundary).Returns("TEST");
                _mockResponse.Setup(r => r.Headers).Returns(new Dictionary<string, string>());

                var result = new ArchiveLinkResult(_mockResponse.Object, mockHandler.Object);
                await result.ExecuteResultAsync(_context);

                mockHandler.Verify(h => h.DeleteFile(testFile), Times.Once);
            }
            finally
            {
                if (File.Exists(testFile))
                {
                    File.Delete(testFile); 
                }
            }
        }

        // --- Helpers ---

        private ActionContext CreateActionContext(out MemoryStream bodyStream)
        {
            var context = new DefaultHttpContext();
            bodyStream = new MemoryStream();
            context.Response.Body = bodyStream;
            return new ActionContext { HttpContext = context };
        }

        private SapDocumentComponentModel CreateTestComponent(Stream? data)
        {
            return new SapDocumentComponentModel
            {
                Data = data,
                ContentType = "application/pdf",
                Charset = "utf-8",
                Version = "1.0",
                ContentLength = data?.Length ?? 0,
                CompId = "Comp1",
                CreationDate = new DateTime(2024, 6, 1, 8, 30, 0, DateTimeKind.Utc),
                ModifiedDate = new DateTime(2024, 6, 1, 9, 0, 0, DateTimeKind.Utc),
                Status = "COMPLETE",
                PVersion = "v1",
                FileName = ""
            };
        }

        private string GetOutputText()
        {
            _bodyStream.Seek(0, SeekOrigin.Begin);
            return new StreamReader(_bodyStream).ReadToEnd();
        }

        private byte[] GetOutputBytes()
        {
            _bodyStream.Seek(0, SeekOrigin.Begin);
            return _bodyStream.ToArray();
        }

        private void AssertResponse(int statusCode, string contentType, string body)
        {
            Assert.That(_context.HttpContext.Response.StatusCode, Is.EqualTo(statusCode));
            Assert.That(_context.HttpContext.Response.ContentType, Is.EqualTo(contentType));

            var output = GetOutputText();
            Assert.That(output, Is.EqualTo(body));
        }
    }

}
