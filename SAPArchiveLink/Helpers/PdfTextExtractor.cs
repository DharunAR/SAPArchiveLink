using UglyToad.PdfPig;

namespace SAPArchiveLink
{
    public class PdfTextExtractor : ITextExtractor
    {
        /// <summary>
        /// Extracts text from a PDF stream using PdfPig library.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public string ExtractText(Stream stream)
        {
            using var pdf = PdfDocument.Open(stream);
            return string.Join("\n", pdf.GetPages().Select(p => p.Text));
        }
    }
}
