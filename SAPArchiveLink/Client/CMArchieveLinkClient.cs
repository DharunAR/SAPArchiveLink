using System.Net.Mime;
using System.Runtime.InteropServices;
using System;
using TRIM.SDK;
using static System.Runtime.InteropServices.JavaScript.JSType;
using SAPArchiveLink;
using System.IO;

namespace SAPArchiveLink
{
    /// <summary>
    /// Implementation of the ICMArchieveLinkClient interface for handling archive link Client operations.
    /// </summary>
    public class CMArchieveLinkClient: ICMArchieveLinkClient
    {
        private readonly TrimConfigSettings _trimConfig;
        private readonly IDatabaseConnection _databaseConnection;
        public CMArchieveLinkClient(TrimConfigSettings trimConfig,IDatabaseConnection databaseConnection)
        {
            _trimConfig = trimConfig;
            _databaseConnection= databaseConnection;
        }

        /// <summary>
        /// Puts an archive certificate into the system.
        /// </summary>
        /// <param name="serialName"></param>
        /// <param name="fingerprint"></param>
        /// <param name="issuerCertificate"></param>
        /// <param name="validFrom"></param>
        /// <param name="validTill"></param>
        /// <param name="content"></param>
        /// <param name="permissions"></param>
        /// <param name="authId"></param>
        /// <param name="archiveDataID"></param>
        public Task PutArchiveCertificate(string authId, int protectionLevel, byte[] certificate, string contRep)
        {

            ArchiveCertificate? archiveCertificate = null;
            archiveCertificate = ArchiveCertificate.FromByteArray(certificate);
            string serialName = archiveCertificate.getSerialNumber();
            string fingerPrint = archiveCertificate.GetFingerprint();
            string issuer = archiveCertificate.getIssuerName();
            TrimDateTime validFrom = TrimDateTime.Parse(archiveCertificate.ValidFrom());
            TrimDateTime validTill = TrimDateTime.Parse(archiveCertificate.ValidTill());

            return null;
        }

        /// <summary>
        /// Retrieves a record from the database based on the document ID and content repository.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="docId"></param>
        /// <param name="contRep"></param>
        /// <returns></returns>
        public Record GetRecord(Database db, string docId, string contRep)
        {
            Record? retVal = null;
            TrimMainObjectSearch tmos = new TrimMainObjectSearch(db, BaseObjectTypes.Record);
            TrimSearchClause docClause = new TrimSearchClause(db, BaseObjectTypes.Record, SearchClauseIds.RecordSapdoc);
            docClause.SetCriteriaFromString(docId);
            tmos.AddSearchClause(docClause);
            TrimSearchClause contRepClause = new TrimSearchClause(db, BaseObjectTypes.Record, SearchClauseIds.RecordSaprepos);
            contRepClause.SetCriteriaFromString(contRep);
            tmos.AddSearchClause(contRepClause);
            if (tmos.Count > 0)
            {
                var uris = tmos.GetResultAsUriArray(1);
                retVal = new Record(db, uris[0]);
            }
            return retVal;
        }

        /// <summary>
        /// Retreive the document component based on the ComponentId
        /// </summary>
        /// <param name="components"></param>
        /// <param name="compId"></param>
        /// <returns></returns>
        public async Task<SAPDocumentComponent> GetDocumentComponent(RecordSapComponents components, string compId)
        {
            string? fileName;
            RecordSapComponent? sapComponent = null;
            if (!string.IsNullOrWhiteSpace(compId))
            {
                var tt = components.GetEnumerator() as List<RecordSapComponent>;
                foreach (RecordSapComponent c in components)
                {
                    if (c.ComponentId == compId)
                    {
                        sapComponent = c;
                        break;
                    }
                }
                if (sapComponent != null)
                {
                    ExtractDocument extractDocument = sapComponent.GetExtractDocument();
                    fileName = _trimConfig.WorkPath;
                    extractDocument.DoExtract(fileName, true, false, string.Empty);

                    using (Stream fileStream = File.OpenRead(extractDocument.FileName))
                    {
                        var memoryStream = new MemoryStream();
                        await fileStream.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;
                        return new SAPDocumentComponent
                        {
                            CompId = sapComponent.ComponentId,
                            ContentType = sapComponent.ContentType ?? "application/octet-stream",
                            Charset = sapComponent.CharacterSet ?? "UTF-8",
                            Version = sapComponent.ApplicationVersion,
                            ContentLength = sapComponent.Bytes,
                            CreationDate = sapComponent.ArchiveDate,
                            ModifiedDate = sapComponent.DateModified,
                            Status = "online",
                            PVersion = sapComponent.ArchiveLinkVersion,
                            Data = memoryStream,
                            FileName = extractDocument.FileName
                        };
                    }
                }
            }
            return null;
        }


        /// <summary>
        /// Retrieves the document components for a given set of SAP components.
        /// </summary>
        /// <param name="components"></param>
        /// <returns></returns>

        public async Task<List<SAPDocumentComponent>> GetDocumentComponents(RecordSapComponents components)
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
                extractDocument.DoExtract(fileName, true, false, string.Empty);

                using (Stream fileStream = File.OpenRead(extractDocument.FileName))
                {
                    var memoryStream = new MemoryStream();
                    await fileStream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
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
            }
            return documentComponents;
        }

        /// <summary>
        /// Checks if a specific component is available in the record's SAP components.
        /// </summary>
        /// <param name="components"></param>
        /// <param name="compId"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets the database connection for the archive link client.
        /// </summary>
        /// <returns></returns>
        public Database GetDatabase()
        {
            return _databaseConnection.GetDatabase();
        }
    }
}
