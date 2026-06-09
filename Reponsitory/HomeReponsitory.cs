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
            return await _context.TemperatureDailyLogs
                .AsNoTracking()
                .Where(x => x.LogDate.Date >= fromDate.Date &&
                            x.LogDate.Date <= toDate.Date)
                .OrderBy(x => x.LogDate)
                .ToListAsync();
        }

        public async Task<TemperatureDailyLog?> GetLatestDailyLogAsync()
        {
            var today = DateTime.Today;

            return await _context.TemperatureDailyLogs
                .AsNoTracking()
                .Where(x => x.LogDate.Date == today)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<TemperatureDailyLog?> GetTodayLogAsync(DateTime date)
        {
            return await _context.TemperatureDailyLogs
                .FirstOrDefaultAsync(x => x.LogDate.Date == date.Date);
        }

        public async Task UpdateDailyLogAsync(TemperatureDailyLog log)
        {
            _context.TemperatureDailyLogs.Update(log);
            await _context.SaveChangesAsync();
        }
    }
}