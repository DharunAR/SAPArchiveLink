using Microsoft.Extensions.Options;
using TRIM.SDK;

namespace SAPArchiveLink
{
    public class ArchiveRecord : IArchiveRecord
    {
        private Record _record;
        private readonly TrimConfigSettings _trimConfig;
        private List<SapDocumentComponent>? _cachedComponents;
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

        private RecordSapComponentsAdapter ComponentsAdapter => 
            _componentsAdapter ??= new RecordSapComponentsAdapter(_record.ChildSapComponents);

        public List<SapDocumentComponent> GetAllComponents()
        {
            return _cachedComponents ??= ComponentsAdapter.GetAllComponents();
        }

        public SapDocumentComponent? GetComponentById(string compId)
        {
            return ComponentsAdapter.GetComponentById(compId);
        }

        public bool HasComponent(string compId)
        {
            var components = GetAllComponents();
            return components.Any(c => c.CompId == compId);
        }

        public async Task<List<SapDocumentComponent>> ExtractAllComponents()
        {
            var result = new List<SapDocumentComponent>();

            foreach (RecordSapComponent sdkComponent in _record.ChildSapComponents)
            {
                var sapComponent = await ExtractToSapComponent(sdkComponent);
                if (sapComponent != null)
                    result.Add(sapComponent);
            }

            return result;
        }

        public async Task<SapDocumentComponent?> ExtractComponentById(string compId)
        {
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
                logger.LogError($"No valid record type found for content repository '{model.ContRep}'.");
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
                var tmo = db.FindTrimObjectByName(BaseObjectTypes.RecordType, config.RecordTypeName);
                if (tmo is RecordType rt && rt.UsualBehaviour == RecordBehaviour.SapDocument)
                    return rt;

                logger.LogError($"Invalid record type: '{config.RecordTypeName}'. Must be SAP Document behavior.");
                return null;
            }

            var tmos = new TrimMainObjectSearch(db, BaseObjectTypes.RecordType);
            var clause = new TrimSearchClause(db, BaseObjectTypes.RecordType, SearchClauseIds.RecordTypeSaprepository);
            clause.SetCriteriaFromString(contRep);
            tmos.AddSearchClause(clause);

            if (tmos.Count > 0)
            {
                var uris = tmos.GetResultAsUriArray(1);
                return new RecordType(db, uris[0]);
            }

            logger.LogError($"No RecordType found for content repository '{contRep}'.");
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

        private async Task<SapDocumentComponent> ExtractToSapComponent(RecordSapComponent sdkComponent)
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

                return new SapDocumentComponent
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

                var now = DateTime.UtcNow;

                var compList = _record.ChildSapComponents;
                var sapComponent = compList.New();

                sapComponent.ComponentId = compId;
                sapComponent.ApplicationVersion = version;
                sapComponent.ContentType = contentType;
                sapComponent.CharacterSet = charSet;
                sapComponent.ArchiveDate = now;
                sapComponent.DateModified = now;
                sapComponent.SetDocument(filePath);

                _record.SapModifiedDate = now;
            }
            catch (Exception ex)
            {
                _log.LogError($"Failed to add component '{compId}' to record {_record.SapDocumentId}", ex);
                throw;
            }
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
