using Microsoft.AspNetCore.Razor.TagHelpers;
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
          //  _permission = permission;
            //_isUsedInElibContext = isUsedInElibContext;
        }
        // Fix for CS1729: 'object' does not contain a constructor that takes 1 arguments
        // The issue is that the `base(certData)` call is invalid because the base class of `ArchiveCertificate` is `object`, 
        // which does not have a constructor that accepts arguments. Since `ArchiveCertificate` does not inherit from any other class, 
        // the `base(certData)` call should be removed.

        private ArchiveCertificate(byte[] certData)
        {
            if (certData == null || certData.Length == 0)
            {
                throw new ArgumentNullException(nameof(certData), "Certificate data cannot be null or empty.");
            }

            _certificate = new X509Certificate2(certData);         
        }

        public static ArchiveCertificate FromByteArray(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentNullException(nameof(data), "Input certificate cannot be null or empty.");
            }

            ArchiveCertificate? certificate = null;
            MemoryStream? ms = null;

            try
            {
                try
                {
                    // Try to parse as PKCS7
                    var signedCms = new System.Security.Cryptography.Pkcs.SignedCms();
                    signedCms.Decode(data);

                    if (signedCms.Certificates.Count > 0)
                    {
                        var x509Cert = signedCms.Certificates[0];
                        certificate = new ArchiveCertificate(x509Cert.RawData);
                    }
                }
                catch (Exception)
                {
                    // Fallback to X.509 if not PKCS7
                    ms?.Dispose(); // dispose any prior stream if needed

                    ms = new MemoryStream(data);
                    var cert = new X509Certificate2(ms.ToArray());
                    certificate = new ArchiveCertificate(cert.RawData);
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Failed to create ArchiveCertificate from byte array.", ex);
            }
            finally
            {
                if (ms != null)
                {
                    try
                    {
                        ms.Dispose();
                    }
                    catch (IOException)
                    {
                        // Log the exception if needed
                    }
                }
            }

            return certificate ?? throw new InvalidOperationException("Failed to create a valid ArchiveCertificate.");
        }

        public string GetFingerprint()
        {
            return _certificate.Thumbprint;
            //using (var sha1 = SHA1.Create())
            //{
            //    return sha1.ComputeHash(_certificate.RawData);
            //}
        }

        public string getSerialNumber()
        {
            return _certificate.SerialNumber;
        }

        public string getIssuerName()
        {
            return _certificate.Issuer;
        }
        public string ValidTill()
        {
            return _certificate.NotAfter.ToString();
        }
        public string ValidFrom()
        {
            return _certificate.NotBefore.ToString();
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
