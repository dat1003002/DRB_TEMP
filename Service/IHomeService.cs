using DRB_TEMP.Models;

namespace DRB_TEMP.Service
{
    public interface IHomeService
    {
        Task<bool> SaveHighestTemperatureAsync(double? nhietDo, double? doAm);

        Task<List<object>> GetLast7DaysTemperatureAsync();

        Task<List<object>> GetCurrentMonthTemperatureAsync(int year, int month);

        Task<List<TemperatureDailyLog>> GetMonthTemperatureLogsAsync(int year, int month);

        Task<object?> GetLatestDailyLogAsync();

        Task SaveIntradayLogAsync(double? nhietDo, double? doAm);

        Task<List<object>> GetTodayIntradayTemperatureAsync();

        Task ClearIntradayLogsAsync();
    }
}