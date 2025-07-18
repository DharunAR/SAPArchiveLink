using System.Collections.Concurrent;

namespace SAPArchiveLink
{
    /// <summary>
    /// In-memory implementation of ICounterCache for managing archive counters.
    /// </summary>
    public class InMemoryCounterCache : ICounterCache
    {
        private readonly ConcurrentDictionary<string, ArchiveCounter> _cache = new();

        /// <summary>
        /// Retrieves or creates an ArchiveCounter for the specified archive ID.
        /// </summary>
        /// <param name="archiveId"></param>
        /// <returns></returns>
        public ArchiveCounter GetOrCreate(string archiveId)
        {
            return _cache.GetOrAdd(archiveId, new ArchiveCounter());
        }

        /// <summary>
        ///returns all archive counters in the cache.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyDictionary<string, ArchiveCounter> GetAll()
        {
            return new Dictionary<string, ArchiveCounter>(_cache);
        }

        /// <summary>
        /// Resets the ArchiveCounter for the specified archive ID.
        /// </summary>
        /// <param name="archiveId"></param>
        public void Reset(string archiveId)
        {
            _cache[archiveId] = new ArchiveCounter();
        }
    }
}
