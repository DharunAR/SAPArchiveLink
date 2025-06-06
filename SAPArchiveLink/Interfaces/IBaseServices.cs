namespace SAPArchiveLink
{
    public interface IBaseServices
    {
        Task<ICommandResponse> PutCert(String authId, Stream inputStream, string contRepId, String permissions);
        Task<ICommandResponse> DocGetSapComponents(SapDocumentRequest sapDocumentRequest);
        Task<ICommandResponse> GetSapDocument(SapDocumentRequest sapDoc);
    }
}
