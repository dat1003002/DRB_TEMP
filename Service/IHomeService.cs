namespace DRB_TEMP.Service
{
    public interface IHomeService
    {
        Task<bool> SaveDailyLogAt13hAsync(double? nhietDo, double? doAm);

        Task<List<object>> GetLast7DaysTemperatureAsync();

        Task<List<object>> GetCurrentMonthTemperatureAsync();

        Task<object?> GetLatestDailyLogAsync();
    }
}