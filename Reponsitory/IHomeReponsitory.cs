using DRB_TEMP.Models;

namespace DRB_TEMP.Reponsitory
{
    public interface IHomeReponsitory
    {
        Task<bool> HasLogTodayAsync(DateTime date);

        Task SaveDailyLogAsync(TemperatureDailyLog log);

        Task<List<TemperatureDailyLog>> GetLogsByDateRangeAsync(DateTime fromDate, DateTime toDate);

        Task<TemperatureDailyLog?> GetLatestDailyLogAsync();
    }
}