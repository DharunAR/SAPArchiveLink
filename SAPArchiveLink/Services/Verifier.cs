
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
            if (_signedData == null || _signedData.Length == 0)
                throw new InvalidOperationException("Signed data must be set before verification.");


            try
            {
                var signedCms = new SignedCms(new ContentInfo(data), detached: true);
                signedCms.Decode(_signedData);
              
                var extraStore = new X509Certificate2Collection();
                if (_rawCertificates != null)
                    extraStore.AddRange(_rawCertificates); 
                signedCms.CheckSignature(extraStore, true);
                var signerCert = signedCms.SignerInfos[0].Certificate;
                if (signerCert == null)
                    throw new Exception("No certificate found in signer info.");

                if (_certificates != null && signerCert.Thumbprint != _certificates.GetCertificate().Thumbprint)
                    throw new Exception("Signer certificate does not match expected certificate.");

                if (_certificates != null && _requiredPermission >= 0)
                {
                    int granted = _certificates.GetPermission();
                    if ((granted & _requiredPermission) == 0)
                        throw new Exception("Permission denied: insufficient certificate rights.");
                }

                _verifiedCertificate = signerCert;
            }
            catch (Exception)
            {
                try
                {
                    var cert = new X509Certificate2(_signedData);
                    cert.Verify();
                    _verifiedCertificate = cert;

                    if (cert == null)
                        throw new Exception("No certificate found in signer info.");

                    if (_certificates != null && cert.Thumbprint != _certificates.GetCertificate().Thumbprint)
                        throw new Exception("Signer certificate does not match expected certificate.");

                    if (_certificates != null && _requiredPermission >= 0)
                    {
                        int granted = _certificates.GetPermission();
                        if ((granted & _requiredPermission) == 0)
                            throw new Exception("Permission denied: insufficient certificate rights.");
                    }
                }
                catch (Exception fallbackEx)
                {
                    throw new Exception("Both PKCS#7 and X.509 verification failed.", fallbackEx);
                }
            }
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
