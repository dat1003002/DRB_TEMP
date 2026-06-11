using DRB_TEMP.Data;
using Microsoft.EntityFrameworkCore;

namespace DRB_TEMP.Service
{
    public class MidnightCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MidnightCleanupService> _logger;

        public MidnightCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<MidnightCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextMidnight = now.Date.AddDays(1);
                var delay = nextMidnight - now;

                _logger.LogInformation("Midnight cleanup scheduled in {Minutes} minutes.", (int)delay.TotalMinutes);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested) break;

                // Kiểm tra lại wall-clock sau delay để tránh NTP/DST làm lệch giờ
                if (DateTime.Now < nextMidnight.AddMinutes(-1))
                {
                    _logger.LogWarning("Midnight cleanup woke up early, skipping.");
                    continue;
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var deleted = await db.TemperatureIntradayLogs.ExecuteDeleteAsync(stoppingToken);

                    _logger.LogInformation("Midnight cleanup: removed {Count} intraday records.", deleted);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during midnight cleanup.");
                }
            }
        }
    }
}
