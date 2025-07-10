using System.Security.Cryptography.X509Certificates;

namespace SAPArchiveLink
{
    public interface IVerifier
    {
        void SetCertificate(IArchiveCertificate certificate);
        void SetSignedData(byte[] signedData);
        void SetRequiredPermission(int permission);
        void VerifyAgainst(byte[] data);
        X509Certificate2 GetCertificate(int index = -1);
    }
}
