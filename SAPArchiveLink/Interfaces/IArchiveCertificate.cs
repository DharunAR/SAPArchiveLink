using System.Security.Cryptography.X509Certificates;

namespace SAPArchiveLink
{
    public interface IArchiveCertificate
    {
        string GetFingerprint();

        int GetPermission();

        bool IsUsedInElibContext();

        X509Certificate2 GetCertificate();

        string getSerialNumber();

        string getIssuerName();

        string ValidTill();

        string ValidFrom();

    }

}
