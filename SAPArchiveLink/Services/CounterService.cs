namespace SAPArchiveLink
{
    public class CounterService
    {
        private readonly ICounterCache _cache;
        private readonly ILogHelper<CounterService> _logHelper;

        public CounterService(ICounterCache cache, ILogHelper<CounterService> logHelper)
        {
            _cache = cache;
            _logHelper = logHelper;
        }

        public void UpdateCounter(string archiveId, CounterType type, int value)
        {
            try
            {
                if (value <= 0)
                    return;
                var counter = _cache.GetOrCreate(archiveId);

                switch (type)
                {
                    case CounterType.Create:
                        counter.IncrementCreate(value);
                        break;
                    case CounterType.Delete:
                        counter.IncrementDelete(value);
                        break;
                    case CounterType.Update:
                        counter.IncrementUpdate(value);
                        break;
                    case CounterType.View:
                        counter.IncrementView(value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error updating counter for archive {archiveId} with type {type}: {ex.Message}");
            }           
        }
    }
}
