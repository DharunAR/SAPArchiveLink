using SAPArchiveLink.Interfaces;

namespace SAPArchiveLink
{
    public class TextDocumentAppender: IDocumentAppender
    {
        public async Task<Stream> AppendAsync(Stream existingStream, Stream newContentStream)
        {
            var output = new MemoryStream();

            await existingStream.CopyToAsync(output);
            await newContentStream.CopyToAsync(output);

            output.Position = 0;
            return output;
        }
    }
}
