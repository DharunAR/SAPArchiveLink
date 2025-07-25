using SAPArchiveLink.Resources;
using System.Security;
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
            _rawCertificates = new X509Certificate2Collection();
            if (certificate?.GetCertificate() != null)
            {
                _rawCertificates.Add(certificate.GetCertificate());
            }
        }

        public void SetRawCertificates(X509Certificate2 certificate)
        {
            _rawCertificates = new X509Certificate2Collection();
            if (certificate != null)
            {
                _rawCertificates.Add(certificate);
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
                throw new InvalidOperationException(Resource.SignedDataMustValidate);

            try
            {
                var signedCms = new SignedCms(new ContentInfo(data), detached: true);
                signedCms.Decode(_signedData);

                if (signedCms.SignerInfos.Count == 0)
                    throw new InvalidOperationException(Resource.NoSignerFound);

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
                    var si = signerInfo.SignerIdentifier;
                    if (si?.Type == SubjectIdentifierType.IssuerAndSerialNumber && si.Value is X509IssuerSerial issuerSerial)
                    {
                        bool issuerMatches = cert.Issuer == issuerSerial.IssuerName;
                        bool serialMatches = cert.SerialNumber.Equals(issuerSerial.SerialNumber, StringComparison.OrdinalIgnoreCase);

                        if (issuerMatches && serialMatches)
                        {
                            _verifiedCertificate = cert;
                            break;
                        }
                    }
                }

                if (_verifiedCertificate == null)
                    throw new CryptographicException(Resource.MachingCertNotFound);


                if (_certificate != null && _requiredPermission >= 0)
                {
                    int granted = _certificate.GetPermission();
                    if ((granted & _requiredPermission) == 0)
                        throw new UnauthorizedAccessException(Resource.PermissionDenied);
                }

                _verifiedCertificate.Verify();
            }
            catch (Exception)
            {
                try
                {
                    var cert = new X509Certificate2(_signedData);
                    if (!cert.Verify())
                        throw new CryptographicException(Resource.CertVerificationFailed);
                    _verifiedCertificate = cert;

                    if (cert == null)
                        throw new InvalidOperationException(Resource.NoSignerCertFound);

                    if (_certificate != null && cert.Thumbprint != _certificate.GetCertificate().Thumbprint)
                        throw new SecurityException(Resource.SignerCertNotMatch);

                    if (_certificate != null && _requiredPermission >= 0)
                    {
                        int granted = _certificate.GetPermission();
                        if ((granted & _requiredPermission) == 0)
                            throw new UnauthorizedAccessException(Resource.PermissionDenied);
                    }
                }
                catch (Exception fallbackEx)
                {
                    throw new CryptographicException(Resource.CertVerFailed, fallbackEx);
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
