namespace SAPArchiveLink
{
    public static class TextExtractorFactory
    {
        private static readonly Dictionary<string, ITextExtractor> _extractors =
         new(StringComparer.OrdinalIgnoreCase);

        public static void Register(string contentType, ITextExtractor extractor)
        {
            if (string.IsNullOrWhiteSpace(contentType) || extractor is null)
                throw new ArgumentException("Invalid extractor registration.");
            
            var normalized = contentType.Split(';')[0].Trim().ToLowerInvariant();
            _extractors[normalized] = extractor;
        }

        public static ITextExtractor? GetExtractor(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
                return null;

            var normalized = contentType.Split(';')[0].Trim().ToLowerInvariant();
            return _extractors.TryGetValue(normalized, out var extractor) ? extractor : null;
        }
    }
}
