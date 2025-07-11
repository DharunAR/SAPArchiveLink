namespace SAPArchiveLink.Interfaces
{
    public interface IDocumentAppender
    {
        Task<Stream> AppendAsync(Stream existingStream, Stream newContentStream);
    }
}
