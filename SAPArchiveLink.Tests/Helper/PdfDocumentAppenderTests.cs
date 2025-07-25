using NUnit.Framework;
using SAPArchiveLink;
using System.IO;
using System.Threading.Tasks;
using PdfSharpCore.Pdf;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class PdfDocumentAppenderTests
    {
        private static MemoryStream CreatePdfWithPages(int pageCount)
        {
            var doc = new PdfDocument();
            for (int i = 0; i < pageCount; i++)
            {
                doc.AddPage();
            }
            var ms = new MemoryStream();
            // Always save, even if there are zero pages
            doc.Save(ms, false);
            ms.Position = 0;
            return ms;
        }

        [Test]
        public async Task AppendAsync_AppendsPagesFromBothStreams()
        {
            // Arrange
            var appender = new PdfDocumentAppender();
            var existingPdf = CreatePdfWithPages(2);
            var newPdf = CreatePdfWithPages(3);

            // Act
            var resultStream = await appender.AppendAsync(existingPdf, newPdf);

            // Assert
            resultStream.Position = 0;
            var resultDoc = PdfSharpCore.Pdf.IO.PdfReader.Open(resultStream, PdfSharpCore.Pdf.IO.PdfDocumentOpenMode.Import);
            Assert.That(resultDoc.PageCount, Is.EqualTo(5));
        }    

    }
}
