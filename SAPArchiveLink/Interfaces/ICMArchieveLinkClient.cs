using System.Runtime.InteropServices;
using TRIM.SDK;

namespace SAPArchiveLink
{
    public interface ICMArchieveLinkClient
    {
        void PutArchiveCertificate(String serialName, String fingerprint, String issuerCertificate, String validFrom, String validTill, String content, int permissions, String authId, long archiveDataID);
        Record GetRecord(Database db, string docId, string contRep);
        Task<List<SAPDocumentComponent>> GetDocumentComponents(RecordSapComponents components);
        Task<SAPDocumentComponent> GetDocumentComponent(RecordSapComponents components, string compId);
        bool IsRecordComponentAvailable(RecordSapComponents components, string compId);
        Database GetDatabase();
    }
}
