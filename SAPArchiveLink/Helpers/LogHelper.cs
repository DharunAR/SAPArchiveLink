namespace SAPArchiveLink
{
    public class LogHelper<T> : ILogHelper<T>
    {
        private readonly ILogger<T> _logger;

        public LogHelper(ILogger<T> logger)
        {
            _logger = logger;
        }

        public void LogInformation(string message, params object[] args)
        {
            _logger.LogInformation(message,args);
        }

        public void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message,args);
        }

        public void LogError(string message, Exception? ex = null, params object[] args)
        {
            _logger.LogError(ex, message,args);
        }
    }
}
