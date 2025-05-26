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
            _permission = permission;
            _isUsedInElibContext = isUsedInElibContext;
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
            _permission = 0; // Default value
            _isUsedInElibContext = false; // Default value
        }

        public static ArchiveCertificate FromByteArray(byte[] data)
        {
            //if (LogHelper.EnterOn())
            //    LogHelper.Enter(MN);

            ArchiveCertificate certificate = null;
            MemoryStream ms = null;

            try
            {
                //if (LogHelper.DevOn())
                //{
                //    // Dump the certificate into the log directory
                //    string logFile = ConfigStoreManager.ConfigStore.RootSection.GetProperty("IXOS_SRV_LOG", ".");
                //    if (!logFile.EndsWith(Path.DirectorySeparatorChar.ToString()))
                //        logFile += Path.DirectorySeparatorChar;
                //    logFile += $"cert_{data.Length}.cer";

                //    //LogHelper.DevDbg(MN, $"Dumping certificate to {logFile}");

                //    try
                //    {
                //        File.WriteAllBytes(logFile, data);
                //    }
                //    catch (IOException ex)
                //    {
                //        //LogHelper.Warn(MN, $"Exception occurred while dumping certificate to {logFile}", ex);
                //    }
                //}

                // 1. Try PKCS7 format
               // LogHelper.Debug(MN, "Parsing certificate...");
                try
                {
                  //  LogHelper.Debug(MN, "Try PKCS7...");
                    ms = new MemoryStream(data);

                    // Using PKCS7 SignedCms (System.Security.Cryptography.Pkcs in .NET)
                    var signedCms = new System.Security.Cryptography.Pkcs.SignedCms();
                    signedCms.Decode(data);

                    if (signedCms.Certificates.Count > 0)
                    {
                        var x509Cert = signedCms.Certificates[0];
                        certificate = new ArchiveCertificate(x509Cert.RawData);
                    }
                }
                catch (Exception pkcs7Ex)
                {
                 //  LogHelper.Debug(MN, "Certificate is not in PKCS7 format", pkcs7Ex);

                    if (ms != null)
                        ms.Dispose();

                    // 2. Try X.509 format
                 //   LogHelper.Debug(MN, "Try X.509...");
                    ms = new MemoryStream(data);
                    var cert = new X509Certificate2(ms.ToArray());
                    certificate = new ArchiveCertificate(cert.RawData);
                }
            }
            catch (Exception ex)
            {
            //    throw new ICSException(
            //        ICSException.ERROR_WRAPPER,
            //        ICSException.ERROR_WRAPPER_STR,
            //        new object[] { ex.GetType().Name, ex.Message
            //        },
            //        ex
               // );
            }
            finally
            {
                // Ensure stream is closed
                if (ms != null)
                {
                    try
                    {
                        ms.Dispose();
                    }
                    catch (IOException ex)
                    {
                       // LogHelper.Warn(MN, "Error occurred while closing stream", ex);
                    }
                }
            }

            //if (LogHelper.LeaveOn())
            //    LogHelper.Leave(MN);

            return certificate;
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
