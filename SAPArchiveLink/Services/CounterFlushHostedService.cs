using Microsoft.Extensions.Options;

namespace SAPArchiveLink.Services
{
    public class CounterFlushHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<CounterFlushHostedService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer? _timer;
        private readonly IOptionsMonitor<TrimConfigSettings> config;
        public CounterFlushHostedService(
            ILogger<CounterFlushHostedService> logger,
            IServiceScopeFactory scopeFactory, IOptionsMonitor<TrimConfigSettings> config)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(FlushCounters, null, TimeSpan.Zero, TimeSpan.FromHours(1));
            return Task.CompletedTask;
        }

        private void FlushCounters(object? state)
        {            
            using var scope = _scopeFactory.CreateScope();
            var flusher = scope.ServiceProvider.GetRequiredService<CounterFlusher>();
            flusher.Flush();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose() => _timer?.Dispose();
    }

}
