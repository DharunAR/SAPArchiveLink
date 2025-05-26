namespace SAPArchiveLink
{
    public class LogHelper<T> : ILogHelper<T>
    {
        private readonly ILogger<T> _logger;

        public LogHelper(ILogger<T> logger)
        {
            _logger = logger;
        }

        public void LogInformation(string message)
        {
            _logger.LogInformation(message);
        }

        public void LogWarning(string message)
        {
            _logger.LogWarning(message);
        }

        public void LogError(string message, Exception? ex = null)
        {
            _logger.LogError(ex, message);
        }
    }
}
