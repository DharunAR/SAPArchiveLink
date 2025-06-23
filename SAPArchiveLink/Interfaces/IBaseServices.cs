namespace SAPArchiveLink
{
    public interface IBaseServices
    {
        Task<ICommandResponse> PutCert(String authId, Stream inputStream, string contRepId, String permissions);
        Task<ICommandResponse> CreateRecord(CreateSapDocumentModel createSapDocumentModels, bool isMultipart = false);
        Task<ICommandResponse> DocGetSapComponents(SapDocumentRequest sapDocumentRequest);
        Task<ICommandResponse> GetSapDocument(SapDocumentRequest sapDoc);
        Task<ICommandResponse> UpdateRecord(CreateSapDocumentModel createSapDocumentModels, bool isMultipart = false);
        Task<ICommandResponse> DeleteSapDocument(SapDocumentRequest sapDoc);
    }
}
