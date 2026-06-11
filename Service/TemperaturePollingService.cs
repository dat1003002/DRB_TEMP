using DRB_TEMP.Data;
using DRB_TEMP.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace DRB_TEMP.Service
{
    public class TemperaturePollingService : BackgroundService
    {
        private readonly KepwareService _kepwareService;
        private readonly TemperatureCache _cache;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TemperaturePollingService> _logger;

        private const string TempNodeId = "ns=2;s=CTL.Nhiet Do Xuong.Nhiet Do";
        private const string HumNodeId = "ns=2;s=CTL.Nhiet Do Xuong.Do Am";

        public TemperaturePollingService(
            KepwareService kepwareService,
            TemperatureCache cache,
            IServiceScopeFactory scopeFactory,
            ILogger<TemperaturePollingService> logger)
        {
            _kepwareService = kepwareService;
            _cache = cache;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var start = DateTime.Now;

                try
                {
                    var data = await _kepwareService.ReadMultipleTagsAsync(
                        new List<string> { TempNodeId, HumNodeId });

                    data.TryGetValue(TempNodeId, out var tempRaw);
                    data.TryGetValue(HumNodeId, out var humRaw);

                    var nhietDo = ConvertToDouble(tempRaw?.ToString());
                    var doAm = ConvertToDouble(humRaw?.ToString());

                    if (nhietDo != null)
                    {
                        _cache.Update(nhietDo, doAm);

                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        var dailySaved = await SaveHighestAsync(db, nhietDo, doAm, stoppingToken);

                        if (dailySaved) _cache.MarkDailyLogUpdated();

                        await SaveIntradayAsync(db, nhietDo, doAm, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in temperature polling.");
                }

                // Giữ đúng chu kỳ 1 giây dù xử lý mất bao lâu
                var elapsed = (DateTime.Now - start).TotalMilliseconds;
                var remaining = 1000 - (int)elapsed;

                if (remaining > 0)
                    await Task.Delay(remaining, stoppingToken);
            }
        }

        private static async Task<bool> SaveHighestAsync(
            ApplicationDbContext db,
            double? nhietDo,
            double? doAm,
            CancellationToken ct)
        {
            var today = DateTime.Today;

            var log = await db.TemperatureDailyLogs
                .FirstOrDefaultAsync(x => x.LogDate == today, ct);

            if (log == null)
            {
                try
                {
                    db.TemperatureDailyLogs.Add(new TemperatureDailyLog
                    {
                        NhietDo = nhietDo,
                        DoAm = doAm,
                        LogDate = today,
                        CreatedAt = DateTime.Now
                    });

                    await db.SaveChangesAsync(ct);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            if (nhietDo > (log.NhietDo ?? double.MinValue))
            {
                log.NhietDo = nhietDo;
                log.DoAm = doAm;
                log.CreatedAt = DateTime.Now;

                await db.SaveChangesAsync(ct);
                return true;
            }

            return false;
        }

        private static async Task SaveIntradayAsync(
            ApplicationDbContext db,
            double? nhietDo,
            double? doAm,
            CancellationToken ct)
        {
            db.TemperatureIntradayLogs.Add(new TemperatureIntradayLog
            {
                NhietDo = nhietDo,
                DoAm = doAm,
                RecordedAt = DateTime.Now
            });

            await db.SaveChangesAsync(ct);
        }

        private static double? ConvertToDouble(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            value = value.Replace(",", ".");

            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : null;
        }
    }
}
