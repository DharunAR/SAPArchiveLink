namespace SAPArchiveLink
{
    public class CertificateFactory : ICertificateFactory
    {
        public IArchiveCertificate FromByteArray(byte[] data)
        {
            return ArchiveCertificate.FromByteArray(data);
        }
    }
}
