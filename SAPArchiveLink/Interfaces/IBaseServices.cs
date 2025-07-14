namespace SAPArchiveLink
{
    public interface IBaseServices
    {
        Task<ICommandResponse> PutCert(PutCertificateModel model);
        Task<ICommandResponse> CreateRecord(CreateSapDocumentModel createSapDocumentModels, bool isMultipart = false);
        Task<ICommandResponse> DocGetSapComponents(SapDocumentRequest sapDocumentRequest);
        Task<ICommandResponse> GetSapDocument(SapDocumentRequest sapDoc);
        Task<ICommandResponse> UpdateRecord(CreateSapDocumentModel createSapDocumentModels, bool isMultipart = false);
        Task<ICommandResponse> DeleteSapDocument(SapDocumentRequest sapDoc);
        Task<ICommandResponse> GetDocumentInfo(SapDocumentRequest sapDocumentRequest);
        Task<ICommandResponse> GetSearchResult(SapSearchRequestModel sapSearchRequest);
        Task<ICommandResponse> GetServerInfo(string contRep, string pVersion, string resultAs);
        Task<ICommandResponse> AppendDocument(AppendSapDocCompModel sapDoc);
    }
}
