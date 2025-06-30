namespace SAPArchiveLink
{
    public static class TextExtractorFactory
    {
        public static ITextExtractor? GetExtractor(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
                return null;

            contentType = contentType.Split(';')[0].Trim().ToLowerInvariant();

            return contentType switch
            {
                "text/plain" => new PlainTextExtractor(),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => new DocxTextExtractor(),
               "application/pdf" => new PdfTextExtractor(),
                 "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => new ExcelTextExtractor(),
                _ => null
            };
        }
    }
}
