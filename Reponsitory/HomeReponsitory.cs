using DRB_TEMP.Data;
using DRB_TEMP.Models;
using Microsoft.EntityFrameworkCore;

namespace DRB_TEMP.Reponsitory
{
    public class HomeReponsitory : IHomeReponsitory
    {
        private readonly ApplicationDbContext _context;

        public HomeReponsitory(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasLogTodayAsync(DateTime date)
        {
            var logDate = date.Date;

            return await _context.TemperatureDailyLogs
                .AnyAsync(x => x.LogDate == logDate);
        }

        public async Task SaveDailyLogAsync(TemperatureDailyLog log)
        {
            _context.TemperatureDailyLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TemperatureDailyLog>> GetLogsByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            var from = fromDate.Date;
            var to = toDate.Date.AddDays(1);

            return await _context.TemperatureDailyLogs
                .AsNoTracking()
                .Where(x => x.LogDate >= from && x.LogDate < to)
                .OrderBy(x => x.LogDate)
                .ToListAsync();
        }

        public async Task<TemperatureDailyLog?> GetLatestDailyLogAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            return await _context.TemperatureDailyLogs
                .AsNoTracking()
                .Where(x => x.LogDate >= today && x.LogDate < tomorrow)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<TemperatureDailyLog?> GetTodayLogAsync(DateTime date)
        {
            var logDate = date.Date;

            return await _context.TemperatureDailyLogs
                .FirstOrDefaultAsync(x => x.LogDate == logDate);
        }

        public async Task UpdateDailyLogAsync(TemperatureDailyLog log)
        {
            _context.TemperatureDailyLogs.Update(log);
            await _context.SaveChangesAsync();
        }

        public async Task SaveIntradayLogAsync(TemperatureIntradayLog log)
        {
            _context.TemperatureIntradayLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TemperatureIntradayLog>> GetTodayIntradayLogsAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            return await _context.TemperatureIntradayLogs
                .AsNoTracking()
                .Where(x => x.RecordedAt >= today && x.RecordedAt < tomorrow)
                .OrderBy(x => x.RecordedAt)
                .ToListAsync();
        }

        public async Task ClearIntradayLogsAsync()
        {
            await _context.TemperatureIntradayLogs.ExecuteDeleteAsync();
        }
    }
}
