namespace SAPArchiveLink
{
    public interface IBaseServices
    {
        Task<ICommandResponse> PutCert(String authId, Stream inputStream, string contRepId, String permissions);
    }
}
