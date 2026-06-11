using DRB_TEMP.Models;
using DRB_TEMP.Reponsitory;

namespace DRB_TEMP.Service
{
    public class HomeService : IHomeService
    {
        private readonly IHomeReponsitory _homeReponsitory;

        public HomeService(IHomeReponsitory homeReponsitory)
        {
            _homeReponsitory = homeReponsitory;
        }

        public async Task<bool> SaveHighestTemperatureAsync(double? nhietDo, double? doAm)
        {
            if (nhietDo == null) return false;

            var today = DateTime.Today;

            var log = await _homeReponsitory.GetTodayLogAsync(today);

            if (log == null)
            {
                try
                {
                    await _homeReponsitory.SaveDailyLogAsync(new TemperatureDailyLog
                    {
                        NhietDo = nhietDo,
                        DoAm = doAm,
                        LogDate = today,
                        CreatedAt = DateTime.Now
                    });

                    return true;
                }
                catch (Exception)
                {
                    // Bản ghi có thể đã được request đồng thời tạo trước,
                    // đọc lại và thử update bên dưới
                    log = await _homeReponsitory.GetTodayLogAsync(today);

                    if (log == null) return false;
                }
            }

            if (nhietDo > (log.NhietDo ?? double.MinValue))
            {
                log.NhietDo = nhietDo;
                log.DoAm = doAm;
                log.CreatedAt = DateTime.Now;

                await _homeReponsitory.UpdateDailyLogAsync(log);

                return true;
            }

            return false;
        }

        public async Task<List<object>> GetLast7DaysTemperatureAsync()
        {
            var today = DateTime.Now.Date;
            var fromDate = today.AddDays(-6);
            var toDate = today;

            var logs = await _homeReponsitory.GetLogsByDateRangeAsync(fromDate, toDate);

            var result = new List<object>();

            for (var date = fromDate; date <= toDate; date = date.AddDays(1))
            {
                var log = logs.FirstOrDefault(x => x.LogDate.Date == date.Date);

                result.Add(new
                {
                    date = date.ToString("dd/MM"),
                    dayName = GetVietnameseDayName(date),
                    value = log?.NhietDo ?? 0
                });
            }

            return result;
        }

        public async Task<List<object>> GetCurrentMonthTemperatureAsync(int year, int month)
        {
            if (month < 1 || month > 12)
            {
                year = DateTime.Now.Year;
                month = DateTime.Now.Month;
            }

            var firstDay = new DateTime(year, month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);

            var logs = await _homeReponsitory.GetLogsByDateRangeAsync(firstDay, lastDay);

            var result = new List<object>();

            for (var date = firstDay; date <= lastDay; date = date.AddDays(1))
            {
                var log = logs.FirstOrDefault(x => x.LogDate.Date == date.Date);

                result.Add(new
                {
                    day = date.Day,
                    date = date.ToString("dd/MM"),
                    value = log?.NhietDo
                });
            }

            return result;
        }

        public async Task<List<TemperatureDailyLog>> GetMonthTemperatureLogsAsync(int year, int month)
        {
            if (month < 1 || month > 12)
            {
                year = DateTime.Now.Year;
                month = DateTime.Now.Month;
            }

            var firstDay = new DateTime(year, month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);

            return await _homeReponsitory.GetLogsByDateRangeAsync(firstDay, lastDay);
        }

        public async Task<object?> GetLatestDailyLogAsync()
        {
            var log = await _homeReponsitory.GetLatestDailyLogAsync();

            if (log == null)
            {
                return new
                {
                    nhietDo = 0,
                    doAm = 0,
                    updateTime = "--"
                };
            }

            return new
            {
                nhietDo = log.NhietDo ?? 0,
                doAm = log.DoAm ?? 0,
                updateTime = log.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss")
            };
        }

        public async Task SaveIntradayLogAsync(double? nhietDo, double? doAm)
        {
            if (nhietDo == null) return;

            await _homeReponsitory.SaveIntradayLogAsync(new Models.TemperatureIntradayLog
            {
                NhietDo = nhietDo,
                DoAm = doAm,
                RecordedAt = DateTime.Now
            });
        }

        public async Task<List<object>> GetTodayIntradayTemperatureAsync()
        {
            var logs = await _homeReponsitory.GetTodayIntradayLogsAsync();

            return logs.Select(x => (object)new
            {
                time = x.RecordedAt.ToString("HH:mm:ss"),
                nhietDo = x.NhietDo ?? 0,
                doAm = x.DoAm ?? 0
            }).ToList();
        }

        public async Task ClearIntradayLogsAsync()
        {
            await _homeReponsitory.ClearIntradayLogsAsync();
        }

        private static string GetVietnameseDayName(DateTime date)
        {
            return date.DayOfWeek switch
            {
                DayOfWeek.Monday => "Thứ 2",
                DayOfWeek.Tuesday => "Thứ 3",
                DayOfWeek.Wednesday => "Thứ 4",
                DayOfWeek.Thursday => "Thứ 5",
                DayOfWeek.Friday => "Thứ 6",
                DayOfWeek.Saturday => "Thứ 7",
                _ => "Chủ nhật"
            };
        }
    }
}