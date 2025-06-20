using TRIM.SDK;

namespace SAPArchiveLink
{
    public class ArchiveRecord : IArchiveRecord
    {
        private Record _record;
        private readonly TrimConfigSettings _trimConfig;
        private RecordSapComponentsAdapter? _componentsAdapter;
        private readonly ILogHelper<ArchiveRecord> _log;

        public ArchiveRecord(Record record, TrimConfigSettings trimConfig, ILogHelper<ArchiveRecord> log)
        {
            _record = record ?? throw new ArgumentNullException(nameof(record));
            _trimConfig = trimConfig ?? throw new ArgumentNullException(nameof(trimConfig));
            _log = log;
        }

        public DateTime DateCreated => _record.DateCreated.ToDateTime();
        public DateTime DateModified => _record.DateModified.ToDateTime();
        public int ComponentCount => (int)_record.ChildSapComponents.Count;

        private RecordSapComponentsAdapter ComponentsAdapter => _componentsAdapter ??= new RecordSapComponentsAdapter(_record.ChildSapComponents);

        public List<SapDocumentComponentModel> GetAllComponents()
        {
            return ComponentsAdapter.GetAllComponents();
        }

        public SapDocumentComponentModel? GetComponentById(string compId)
        {
            return ComponentsAdapter.GetComponentById(compId);
        }

        public IRecordSapComponent? FindComponentById(string compId)
        {
            return ComponentsAdapter.FindComponentById(compId);
        }

        public bool HasComponent(string compId)
        {
            return FindComponentById(compId) != null;
        }

        public async Task<List<SapDocumentComponentModel>> ExtractAllComponents()
        {
            _log.LogInformation($"Extracting all components from record {_record.SapDocumentId}");
            var result = new List<SapDocumentComponentModel>();

            foreach (RecordSapComponent sdkComponent in _record.ChildSapComponents)
            {
                var sapComponent = await ExtractToSapComponent(sdkComponent);
                if (sapComponent != null)
                    result.Add(sapComponent);
            }

            return result;
        }

        public async Task<SapDocumentComponentModel?> ExtractComponentById(string compId)
        {
            _log.LogInformation($"Extracting component with ID: {compId} from record {_record.SapDocumentId}");
            foreach (RecordSapComponent sdkComponent in _record.ChildSapComponents)
            {
                if (sdkComponent.ComponentId == compId)
                {
                    return await ExtractToSapComponent(sdkComponent);
                }
            }

            return null;
        }

        public static ArchiveRecord? CreateNewArchiveRecord(Database db, TrimConfigSettings trimConfig, ILogHelper<ArchiveRecord> logger, CreateSapDocumentModel model)
        {
            var recType = GetRecordType(db, trimConfig, model.ContRep, logger);
            if (recType == null)
            {
                logger.LogError(TrimApplication.GetMessage(MessageIds.sap_NoValidRecTypeFound, model.ContRep));
                return null;
            }

            var now = DateTime.UtcNow;

            var record = new Record(db, recType)
            {
                SapReposId = model.ContRep,
                SapDocumentId = model.DocId,
                SapArchiveLinkVsn = model.PVersion,
                SapDocumentProtection = model.DocProt,
                SapArchiveDate = now,
                SapModifiedDate = now,
                TypedTitle = GenerateTitle(recType, model, now)
            };

            return new ArchiveRecord(record, trimConfig, logger);
        }

        public static RecordType? GetRecordType(Database db, TrimConfigSettings config, string contRep, ILogHelper<ArchiveRecord> logger)
        {
            if (!string.IsNullOrWhiteSpace(config.RecordTypeName))
            {
                logger.LogInformation($"Get RecordType by name: {config.RecordTypeName}");
                var tmo = db.FindTrimObjectByName(BaseObjectTypes.RecordType, config.RecordTypeName);
                if (tmo is RecordType rt)
                {
                    if (rt.UsualBehaviour == RecordBehaviour.SapDocument)
                    {
                        return rt;
                    }
                    return null;
                }
                return null;
            }

            logger.LogInformation($"Get RecordType by SapRepository: {contRep}");
            var tmos = new TrimMainObjectSearch(db, BaseObjectTypes.RecordType);
            var clause = new TrimSearchClause(db, BaseObjectTypes.RecordType, SearchClauseIds.RecordTypeSaprepository);
            clause.SetCriteriaFromString(contRep);
            tmos.AddSearchClause(clause);

            if (tmos.Count > 0)
            {
                var uris = tmos.GetResultAsUriArray(1);
                return new RecordType(db, uris[0]);
            }
            return null;
        }

        private static string GenerateTitle(RecordType rt, CreateSapDocumentModel model, DateTime now)
        {
            return rt.SapTitleTemplate
                .Replace("%docid%", model.DocId)
                .Replace("%date%", now.ToShortTimeString())
                .Replace("%prot%", model.DocProt)
                .Replace("%alvsn%", model.PVersion)
                .Replace("%contrep%", model.ContRep);
        }

        private async Task<SapDocumentComponentModel> ExtractToSapComponent(RecordSapComponent sdkComponent)
        {
            var extractDocument = sdkComponent.GetExtractDocument();
            if(extractDocument != null)
            {
                string uploadsPath = $"{_trimConfig.WorkPath}\\Uploads";
                Directory.CreateDirectory(uploadsPath);
                var fileName = File.Exists(extractDocument.FileName)
                    ? extractDocument.FileName
                    : Path.Combine(uploadsPath);

                extractDocument.DoExtract(fileName, true, false, string.Empty);

                await using var fileStream = File.OpenRead(extractDocument.FileName);
                var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                return new SapDocumentComponentModel
                {
                    CompId = sdkComponent.ComponentId,
                    ContentType = sdkComponent.ContentType ?? "application/octet-stream",
                    Charset = sdkComponent.CharacterSet ?? "UTF-8",
                    Version = sdkComponent.ApplicationVersion,
                    ContentLength = sdkComponent.Bytes,
                    CreationDate = sdkComponent.ArchiveDate,
                    ModifiedDate = sdkComponent.DateModified,
                    Status = "online",
                    PVersion = sdkComponent.ArchiveLinkVersion,
                    Data = memoryStream,
                    FileName = fileName
                };
            }
            return null;
        }

        public void AddComponent(string compId, string filePath, string contentType, string charSet, string version)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(compId))
                    return;
                ComponentsAdapter.AddComponent(compId, version, contentType, charSet, filePath);
            }
            catch (Exception ex)
            {
                _log.LogError($"Failed to add component '{compId}' to record {_record.SapDocumentId}", ex);
                throw;
            }
        }
        public void UpdateComponent(IRecordSapComponent component, SapDocumentComponentModel model)
        {          
            ComponentsAdapter.UpdateComponent(component, model);
        }
        
        public void SetRecordMetadata()
        {
            _record.SapModifiedDate = TrimDateTime.Now;
        }

        public void Save()
        {
            try
            {
                _record.Save();
                _log.LogInformation($"Record {_record.SapDocumentId} saved successfully.");
            }
            catch (Exception ex)
            {
                _log.LogError($"Error saving record {_record.SapDocumentId}", ex);
                throw;
            }
        }
    }

}
