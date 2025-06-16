using TRIM.SDK;

namespace SAPArchiveLink
{
    public class TrimRepository : ITrimRepository
    {
        private readonly Database _db;
        private readonly TrimConfigSettings _trimConfig;
        private readonly ILoggerFactory _loggerFactory;

        public TrimRepository(Database database, TrimConfigSettings trimConfig, ILoggerFactory loggerFactory)
        {
            _db = database ?? throw new ArgumentNullException(nameof(database));
            _trimConfig = trimConfig ?? throw new ArgumentNullException(nameof(trimConfig));
            _loggerFactory = loggerFactory;
        }

        public IArchiveRecord? GetRecord(string docId, string contRep)
        {
            var tmos = new TrimMainObjectSearch(_db, BaseObjectTypes.Record);

            var docClause = new TrimSearchClause(_db, BaseObjectTypes.Record, SearchClauseIds.RecordSapdoc);
            docClause.SetCriteriaFromString(docId);
            tmos.AddSearchClause(docClause);
            var contRepClause = new TrimSearchClause(_db, BaseObjectTypes.Record, SearchClauseIds.RecordSaprepos);
            contRepClause.SetCriteriaFromString(contRep);
            tmos.AddSearchClause(contRepClause);

            if (tmos.Count > 0)
            {
                var uris = tmos.GetResultAsUriArray(1);
                var record = new Record(_db, uris[0]);
                return new ArchiveRecord(record, _trimConfig, new LogHelper<ArchiveRecord>(_loggerFactory.CreateLogger<ArchiveRecord>()));
            }

            return null;
        }

        public IArchiveRecord? CreateRecord(CreateSapDocumentModel model)
        {
            return ArchiveRecord.CreateNewArchiveRecord(_db, _trimConfig, new LogHelper<ArchiveRecord>(_loggerFactory.CreateLogger<ArchiveRecord>()), model);
        }

        public void Dispose() => _db.Dispose();
    }
}
