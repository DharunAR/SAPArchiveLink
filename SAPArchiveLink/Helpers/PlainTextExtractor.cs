using System.Text;

namespace SAPArchiveLink
{
    public class PlainTextExtractor : ITextExtractor
    {
        public string ExtractText(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);
            return reader.ReadToEnd();
        }
    }
}
