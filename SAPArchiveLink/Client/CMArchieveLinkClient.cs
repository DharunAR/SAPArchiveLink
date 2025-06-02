using SAPArchiveLink.Client;
using TRIM.SDK;

namespace SAPArchiveLink
{
    public class CMArchieveLinkClient
    {
        private readonly TrimConfigSettings _trimConfig;
        public CMArchieveLinkClient(TrimConfigSettings trimConfig)
        {
            _trimConfig = trimConfig;
        }

        public void PutArchiveCertificate()
        {
            using (var db = GetDatabase())
            {

            }
        }
        public Record GetRecord(string docId, string contRep)
        {
            using (var db = GetDatabase())
            {
                Record? retVal = null;
                TrimMainObjectSearch tmos = new TrimMainObjectSearch(db, BaseObjectTypes.Record);
                TrimSearchClause docClause = new TrimSearchClause(db, BaseObjectTypes.Record, SearchClauseIds.RecordSapdoc);
                docClause.SetCriteriaFromString(docId);
                TrimSearchClause contRepClause = new TrimSearchClause(db, BaseObjectTypes.Record, SearchClauseIds.RecordSaprepos);
                contRepClause.SetCriteriaFromString(contRep);
                if (tmos.Count > 0)
                {
                    retVal = tmos.GetEnumerator()?.Current as Record;
                }
                return retVal;
            }   
        }

        public List<SAPDocumentComponent> GetDocumentComponents(RecordSapComponents components)
        {
            List<SAPDocumentComponent> documentComponents = new();
            foreach (RecordSapComponent c in components)
            {
                string? fileName;
                ExtractDocument extractDocument = c.GetExtractDocument();
                if (File.Exists(extractDocument.FileName))
                {
                    fileName = extractDocument.FileName;
                }
                else
                {
                    fileName = _trimConfig.WorkPath;
                }
                extractDocument.DoExtract(fileName, false, false, string.Empty);

                Stream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                documentComponents.Add(new SAPDocumentComponent
                {
                    CompId = c.ComponentId,
                    ContentType = c.ContentType ?? "application/octet-stream",
                    Charset = c.CharacterSet ?? "UTF-8",
                    Version = c.ApplicationVersion,
                    ContentLength = c.Bytes,
                    CreationDate = c.ArchiveDate,
                    ModifiedDate = c.DateModified,
                    Status = "online",
                    PVersion = c.ArchiveLinkVersion,
                    Data = fileStream
                });
            }
            return documentComponents;
        }

        public bool IsRecordComponentAvailable(RecordSapComponents components, string compId)
        {
            foreach (RecordSapComponent component in components)
            {
                if (component.ComponentId == compId)
                {
                    return true;
                }
            }
            return false;
        }

        private Database GetDatabase()
        {
            return DatabaseConnection.GetDatabase(_trimConfig);
        }
    }

}
