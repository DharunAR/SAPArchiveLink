using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf;
using System.Text;

namespace SAPArchiveLink.Helpers
{
    public class PdfTextExtractor : ITextExtractor
    {
        public string ExtractText(Stream stream)
        {
            using var pdfReader = new PdfReader(stream);
            using var pdfDoc = new PdfDocument(pdfReader);
            var sb = new StringBuilder();

            for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
            {
                var strategy = new SimpleTextExtractionStrategy();                
                string text = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i), strategy);
                sb.AppendLine(text);
            }

            return sb.ToString();
        }
    }
}
