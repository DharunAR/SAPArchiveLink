
using Microsoft.Extensions.Options;

namespace SAPArchiveLink
{
    public class CounterFlusher
    {
        private readonly ICounterCache _cache;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TrimInitialization _trimInitialization;
        private readonly ILogHelper<CounterFlusher> _logger;

        public CounterFlusher(
            ICounterCache cache,
            IServiceScopeFactory scopeFactory,
            TrimInitialization trimInitialization,
            ILogHelper<CounterFlusher> logger)
        {
            _cache = cache;
            _scopeFactory = scopeFactory;
            _trimInitialization = trimInitialization;
            _logger = logger;
        }

        public void Flush()
        {
            if (_trimInitialization.IsInitialized)
            {
                foreach (var kvp in _cache.GetAll())
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var _databaseConnection = scope.ServiceProvider.GetRequiredService<IDatabaseConnection>();
                        using (ITrimRepository trimRepo = _databaseConnection.GetDatabase())
                        {
                            trimRepo.SaveCounters(kvp.Key, kvp.Value);
                        }
                           
                    }

                    _cache.Reset(kvp.Key);
                }
            }
            else
            {
                _logger.LogInformation("Trim repository is not initialized. Cannot flush counters.");
            }
        }
    }
}
