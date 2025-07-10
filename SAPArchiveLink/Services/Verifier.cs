
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;

namespace SAPArchiveLink
{
    public class Verifier : IVerifier
    {
        private IArchiveCertificate _certificate;
        private X509Certificate2Collection _rawCertificates; // Change type to X509Certificate2Collection
        private byte[] _signedData;
        private int _requiredPermission = -1;
        private X509Certificate2 _verifiedCertificate;

        public void SetCertificate(IArchiveCertificate certificate)
        {
            _certificate = certificate;
            _rawCertificates = new X509Certificate2Collection(); // Initialize as a collection
            if (certificate?.GetCertificate() != null)
            {
                _rawCertificates.Add(certificate.GetCertificate()); // Add the certificate to the collection
            }
        }

        public void SetRawCertificates(X509Certificate2 certificate)
        {
            _rawCertificates = new X509Certificate2Collection(); // Initialize as a collection
            if (certificate != null)
            {
                _rawCertificates.Add(certificate); // Add the certificate to the collection
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

                if (signedCms.SignerInfos.Count == 0)
                    throw new Exception("No SignerInfo found");

                var signerInfo = signedCms.SignerInfos[0];

                // Try to match the certificate manually from trusted collection

                foreach (var cert in _rawCertificates)
                {
                    if (signerInfo.Certificate != null && cert.Thumbprint == signerInfo.Certificate.Thumbprint)
                    {
                        _verifiedCertificate = cert;
                        break;
                    }
                    // Try to match issuer + serial
                    var si = signerInfo.SignerIdentifier as SubjectIdentifier;
                    if (si?.Type == SubjectIdentifierType.IssuerAndSerialNumber)
                    {
                        var issuerSerial = (X509IssuerSerial)si.Value;
                        if (cert.Issuer == issuerSerial.IssuerName && cert.SerialNumber.Equals(issuerSerial.SerialNumber, StringComparison.OrdinalIgnoreCase))
                        {
                            _verifiedCertificate = cert;
                            break;
                        }
                    }
                }

                if (_verifiedCertificate == null)
                    throw new CryptographicException("Matching certificate not found in trusted set.");


                if (_certificate != null && _requiredPermission >= 0)
                {
                    int granted = _certificate.GetPermission();
                    if ((granted & _requiredPermission) == 0)
                        throw new Exception("Permission denied: insufficient certificate rights.");
                }

                _verifiedCertificate.Verify();
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

                    if (_certificate != null && cert.Thumbprint != _certificate.GetCertificate().Thumbprint)
                        throw new Exception("Signer certificate does not match expected certificate.");

                    if (_certificate != null && _requiredPermission >= 0)
                    {
                        int granted = _certificate.GetPermission();
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
