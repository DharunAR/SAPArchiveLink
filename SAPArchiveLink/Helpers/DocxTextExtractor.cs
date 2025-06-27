using DocumentFormat.OpenXml.Packaging;
namespace SAPArchiveLink
{
    public class DocxTextExtractor : ITextExtractor
    {
        public string ExtractText(Stream stream)
        {
            using var wordDoc = WordprocessingDocument.Open(stream, false);
            return wordDoc.MainDocumentPart?.Document?.InnerText ?? "";
        }
    }
}
