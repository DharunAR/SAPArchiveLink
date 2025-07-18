using SAPArchiveLink.Interfaces;

namespace SAPArchiveLink
{
    public class TextDocumentAppender: IDocumentAppender
    {
        /// <summary>
        /// Appends the content of a new stream to an existing stream and returns a new stream containing both.
        /// </summary>
        /// <param name="existingStream"></param>
        /// <param name="newContentStream"></param>
        /// <returns></returns>
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
