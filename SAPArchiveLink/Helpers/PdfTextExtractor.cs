using UglyToad.PdfPig;

namespace SAPArchiveLink
{
    public class PdfTextExtractor : ITextExtractor
    {
        public string ExtractText(Stream stream)
        {
            using var pdf = PdfDocument.Open(stream);
            return string.Join("\n", pdf.GetPages().Select(p => p.Text));
        }
    }
}
