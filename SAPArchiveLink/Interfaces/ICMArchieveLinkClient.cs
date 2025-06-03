using TRIM.SDK;

namespace SAPArchiveLink
{
    public interface ICMArchieveLinkClient
    {
        void PutArchiveCertificate();
        Record GetRecord(Database db, string docId, string contRep);
        List<SAPDocumentComponent> GetDocumentComponents(RecordSapComponents components);
        bool IsRecordComponentAvailable(RecordSapComponents components, string compId);
        Database GetDatabase();
    }
}
