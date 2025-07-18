namespace SAPArchiveLink
{
    public interface ITrimRepository : IDisposable
    {
        IArchiveRecord? GetRecord(string docId, string contRep);
        IArchiveRecord? CreateRecord(CreateSapDocumentModel model);
        void SaveCertificate(string authId, int protectionLevel, IArchiveCertificate certificate, string contRep);
        IArchiveCertificate GetArchiveCertificate(string contentRepo);
        ServerInfoModel GetServerInfo(string pVersion, string contRep);
        void SaveCounters(string archiveId, ArchiveCounter counter);
        bool IsSAPLicenseEnabled();
    }

}
