using System.Text;
using Microsoft.Extensions.Options;
using Moq;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class DownloadFileHandlerTests
    {
        private DownloadFileHandler _handler;
        private Mock<IOptionsMonitor<TrimConfigSettings>> _configMock;
        private Mock<ILogHelper<DownloadFileHandler>> _logHelperMock;
        private string _workPath;

        [SetUp]
        public void SetUp()
        {
            _workPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_workPath);

            var configSettings = new TrimConfigSettings { WorkPath = _workPath };
            _configMock = new Mock<IOptionsMonitor<TrimConfigSettings>>();
            _configMock.Setup(c => c.CurrentValue).Returns(configSettings);

            _logHelperMock = new Mock<ILogHelper<DownloadFileHandler>>();

            _handler = new DownloadFileHandler(_configMock.Object, _logHelperMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_workPath))
                Directory.Delete(_workPath, true);
        }

        [Test]
        public async Task DownloadDocument_CreatesFileAndReturnsPath()
        {
            var filePath = Path.Combine(_workPath, "test.txt");
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Hello World"));

            var result = await _handler.DownloadDocument(stream, filePath);

            Assert.That(File.Exists(filePath), Is.True);
            Assert.That(result, Is.EqualTo(filePath));
        }

        [Test]
        public void DeleteFile_DeletesExistingFile()
        {
            var filePath = Path.Combine(_workPath, "delete.txt");
            File.WriteAllText(filePath, "data");

            _handler.DeleteFile(filePath);

            Assert.That(File.Exists(filePath), Is.False);
        }

        [Test]
        public async Task HandleRequestAsync_Singlepart_ReturnsComponent()
        {
            var docId = "doc1";
            var contentType = "text/plain";
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("data"));

            var result = await _handler.HandleRequestAsync(contentType, stream, docId);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].FileName, Does.Contain(docId));
            Assert.That(result[0].ContentType, Is.EqualTo(contentType));
        }

        [Test]
        public void ClearAllFiles_RemovesFilesInDirectory()
        {
            var uploadsDir = Path.Combine(_workPath, "Uploads");
            Directory.CreateDirectory(uploadsDir);
            var file1 = Path.Combine(uploadsDir, "file1.txt");
            File.WriteAllText(file1, "data");

            _handler.ClearAllFiles();

            Assert.That(File.Exists(file1), Is.False);
        }

        [Test]
        public void Constructor_ThrowsIfWorkPathNotSet()
        {
            var configSettings = new TrimConfigSettings { WorkPath = null };
            var configMock = new Mock<IOptionsMonitor<TrimConfigSettings>>();
            configMock.Setup(c => c.CurrentValue).Returns(configSettings);

            Assert.Throws<InvalidOperationException>(() =>
                new DownloadFileHandler(configMock.Object, _logHelperMock.Object));
        }

        [Test]
        public void Constructor_ThrowsIfLogHelperNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DownloadFileHandler(_configMock.Object, null));
        }

        [Test]
        public async Task ParseMultipartManuallyAsync_ThrowsIfBoundaryMissing()
        {
            var contentType = "multipart/form-data";
            using var stream = new MemoryStream();

            var methodInfo = _handler.GetType()
                .GetMethod("ParseMultipartManuallyAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (methodInfo == null)
            {
                Assert.Fail("Method 'ParseMultipartManuallyAsync' not found.");
                return;
            }

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                var task = (Task<List<SapDocumentComponentModel>>)methodInfo.Invoke(_handler, new object[] { contentType, stream });
                await task;
            });

            Assert.That(ex.Message, Is.EqualTo("Boundary not found in Content-Type."));
        }

        [Test]
        public async Task ParseMultipartManuallyAsync_ParsesSectionWithFileNameAndExtension()
        {
            var boundary = "----WebKitFormBoundary7MA4YWxkTrZu0gW";
            var contentType = $"multipart/form-data; boundary={boundary}";
            var fileName = "test.txt";
            var fileContent = "Hello World";
            var multipartContent = $"--{boundary}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"{fileName}\"\r\nContent-Type: text/plain\r\n\r\n{fileContent}\r\n--{boundary}--\r\n";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(multipartContent));

            var result = await (Task<List<SapDocumentComponentModel>>)_handler.GetType()
                .GetMethod("ParseMultipartManuallyAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_handler, new object[] { contentType, stream });

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].FileName, Does.EndWith(fileName));
            Assert.That(result[0].Data, Is.Not.Null);
        }

        [Test]
        public async Task ParseMultipartManuallyAsync_ParsesSectionWithoutFileName_UsesCompId()
        {
            var boundary = "----TestBoundary";
            var contentType = $"multipart/form-data; boundary={boundary}";
            var compId = "comp123";
            var fileContent = "data";
            var multipartContent = $"--{boundary}\r\nContent-Disposition: form-data; name=\"file\"\r\nX-compId: {compId}\r\nContent-Type: application/pdf\r\n\r\n{fileContent}\r\n--{boundary}--\r\n";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(multipartContent));

            var result = await (Task<List<SapDocumentComponentModel>>)_handler.GetType()
                .GetMethod("ParseMultipartManuallyAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_handler, new object[] { contentType, stream });

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].FileName, Does.Contain(compId));
            Assert.That(result[0].ContentType, Is.EqualTo("application/pdf"));
        }

        [Test]
        public async Task ParseMultipartManuallyAsync_ParsesSectionWithFileNameAndWordExtension()
        {
            var boundary = "----WebKitFormBoundary7MA4YWxkTrZu0gW";
            var contentType = $"multipart/form-data; boundary={boundary}";
            var fileName = "test";
            var fileContent = "Hello World";
            var multipartContent = $"--{boundary}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"{fileName}\"\r\nContent-Type: application/vnd.openxmlformats-officedocument.wordprocessingml.document\r\n\r\n{fileContent}\r\n--{boundary}--\r\n";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(multipartContent));

            var result = await (Task<List<SapDocumentComponentModel>>)_handler.GetType()
                .GetMethod("ParseMultipartManuallyAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_handler, new object[] { contentType, stream });

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].FileName, Does.EndWith(fileName+".docx"));
            Assert.That(result[0].Data, Is.Not.Null);
        }
    }
}
