using System.Runtime.InteropServices;
using TRIM.SDK;

namespace SAPArchiveLink
{
    public interface ICMArchieveLinkClient
    {      
        Record GetRecord(Database db, string docId, string contRep);

        Task<List<SAPDocumentComponent>> GetDocumentComponents(RecordSapComponents components);

        Task<SAPDocumentComponent> GetDocumentComponent(RecordSapComponents components, string compId);

        bool IsRecordComponentAvailable(RecordSapComponents components, string compId);

        Database GetDatabase();

        Task PutArchiveCertificate(string authId, int protectionLevel, byte[] certificate, string contRep);
    }
}
