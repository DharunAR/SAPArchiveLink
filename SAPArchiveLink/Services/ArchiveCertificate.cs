using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SAPArchiveLink
{
    public class ArchiveCertificate : IArchiveCertificate
    {
        private readonly X509Certificate2 _certificate;
        private readonly int _permission;
        private readonly bool _isEnabled;
        private readonly string _authId;

        public static ArchiveCertificate FromByteArray(byte[] data)
        {
            ArchiveCertificate? certificate = null;
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
                    string certificateContent = Encoding.UTF8.GetString(data);
                    if (certificateContent.Contains("-----BEGIN"))
                    {
                        // PEM format
                        certificate = LoadFromPem(certificateContent);
                    }
                    else
                    {
                        // Assume base64 encoded binary (DER or PFX)
                        var cert = new X509Certificate2(data);
                        certificate = new ArchiveCertificate(cert.RawData);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Failed to create ArchiveCertificate from byte array.", ex);
            }

            return certificate ?? throw new InvalidOperationException("Failed to create a valid ArchiveCertificate.");
        }

        public ArchiveCertificate(X509Certificate2 certificate,string authId, int permission = 0, bool isEnabled = false)
        {
            _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
            _isEnabled = isEnabled;
            _permission = permission;
            _authId = authId;
        }

        public string GetFingerprint()
        {
            return _certificate.Thumbprint;
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
        public string GetAuthId()
        {
            return _authId;
        }

        public bool IsEnabled()
        {
            return _isEnabled;
        }

        public X509Certificate2 GetCertificate()
        {
            return _certificate;
        }

        private ArchiveCertificate(byte[] certData)
        {
            if (certData == null || certData.Length == 0)
            {
                throw new ArgumentNullException(nameof(certData), "Certificate data cannot be null or empty.");
            }

            _certificate = new X509Certificate2(certData);         
        }      

        private static ArchiveCertificate LoadFromPem(string pemString)
        {
            string? certPem = null;
            string? keyPem = null;

            // Extract CERTIFICATE and PRIVATE KEY sections  
            var lines = pemString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sectionBuilder = new();
            string? currentSection = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("-----BEGIN "))
                {
                    currentSection = line;
                    sectionBuilder.Clear();
                }
                else if (line.StartsWith("-----END "))
                {
                    var sectionContent = sectionBuilder.ToString();
                    if (currentSection != null && currentSection.Contains("CERTIFICATE") && !currentSection.Contains("PRIVATE KEY"))
                    {
                        certPem = $"{currentSection}\n{sectionContent}{line}";
                    }
                    else if (currentSection != null && currentSection.Contains("PRIVATE KEY"))
                    {
                        keyPem = $"{currentSection}\n{sectionContent}{line}";
                    }
                    currentSection = null;
                }
                else if (currentSection != null)
                {
                    sectionBuilder.AppendLine(line);
                }
            }

            if (certPem == null)
                throw new InvalidOperationException("No CERTIFICATE section found in PEM.");

            // Load certificate  
            var certBase64 = certPem
                .Replace("-----BEGIN CERTIFICATE-----", "")
                .Replace("-----END CERTIFICATE-----", "")
                .Replace("\n", "")
                .Replace("\r", "");
            var certBytes = Convert.FromBase64String(certBase64);
            var cert = new ArchiveCertificate(certBytes);

            if (keyPem == null)
                return cert; // public cert only  

            // Load private key  
            var keyBase64 = keyPem
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
                .Replace("-----END RSA PRIVATE KEY-----", "")
                .Replace("\n", "")
                .Replace("\r", "");
            var keyBytes = Convert.FromBase64String(keyBase64);

            using RSA rsa = RSA.Create();
            try
            {
                rsa.ImportPkcs8PrivateKey(keyBytes, out _);
            }
            catch (CryptographicException)
            {
                rsa.ImportRSAPrivateKey(keyBytes, out _);
            }

            var certWithKey = cert.GetCertificate().CopyWithPrivateKey(rsa);
            return new ArchiveCertificate(certWithKey.Export(X509ContentType.Pfx));
        }          
      
    }

}
