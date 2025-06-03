namespace SAPArchiveLink
{
    public interface IBaseServices
    {
        void PutCert(String authId, byte[] certificate, string contRepId, String permissions);
        Task<ICommandResponse> DoGetSapDocument(SapDocumentRequest sapDocumentRequest);
    }
}
