﻿using System.Runtime.InteropServices;
using TRIM.SDK;

namespace SAPArchiveLink
{
    public interface ICMArchieveLinkClient
    {
        Record GetRecord(Database db, string docId, string contRep);

        Task<List<SapDocumentComponentModel>> GetDocumentComponents(RecordSapComponents components);

        Task<SapDocumentComponentModel> GetDocumentComponent(RecordSapComponents components, string compId);

        bool IsRecordComponentAvailable(RecordSapComponents components, string compId);

        ITrimRepository GetDatabase();

        Task PutArchiveCertificate(string authId, int protectionLevel, byte[] certificate, string contRep);
      
        Task<ICommandResponse> ComponentCreate(
                                                Database db,
                                                string contRep,
                                                string docId,
                                                string docProt,
                                                string alVersion,
                                                IEnumerable<SapDocumentComponentModel> components);
    }
}
