namespace SAPArchiveLink
{
    public interface IBaseServices
    {
        Task<ICommandResponse> PutCert(String authId, Stream inputStream, string contRepId, String permissions);
        Task<ICommandResponse> DoGetSapDocument(SapDocumentRequest sapDocumentRequest);
    }
}
