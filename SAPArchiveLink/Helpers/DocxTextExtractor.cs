using DocumentFormat.OpenXml.Packaging;
namespace SAPArchiveLink
{
    public class DocxTextExtractor : ITextExtractor
    {
        /// <summary>
        /// Extracts text from a DOCX file stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public string ExtractText(Stream stream)
        {
            using var wordDoc = WordprocessingDocument.Open(stream, false);
            return wordDoc.MainDocumentPart?.Document?.InnerText ?? "";
        }
    }
}
