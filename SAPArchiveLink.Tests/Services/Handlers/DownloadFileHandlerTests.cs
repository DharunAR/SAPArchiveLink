using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using SAPArchiveLink;

namespace SAPArchiveLink.Tests.Services.Handlers
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
    }
}
