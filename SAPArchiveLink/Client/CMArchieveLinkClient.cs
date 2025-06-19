using TRIM.SDK;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Data;
using System.Net;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using static System.Collections.Specialized.BitVector32;

namespace SAPArchiveLink
{
    /// <summary>
    /// Implementation of the ICMArchieveLinkClient interface for handling archive link Client operations.
    /// </summary>
    public class CMArchieveLinkClient : ICMArchieveLinkClient
    {
        private readonly TrimConfigSettings _trimConfig;
        private readonly IDatabaseConnection _databaseConnection;
        private readonly ILogHelper<BaseServices> _logger;
        private readonly ICommandResponseFactory _commandResponseFactory;
        private DownloadFileHandler _downloadFileHandler;

        public CMArchieveLinkClient(IOptions<TrimConfigSettings> trimConfig,
            IDatabaseConnection databaseConnection,
            ILogHelper<BaseServices> helperLogger,
            ICommandResponseFactory commandResponseFactory, DownloadFileHandler fileHandleRequest)
        {
            _trimConfig = trimConfig.Value;
            _databaseConnection = databaseConnection;
            _logger = helperLogger;
            _commandResponseFactory = commandResponseFactory;
            _downloadFileHandler = fileHandleRequest;
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
        public async Task<SapDocumentComponentModel> GetDocumentComponent(RecordSapComponents components, string compId)
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

                    return await CreateDocumentComponent(extractDocument.FileName, sapComponent);
                }
            }
            return null;
        }


        /// <summary>
        /// Retrieves the document components for a given set of SAP components.
        /// </summary>
        /// <param name="components"></param>
        /// <returns></returns>

        public async Task<List<SapDocumentComponentModel>> GetDocumentComponents(RecordSapComponents components)
        {
            List<SapDocumentComponentModel> documentComponents = new();
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

                var sapComponent = await CreateDocumentComponent(extractDocument.FileName, c);
                if (sapComponent != null)
                {
                    documentComponents.Add(sapComponent);
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
        /// Create SAP document component from the extracted file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="sapComponent"></param>
        /// <returns></returns>
        private async Task<SapDocumentComponentModel> CreateDocumentComponent(string fileName, RecordSapComponent sapComponent)
        {
            using (Stream fileStream = File.OpenRead(fileName))
            {
                var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                return new SapDocumentComponentModel
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
                    FileName = fileName
                };
            }
        }

        /// <summary>
        /// Gets the database connection for the archive link client.
        /// </summary>
        /// <returns></returns>
        public ITrimRepository GetDatabase()
        {
            return _databaseConnection.GetDatabase();
        }

        /// <summary>
        /// Retrieves the SAP record type based on the content repository or record type name from the configuration.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="contRep"></param>
        /// <returns></returns>
        public RecordType GetSapRecordType(Database db, string contRep)
        {
            RecordType? recordType = null; // Use nullable type to handle potential null values
            if (!string.IsNullOrWhiteSpace(_trimConfig.RecordTypeName))
            {
                TrimMainObject? tmo = db.FindTrimObjectByName(BaseObjectTypes.RecordType, _trimConfig.RecordTypeName);
                if (tmo != null)
                {
                    var rty = tmo as RecordType; // Safely cast to RecordType
                    if (recordType != null && recordType.UsualBehaviour == RecordBehaviour.SapDocument) // Fix CS8602
                    {
                        recordType = rty;
                    }
                    else
                    {
                        _logger.LogError($"Record Type '{_trimConfig.RecordTypeName}' cannot be used. Only SAP Document behaviour types are allowed.");
                    }
                }
                else
                {
                    _logger.LogError($"No valid record type found with name '{_trimConfig.RecordTypeName}'.");
                }
            }
            else
            {
                TrimMainObjectSearch tmos = new TrimMainObjectSearch(db, BaseObjectTypes.RecordType);
                TrimSearchClause contRepClause = new TrimSearchClause(db, BaseObjectTypes.RecordType, SearchClauseIds.RecordTypeSaprepository);
                contRepClause.SetCriteriaFromString(contRep);
                tmos.AddSearchClause(contRepClause);
                if (tmos.Count > 0)
                {
                    var uris = tmos.GetResultAsUriArray(1);
                    recordType = new RecordType(db, uris[0]);
                }
                else
                {
                    _logger.LogError($"No valid record type found for content repository '{contRep}'.");
                }
            }

            return recordType;
        }

        public RecordSapComponent FindComponent(Record record, string compId)
        {
            RecordSapComponents recordSapComponents = record.ChildSapComponents;

            foreach (RecordSapComponent component in recordSapComponents)
            {
                if (component.GetPropertyAsString(PropertyIds.RecordSapComponentComponentId, StringDisplayType.Default, false) == compId)
                {
                    return component; ;
                }

            }
            return null;
        }

        /// <summary>
        /// Creates a new SAP document component in the specified content repository.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="SapDocumentComponentModel"></param>
        /// <returns></returns>
        public async Task<ICommandResponse> ComponentCreate(
        Database db,
        string contRep,
        string docId,
        string docProt,
        string alVersion,
        IEnumerable<SapDocumentComponentModel> components)
        {
            var now = DateTime.Now;

            if (string.IsNullOrWhiteSpace(contRep))
            {
                _logger.LogError("Content repository is not specified.");
                return _commandResponseFactory.CreateError("Content repository is not specified.", StatusCodes.Status404NotFound);
            }

            // Get or create document
            var myDoc = GetRecord(db, docId, contRep);
            if (myDoc == null)
            {
                if (string.IsNullOrWhiteSpace(docId))
                {
                    docId = Guid.NewGuid().ToString("N");
                }

                var recType = GetSapRecordType(db, contRep);
                if (recType == null)
                {
                    _logger.LogError($"No valid record type found for content repository '{contRep}'.");
                    return _commandResponseFactory.CreateError($"No valid record type found for content repository '{contRep}'.", StatusCodes.Status404NotFound);
                }

                myDoc = new Record(db, recType)
                {
                    SapReposId = contRep,
                    SapDocumentId = docId,
                    SapArchiveLinkVsn = alVersion,
                    SapDocumentProtection = docProt,
                    SapArchiveDate = now,
                    SapModifiedDate = now
                };

                string docTitle = recType.SapTitleTemplate;
                docTitle = docTitle.Replace("%docid%", docId)
                                   .Replace("%date%", now.ToShortTimeString())
                                   .Replace("%prot%", docProt)
                                   .Replace("%alvsn%", alVersion)
                                   .Replace("%contrep%", contRep);
                myDoc.TypedTitle = docTitle;
            }

            // Process each component
            foreach (var comp in components)
            {
                if (string.IsNullOrWhiteSpace(comp.CompId))
                {
                    _logger.LogError("Component ID is missing.");
                    return _commandResponseFactory.CreateError("Component ID is missing.", StatusCodes.Status400BadRequest);
                }

                if (FindComponent(myDoc, comp.CompId) != null)
                {
                    _logger.LogError($"A component with ID '{comp.CompId}' already exists in document '{docId}'.");
                    return _commandResponseFactory.CreateError($"A component with ID '{comp.CompId}' already exists in document '{docId}'.", StatusCodes.Status400BadRequest);
                }

                var filePath = await DownlaodDocument(comp.Data, comp.FileName);
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    _logger.LogError("Failed to save component file.");
                    return _commandResponseFactory.CreateError("Failed to save component file.", StatusCodes.Status400BadRequest);
                }

                var recordSapComponent = myDoc.ChildSapComponents.New();
                recordSapComponent.ComponentId = comp.CompId;
                recordSapComponent.ApplicationVersion = alVersion;
                recordSapComponent.ContentType = comp.ContentType;
                recordSapComponent.CharacterSet = comp.Charset;
                recordSapComponent.ArchiveDate = now;
                recordSapComponent.DateModified = now;
                recordSapComponent.SetDocument(filePath);
                myDoc.SapModifiedDate = now;
            }

            try
            {
                myDoc.Save();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save document '{docId}': {ex.Message}");
                return _commandResponseFactory.CreateProtocolText("Failed to save document.");
            }
            finally
            {
                // Clean up temporary files if necessary
                foreach (var comp in components)
                {
                    if (File.Exists(comp.FileName))
                    {
                        File.Delete(comp.FileName);
                    }
                }
            }

            return _commandResponseFactory.CreateProtocolText("Component(s) created successfully.", StatusCodes.Status201Created);
        }

        private async Task<string> DownlaodDocument(Stream stream, string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);               
            }          
            using var fileStream = new FileStream(filePath, FileMode.Create);
            await stream.CopyToAsync(fileStream);
            return filePath;
        }
    }
}
