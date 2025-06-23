namespace SAPArchiveLink
{
    public interface ICertificateFactory
    {
        IArchiveCertificate FromByteArray(byte[] data);
    }
}
