using System.Text;

namespace SAPArchiveLink
{
    public class PlainTextExtractor : ITextExtractor
    {
        /// <summary>
        /// Extracts plain text from a given stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public string ExtractText(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);
            return reader.ReadToEnd();
        }
    }
}
