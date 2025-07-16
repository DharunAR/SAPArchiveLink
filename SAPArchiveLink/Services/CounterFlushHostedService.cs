namespace SAPArchiveLink
{
    public class CounterFlushHostedService : IHostedService, IDisposable
    {
        private readonly ILogHelper<CounterFlushHostedService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer? _timer;

        public CounterFlushHostedService(
            ILogHelper<CounterFlushHostedService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _timer = new Timer(FlushCounters, null, TimeSpan.Zero, TimeSpan.FromHours(1));
                _logger.LogInformation($"CounterFlushHostedService started.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error starting CounterFlushHostedService: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        private void FlushCounters(object? state)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var flusher = scope.ServiceProvider.GetRequiredService<CounterFlusher>();
                _logger.LogInformation($"FlushCounters processing started...");
                flusher.Flush();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Flush Counter process failed: {ex.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _timer?.Change(Timeout.Infinite, 0);
                _timer?.Dispose();
                _timer = null;
                _logger.LogInformation("CounterFlushHostedService stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error stopping CounterFlushHostedService: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _timer = null;
        }
    }

}
