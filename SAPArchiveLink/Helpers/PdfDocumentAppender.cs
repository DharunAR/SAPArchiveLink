using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using SAPArchiveLink.Interfaces;

namespace SAPArchiveLink
{
    public class PdfDocumentAppender: IDocumentAppender
    {
        /// <summary>
        /// Asynchronously appends the content of a new PDF stream to an existing PDF stream.
        /// </summary>
        /// <param name="existingStream"></param>
        /// <param name="newContentStream"></param>
        /// <returns></returns>
        public async Task<Stream> AppendAsync(Stream existingStream, Stream newContentStream)
        {
            // Read streams into memory asynchronously (required if original/additional are non-seekable like HttpRequest.Body)
            var originalCopy = new MemoryStream();
            var additionalCopy = new MemoryStream();

            await existingStream.CopyToAsync(originalCopy);
            await newContentStream.CopyToAsync(additionalCopy);

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
