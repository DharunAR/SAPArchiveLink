namespace SAPArchiveLink
{
    public interface ICounterCache
    {
        ArchiveCounter GetOrCreate(string archiveId);
        IReadOnlyDictionary<string, ArchiveCounter> GetAll();
        void Reset(string archiveId);
    }
}
