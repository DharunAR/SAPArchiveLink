using SAPArchiveLink.Interfaces;

namespace SAPArchiveLink
{
    public class DocumentAppenderFactory
    {
        private static readonly Dictionary<string, IDocumentAppender> _appender =
        new(StringComparer.OrdinalIgnoreCase);

        public static void Register(string contentType, IDocumentAppender appender)
        {
            if (string.IsNullOrWhiteSpace(contentType) || appender is null)
                throw new ArgumentException("Invalid appender registration.");

            var normalized = contentType.Split(';')[0].Trim().ToLowerInvariant();
            _appender[normalized] = appender;
        }

        public static IDocumentAppender? GetAppender(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return null;

            var normalized = extension.Split(';')[0].Trim().ToLowerInvariant();
            return _appender.TryGetValue(normalized, out var extractor) ? extractor : null;
        }
    }
}
