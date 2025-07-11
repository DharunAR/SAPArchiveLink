using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using SAPArchiveLink.Interfaces;

namespace SAPArchiveLink
{
    public class PdfDocumentAppender: IDocumentAppender
    {
        public async Task<Stream> AppendAsync(Stream original, Stream additional)
        {
            // Read streams into memory asynchronously (required if original/additional are non-seekable like HttpRequest.Body)
            var originalCopy = new MemoryStream();
            var additionalCopy = new MemoryStream();

            await original.CopyToAsync(originalCopy);
            await additional.CopyToAsync(additionalCopy);

            originalCopy.Position = 0;
            additionalCopy.Position = 0;

            using var originalDoc = PdfReader.Open(originalCopy, PdfDocumentOpenMode.Import);
            using var additionalDoc = PdfReader.Open(additionalCopy, PdfDocumentOpenMode.Import);

            var outputDoc = new PdfDocument();

            // Append original pages
            foreach (var page in originalDoc.Pages)
                outputDoc.AddPage(page);

            // Append additional pages
            foreach (var page in additionalDoc.Pages)
                outputDoc.AddPage(page);

            var outputStream = new MemoryStream();
            outputDoc.Save(outputStream, false);
            outputStream.Position = 0;

            return outputStream;
        }
    }
}
