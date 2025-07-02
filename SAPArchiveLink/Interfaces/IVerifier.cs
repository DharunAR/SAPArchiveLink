using System.Security.Cryptography.X509Certificates;

namespace SAPArchiveLink
{
    public interface IVerifier
    {
        void SetCertificates(IArchiveCertificate certificates);
        void SetSignedData(byte[] signedData);
        void SetRequiredPermission(int permission);
        void VerifyAgainst(byte[] data);
        X509Certificate2 GetCertificate(int index = -1);
    }
}
