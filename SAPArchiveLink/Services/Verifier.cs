
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace SAPArchiveLink
{
    public class Verifier : IVerifier
    {
        private IArchiveCertificate _certificates;
        private X509Certificate2Collection _rawCertificates; // Change type to X509Certificate2Collection
        private byte[] _signedData;
        private int _requiredPermission = -1;
        private X509Certificate2 _verifiedCertificate;

        public void SetCertificates(IArchiveCertificate certificates)
        {
            _certificates = certificates;
            _rawCertificates = new X509Certificate2Collection(); // Initialize as a collection
            if (certificates?.GetCertificate() != null)
            {
                _rawCertificates.Add(certificates.GetCertificate()); // Add the certificate to the collection
            }
        }

        public void SetRawCertificates(X509Certificate2 certificates)
        {
            _rawCertificates = new X509Certificate2Collection(); // Initialize as a collection
            if (certificates != null)
            {
                _rawCertificates.Add(certificates); // Add the certificate to the collection
            }
        }

        public void SetSignedData(byte[] signedData)
        {
            _signedData = signedData ?? throw new ArgumentNullException(nameof(signedData));
        }

        public void SetRequiredPermission(int permission)
        {
            _requiredPermission = permission;
        }

        public void VerifyAgainst(byte[] data)
        {
            if (_signedData == null)
                throw new InvalidOperationException("Signed data must be set before verification.");

            var signedCms = new SignedCms(new ContentInfo(data), detached: true);
            signedCms.Decode(_signedData);

            // Optionally validate using only specific trusted certs
            var extraStore = new X509Certificate2Collection();
            if (_rawCertificates != null)
                extraStore.AddRange(_rawCertificates); // Fix: AddRange works with X509Certificate2Collection

            try
            {
                signedCms.CheckSignature(extraStore, true);
            }
            catch (CryptographicException ex)
            {
                throw new Exception(); // throw new ICSSecurityException("Signature verification failed.", ex);
            }

            // Find verified signer cert
            var signerCert = signedCms.SignerInfos[0].Certificate;
            if (signerCert == null)
                throw new Exception(); // throw new ICSSecurityException("No valid certificate found in signed data.");

            // Permission check if needed
            if (_requiredPermission >= 0 && _certificates != null)
            {
                var matching = _certificates.GetCertificate().Thumbprint == signerCert.Thumbprint && _certificates.GetPermission() >= _requiredPermission;

                if (matching == null)
                    throw new Exception();
                // throw new ICSSecurityException("Certificate found but permission insufficient.");
            }

            _verifiedCertificate = signerCert;
        }

        public X509Certificate2 GetCertificate(int index = -1)
        {
            if (index < 0)
                return _verifiedCertificate;

            if (_rawCertificates == null || index >= _rawCertificates.Count)
                return null;

            return _rawCertificates[index];
        }
    }

}
