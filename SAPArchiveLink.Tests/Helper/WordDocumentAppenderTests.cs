using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class WordDocumentAppenderTests
    {
        private MemoryStream CreateWordDocumentWithText(string text)
        {
            var stream = new MemoryStream();
            using (var wordDoc = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text(text)))));
                mainPart.Document.Save();
            }
            stream.Position = 0;
            return stream;
        }

        private string GetDocumentText(Stream docStream)
        {
            docStream.Position = 0;
            using (var wordDoc = WordprocessingDocument.Open(docStream, false))
            {
                var body = wordDoc.MainDocumentPart.Document.Body;
                return body.InnerText;
            }
        }

        [Test]
        public async Task AppendAsync_AppendsNewContentToExistingDocument()
        {
            // Arrange
            var existingStream = CreateWordDocumentWithText("Hello");
            var newContentStream = CreateWordDocumentWithText("World");
            var appender = new WordDocumentAppender();

            // Act
            var resultStream = await appender.AppendAsync(existingStream, newContentStream);

            // Assert
            var resultText = GetDocumentText(resultStream);
            Assert.That(resultText, Does.Contain("Hello"));
            Assert.That(resultText, Does.Contain("World"));
            Assert.That(resultText, Is.EqualTo("HelloWorld"));
        }
    }
}
