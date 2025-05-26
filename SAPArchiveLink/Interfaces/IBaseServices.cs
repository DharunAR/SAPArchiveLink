namespace SAPArchiveLink
{
    public interface IBaseServices
    {
        void PutCert(String authId, byte[] certificate, string contRepId, String permissions);
    }
}
