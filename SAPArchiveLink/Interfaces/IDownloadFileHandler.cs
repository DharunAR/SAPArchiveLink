namespace SAPArchiveLink
{
    public interface IDownloadFileHandler
    {
        Task<List<SapDocumentComponentModel>> HandleRequestAsync(string contentType, Stream body, string docId);
        Task<string> DownloadDocument(Stream stream, string filePath);
        void DeleteFile(string filePath);
    }
}
