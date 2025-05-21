using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SAPArchiveLink
{
    public class ArchiveCertificate : IArchiveCertificate
    {
        private readonly X509Certificate2 _certificate;
        private readonly int _permission;
        private readonly bool _isUsedInElibContext;

        public ArchiveCertificate(X509Certificate2 certificate, int permission = 0, bool isUsedInElibContext = false)
        {
            _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
            _permission = permission;
            _isUsedInElibContext = isUsedInElibContext;
        }

        public byte[] GetFingerprint()
        {
            using (var sha1 = SHA1.Create())
            {
                return sha1.ComputeHash(_certificate.RawData);
            }
        }

        public int GetPermission()
        {
            return _permission;
        }

        public bool IsUsedInElibContext()
        {
            return _isUsedInElibContext;
        }

        public X509Certificate2 GetCertificate()
        {
            return _certificate;
        }
    }

}
