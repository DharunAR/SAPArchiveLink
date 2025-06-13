using TRIM.SDK;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Data;
using System.Net;
using System.Net.Mime;

namespace SAPArchiveLink
{
    /// <summary>
    /// Implementation of the ICMArchieveLinkClient interface for handling archive link Client operations.
    /// </summary>
    public class CMArchieveLinkClient: ICMArchieveLinkClient
    {
        private readonly TrimConfigSettings _trimConfig;
        private readonly IDatabaseConnection _databaseConnection;
        private readonly ILogHelper<BaseServices> _logger;
        private readonly ICommandResponseFactory _commandResponseFactory;


        public CMArchieveLinkClient(IOptions<TrimConfigSettings> trimConfig, IDatabaseConnection databaseConnection, ILogHelper<BaseServices> helperLogger, ICommandResponseFactory commandResponseFactory)
        {
            _trimConfig = trimConfig.Value;
            _databaseConnection= databaseConnection;
            _logger = helperLogger;
            _commandResponseFactory= commandResponseFactory;
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
        public async Task<SapDocumentComponent> GetDocumentComponent(RecordSapComponents components, string compId)
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

        public async Task<List<SapDocumentComponent>> GetDocumentComponents(RecordSapComponents components)
        {
            List<SapDocumentComponent> documentComponents = new();
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
                if(sapComponent != null)
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
        private async Task<SapDocumentComponent> CreateDocumentComponent(string fileName, RecordSapComponent sapComponent)
        {
            using (Stream fileStream = File.OpenRead(fileName))
            {
                var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                return new SapDocumentComponent
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
        public Database GetDatabase()
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


        public string? GetExtensionFromContentType(string contentType)
        {
            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            return provider.Mappings
                .FirstOrDefault(kvp => kvp.Value.Equals(contentType, StringComparison.OrdinalIgnoreCase))
                .Key;
        }


        public static string GetFileExtensionFromContentType(string contentType)
        {

            var t = new ContentType(contentType);
            if (string.IsNullOrWhiteSpace(contentType))
                return ".bin"; // default fallback

            return contentType.ToLowerInvariant() switch
            {
                "application/pdf" => ".pdf",
                "application/msword" => ".doc",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                "application/vnd.ms-excel" => ".xls",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
                "application/zip" => ".zip",
                "application/json" => ".json",
                "application/xml" => ".xml",
                "text/plain" => ".txt",
                "text/html" => ".html",
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/bmp" => ".bmp",
                "image/webp" => ".webp",
                "audio/mpeg" => ".mp3",
                "video/mp4" => ".mp4",
                _ => ".bin" // fallback
            };
        }
        public async Task<string> SaveInputStreamToFile(Stream streamInput, string fileName)
        {
            if (string.IsNullOrWhiteSpace(_trimConfig.WorkPath))
            {
                _logger.LogError("WorkPath is not configured.");
                throw new InvalidOperationException("WorkPath is not configured.");
            }

            var baseDirectory = Path.Combine(_trimConfig.WorkPath, "Downloads");
            Directory.CreateDirectory(baseDirectory); // ensures folder exists  
            var safeFileName = Path.GetFileName(fileName); // avoids directory traversal  
            var fullPath = Path.Combine(baseDirectory, safeFileName);
            var normalizedPath = Path.GetFullPath(fullPath);

            if (!normalizedPath.StartsWith(baseDirectory))
            {
                _logger.LogError("Invalid file path.");
                throw new UnauthorizedAccessException("Invalid file path.");
            }

            using (var inputStream = streamInput)
            using (var fileStream = File.Create(normalizedPath))
            {
               await inputStream.CopyToAsync(fileStream);
            }
            return normalizedPath;
        }

        /// <summary>
        /// Creates a new SAP document component in the specified content repository.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sAPDocumentComponent"></param>
        /// <returns></returns>
        public async Task<ICommandResponse> ComponentCreate(Database db, CreateSapDocumentModel sAPDocumentComponent)
        {
            string alVersion = sAPDocumentComponent.PVersion;
            string contRep = sAPDocumentComponent.ContRep;
            string docId = sAPDocumentComponent.DocId;
            string compId = sAPDocumentComponent.CompId;
            string version = sAPDocumentComponent.Version;
            string contentType = sAPDocumentComponent.ContentType;
            string charSet = sAPDocumentComponent.Charset;
            string docProt = sAPDocumentComponent.DocProt;
            string compFile = null;

            var now = DateTime.Now;

            // Validate content repository
            if (string.IsNullOrWhiteSpace(contRep))
            {
                _logger.LogError("Content repository is not specified.");
                return await Task.FromResult(_commandResponseFactory.CreateError("Content repository is not specified.", StatusCodes.Status404NotFound));
            }
            // Get or create document
            var myDoc = GetRecord(db, docId,contRep);

            if (myDoc != null)
            {
                // Document exists
                if (string.IsNullOrWhiteSpace(compId))
                {
                    _logger.LogError($"No component was specified and a document with ID '{docId}' already exists.");
                    return await Task.FromResult(_commandResponseFactory.CreateError($"No component was specified and a document with ID '{docId}' already exists.", StatusCodes.Status400BadRequest));
                }

                if (FindComponent(myDoc, compId) != null)
                {
                    _logger.LogError($"A component with ID '{compId}' already exists in document '{docId}'.");
                    return await Task.FromResult(_commandResponseFactory.CreateError($"A component with ID '{compId}' already exists in document '{docId}'.", StatusCodes.Status400BadRequest));
                }
            }
            else
            {
                // Document doesn't exist, create new
                if (string.IsNullOrWhiteSpace(docId))
                {
                    docId = Guid.NewGuid().ToString("N");
                }

                var recType = GetSapRecordType(db, contRep);
                if (recType == null)
                {
                    _logger.LogError($"No valid record type found for content repository '{contRep}'.");
                    return await Task.FromResult(_commandResponseFactory.CreateError($"No valid record type found for content repository '{contRep}'.", StatusCodes.Status404NotFound));
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

                // Set document title using template

                compFile = sAPDocumentComponent.Components.FirstOrDefault()?.FileName ?? string.Empty;
                string docTitle = recType.SapTitleTemplate;
                docTitle = docTitle.Replace("%docid%", docId)
                                   .Replace("%date%", now.ToShortTimeString())
                                   .Replace("%prot%", docProt)
                                   .Replace("%alvsn%", alVersion)
                                   .Replace("%contrep%", contRep);
                myDoc.TypedTitle = docTitle;
                compFile = sAPDocumentComponent.Components.FirstOrDefault()?.FileName ?? string.Empty;
                // var path = Path.ChangeExtension(docId, GetExtensionFromContentType(contentType));
                // compFile= await SaveInputStreamToFile(sAPDocumentComponent.Stream, path); // Save the input stream to a file
            }

            // Step 3: Add component if specified
            if (!string.IsNullOrWhiteSpace(compId))
            {
                var recordSapComponents = myDoc.ChildSapComponents;
                var recordSapComponent = recordSapComponents.New();
                recordSapComponent.ComponentId = compId;
                recordSapComponent.ApplicationVersion = alVersion;
                recordSapComponent.ContentType = contentType;
                recordSapComponent.CharacterSet = charSet;
                recordSapComponent.ArchiveDate = now;
                recordSapComponent.DateModified = now;               
                recordSapComponent.SetDocument(compFile);
                myDoc.SapModifiedDate = now;
            }

            try
            {
                myDoc.Save();
            }
            catch (Exception ex)
            {

                throw;
            }
            // Step 4: Save the document
         

            // Return success response
            return await Task.FromResult(_commandResponseFactory.CreateProtocolText("Component created successfully.", StatusCodes.Status201Created));
        }
    }
}
