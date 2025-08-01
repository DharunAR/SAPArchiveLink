using DocumentFormat.OpenXml.Packaging;
using System.Text;
using Draw = DocumentFormat.OpenXml.Drawing;

namespace SAPArchiveLink
{
    public class PowerPointTextExtractor: ITextExtractor
    {
        /// <summary>
        /// Extracts text from a PowerPoint file stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public string ExtractText(Stream stream)
        {
            using var presentation = PresentationDocument.Open(stream, false);
            var presentationPart = presentation.PresentationPart;
            if (presentationPart == null)
                return string.Empty;

            var sb = new StringBuilder();

            foreach (var slidePart in presentationPart.SlideParts)
            {
                var texts = slidePart.Slide.Descendants<Draw.Text>();
                foreach (var text in texts)
                {
                    sb.AppendLine(text.Text);
                }
            }

            return sb.ToString();
        }
    }    
}
