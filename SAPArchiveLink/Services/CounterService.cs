namespace SAPArchiveLink.Services
{
    public class CounterService
    {
        private readonly ICounterCache _cache;

        public CounterService(ICounterCache cache)
        {
            _cache = cache;
        }

        public void UpdateCounter(string archiveId, CounterType type, int value)
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
    }
}
