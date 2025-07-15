using System.Collections.Concurrent;

namespace SAPArchiveLink
{
    public class InMemoryCounterCache : ICounterCache
    {
        private readonly ConcurrentDictionary<string, ArchiveCounter> _cache = new();

        public ArchiveCounter GetOrCreate(string archiveId)
        {
            return _cache.GetOrAdd(archiveId, new ArchiveCounter());
        }

        public IReadOnlyDictionary<string, ArchiveCounter> GetAll()
        {
            return new Dictionary<string, ArchiveCounter>(_cache);
        }

        public void Reset(string archiveId)
        {
            _cache[archiveId] = new ArchiveCounter();
        }
    }
}
