using TRIM.SDK;

namespace SAPArchiveLink
{
    public class ArchiveRecord : IArchiveRecord
    {
        private Record _record;
        private readonly TrimConfigSettings _trimConfig;
        private RecordSapComponentsAdapter? _componentsAdapter;
        private readonly ILogHelper<ArchiveRecord> _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArchiveRecord"/> class.
        /// </summary>
        /// <param name="record"></param>
        /// <param name="trimConfig"></param>
        /// <param name="log"></param>
        /// <exception cref="ArgumentNullException"></exception>
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
            _log.LogInformation($"Finding component with ID: {compId} in record {_record.SapDocumentId}");
            return ComponentsAdapter.FindComponentById(compId);
        }

        public bool HasComponent(string compId)
        {
            return FindComponentById(compId) != null;
        }

        /// <summary>
        /// Extracts all components from the record and returns them as a list of <see cref="SapDocumentComponentModel"/>.
        /// </summary>
        /// <param name="extractContent"></param>
        /// <returns></returns>

        public async Task<List<SapDocumentComponentModel>> ExtractAllComponents(bool extractContent = true)
        {
            _log.LogInformation($"Extracting all components from record {_record.SapDocumentId}");
            var result = new List<SapDocumentComponentModel>();

            foreach (RecordSapComponent sdkComponent in _record.ChildSapComponents)
            {
                var sapComponent = await ExtractToSapComponent(sdkComponent, extractContent);
                if (sapComponent != null)
                {
                    result.Add(sapComponent);
                    _log.LogInformation($"Component {sapComponent.CompId} from record {_record.SapDocumentId} extracted succesfully");
                }
            }
            return result;
        }

        /// <summary>
        /// Extracts a specific component by its ID from the record and returns it as a <see cref="SapDocumentComponentModel"/>.
        /// </summary>
        /// <param name="compId"></param>
        /// <param name="extractContent"></param>
        /// <returns></returns>
        public async Task<SapDocumentComponentModel?> ExtractComponentById(string compId, bool extractContent = true)
        {
            _log.LogInformation($"Extracting component with ID: {compId} from record {_record.SapDocumentId}");
            foreach (RecordSapComponent sdkComponent in _record.ChildSapComponents)
            {
                if (sdkComponent.ComponentId == compId)
                {
                    return await ExtractToSapComponent(sdkComponent, extractContent);
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a new ArchiveRecord based on the provided model.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="trimConfig"></param>
        /// <param name="logger"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ArchiveRecord? CreateNewArchiveRecord(Database db, TrimConfigSettings trimConfig, ILogHelper<ArchiveRecord> logger, CreateSapDocumentModel model)
        {
            logger.LogInformation($"Creating new ArchiveRecord for ContRep: {model.ContRep}, DocId: {model.DocId}, PVersion: {model.PVersion}");
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

        /// <summary>
        /// Retrieves a RecordType based on the provided configuration and content repository.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="config"></param>
        /// <param name="contRep"></param>
        /// <param name="logger"></param>
        /// <returns></returns>

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
                        logger.LogInformation($"RecordType found: {rt.Uri}");
                        return rt;
                    }
                    logger.LogInformation($"No RecordType found: {config.RecordTypeName}");
                    return null;
                }
                logger.LogInformation($"No RecordType found: {config.RecordTypeName}");
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
                logger.LogInformation($"RecordType found: {uris[0]}");
                return new RecordType(db, uris[0]);
            }
            logger.LogInformation($"No RecordType by SapRepository: {contRep} found");
            return null;
        }

        /// <summary>
        /// Extracts metadata from a RecordSapComponent and returns it as a SapDocumentComponentModel.
        /// </summary>
        /// <param name="sdkComponent"></param>
        /// <returns></returns>
        public SapDocumentComponentModel ExtractComponentMetadata(RecordSapComponent sdkComponent)
        {
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
                PVersion = sdkComponent.ArchiveLinkVersion
            };
        }

        /// <summary>
        /// Adds a new component to the record with the specified parameters.
        /// </summary>
        /// <param name="compId"></param>
        /// <param name="filePath"></param>
        /// <param name="contentType"></param>
        /// <param name="charSet"></param>
        /// <param name="version"></param>
        public void AddComponent(string compId, string filePath, string contentType, string charSet, string version)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(compId))
                    return;
                _log.LogInformation($"Adding component '{compId}' to record {_record.SapDocumentId}");
                ComponentsAdapter.AddComponent(compId, version, contentType, charSet, filePath);
            }
            catch (Exception ex)
            {
                _log.LogError($"Failed to add component '{compId}' to record {_record.SapDocumentId}", ex);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing component in the record with the provided model.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="model"></param>
        public void UpdateComponent(IRecordSapComponent component, SapDocumentComponentModel model)
        {       
            _log.LogInformation($"Updating component {component.ComponentId} in record {_record.SapDocumentId}");
            ComponentsAdapter.UpdateComponent(component, model);
        }

        /// <summary>
        ///     Sets the metadata for the record, including the modified date.
        /// </summary>

        public void SetRecordMetadata()
        {
            _record.SapModifiedDate = TrimDateTime.Now;
        }

        /// <summary>
        ///  saves the record to the database.
        /// </summary>
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

        /// <summary>
        /// Deletes a component from the record by its ID.
        /// </summary>
        /// <param name="compId"></param>
        /// <returns></returns>
        public bool DeleteComponent(string compId)
        {
            try
            {
                var compToDelete = ComponentsAdapter.FindComponentById(compId);
                if (compToDelete != null)
                {
                    compToDelete.DeleteComponent();
                    _log.LogInformation($"Component {compId} deleted successfully from record {_record.SapDocumentId}.");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _log.LogError($"Error deleting component {compId} from record {_record.SapDocumentId}", ex);
                throw;
            }
        }

        /// <summary>
        /// Deletes the record and all its components.
        /// </summary>
        public void DeleteRecord()
        {
            _record.Delete();
            _log.LogInformation($"Record {_record.Uri} deleted successfully.");
        }

        private static string GenerateTitle(RecordType rt, CreateSapDocumentModel model, DateTime now)
        {
            return rt.SapTitleTemplate
                .Replace("%docid%", model.DocId)
                .Replace("%date%", now.ToString("g"))
                .Replace("%prot%", model.DocProt)
                .Replace("%alvsn%", model.PVersion)
                .Replace("%contrep%", model.ContRep);
        }
        private async Task<SapDocumentComponentModel?> ExtractToSapComponent(RecordSapComponent sdkComponent, bool extractContent)
        {
            var component = ExtractComponentMetadata(sdkComponent);
            if (extractContent)
            {
                var extractDocument = sdkComponent.GetExtractDocument();
                if (extractDocument == null)
                    return null;
                string uploadsPath = Path.Combine(_trimConfig.WorkPath, "Uploads");
                Directory.CreateDirectory(uploadsPath);

                var fileName = File.Exists(extractDocument.FileName) ? extractDocument.FileName : Path.Combine(uploadsPath);

                extractDocument.DoExtract(fileName, true, false, string.Empty);

                await using var fileStream = File.OpenRead(extractDocument.FileName);
                var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                component.Data = memoryStream;
                component.Charset = sdkComponent.CharacterSet ?? "UTF-8";
                component.FileName = extractDocument.FileName;
                component.RecordSapComponent = new RecordSapComponentWrapper(sdkComponent);
            }
            return component;
        }
    }

}
