using System.Diagnostics;
using System.Globalization;
using DRB_TEMP.Models;
using DRB_TEMP.Service;
using Microsoft.AspNetCore.Mvc;

namespace DRB_TEMP.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly KepwareService _kepwareService;
        private readonly IHomeService _homeService;

        public HomeController(
            ILogger<HomeController> logger,
            KepwareService kepwareService,
            IHomeService homeService)
        {
            _logger = logger;
            _kepwareService = kepwareService;
            _homeService = homeService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("/GetData")]
        public async Task<IActionResult> GetData()
        {
            try
            {
                var tempNodeId = "ns=2;s=CTL.Nhiet Do Xuong.Nhiet Do";
                var humNodeId = "ns=2;s=CTL.Nhiet Do Xuong.Do Am";

                var tags = new List<string>
                {
                    tempNodeId,
                    humNodeId
                };

                var data = await _kepwareService.ReadMultipleTagsAsync(tags);

                data.TryGetValue(tempNodeId, out var tempValue);
                data.TryGetValue(humNodeId, out var humValue);

                var tempText = tempValue?.ToString() ?? "";
                var humText = humValue?.ToString() ?? "";

                var nhietDo = ConvertToDouble(tempText);
                var doAm = ConvertToDouble(humText);

                var dailyLogSaved = await _homeService.SaveDailyLogAt13hAsync(nhietDo, doAm);

                return Json(new
                {
                    success = true,
                    nhietDo = tempText,
                    doAm = humText,
                    time = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    dailyLogSaved
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi đọc dữ liệu từ Kepware");

                return Json(new
                {
                    success = false,
                    nhietDo = "",
                    doAm = "",
                    time = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    dailyLogSaved = false,
                    error = ex.Message
                });
            }
        }

        [HttpGet("/GetLast7DaysTemperature")]
        public async Task<IActionResult> GetLast7DaysTemperature()
        {
            try
            {
                var data = await _homeService.GetLast7DaysTemperatureAsync();

                return Json(new
                {
                    success = true,
                    data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lấy dữ liệu nhiệt độ 7 ngày qua");

                return Json(new
                {
                    success = false,
                    data = new List<object>(),
                    error = ex.Message
                });
            }
        }

        [HttpGet("/GetCurrentMonthTemperature")]
        public async Task<IActionResult> GetCurrentMonthTemperature()
        {
            try
            {
                var data = await _homeService.GetCurrentMonthTemperatureAsync();

                return Json(new
                {
                    success = true,
                    data,
                    monthLabel = DateTime.Now.ToString("MM/yyyy")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lấy dữ liệu nhiệt độ trong tháng");

                return Json(new
                {
                    success = false,
                    data = new List<object>(),
                    monthLabel = DateTime.Now.ToString("MM/yyyy"),
                    error = ex.Message
                });
            }
        }

        private static double? ConvertToDouble(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            value = value.Replace(",", ".");

            if (double.TryParse(
                    value,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var result))
            {
                return result;
            }

            return null;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
        [HttpGet("/GetLatestDailyLog")]
        public async Task<IActionResult> GetLatestDailyLog()
        {
            try
            {
                var data = await _homeService.GetLatestDailyLogAsync();

                return Json(new
                {
                    success = true,
                    data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lấy dữ liệu nhiệt độ hôm nay từ database");

                return Json(new
                {
                    success = false,
                    data = (object?)null,
                    error = ex.Message
                });
            }
        }
    }
}