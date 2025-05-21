using System.Security.Cryptography.X509Certificates;

namespace SAPArchiveLink
{
    public interface IArchiveCertificate
    {
        byte[] GetFingerprint();
        int GetPermission();
        bool IsUsedInElibContext();
        X509Certificate2 GetCertificate();
    }

}
