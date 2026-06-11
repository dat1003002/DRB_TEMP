using System.Diagnostics;
using ClosedXML.Excel;
using DRB_TEMP.Models;
using DRB_TEMP.Service;
using Microsoft.AspNetCore.Mvc;

namespace DRB_TEMP.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TemperatureCache _temperatureCache;
        private readonly IHomeService _homeService;

        public HomeController(
            ILogger<HomeController> logger,
            TemperatureCache temperatureCache,
            IHomeService homeService)
        {
            _logger = logger;
            _temperatureCache = temperatureCache;
            _homeService = homeService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("/GetData")]
        public IActionResult GetData()
        {
            var (nhietDo, doAm, updatedAt) = _temperatureCache.Get();

            if (nhietDo == null)
            {
                return Json(new
                {
                    success = false,
                    nhietDo = "",
                    doAm = "",
                    time = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    dailyLogSaved = false
                });
            }

            return Json(new
            {
                success = true,
                nhietDo = nhietDo.Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
                doAm = doAm?.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) ?? "",
                time = updatedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                dailyLogSaved = _temperatureCache.ConsumeDailyLogUpdated()
            });
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
        public async Task<IActionResult> GetCurrentMonthTemperature(int? year, int? month)
        {
            try
            {
                var selectedYear = year ?? DateTime.Now.Year;
                var selectedMonth = month ?? DateTime.Now.Month;

                if (selectedMonth < 1 || selectedMonth > 12)
                {
                    selectedYear = DateTime.Now.Year;
                    selectedMonth = DateTime.Now.Month;
                }

                var data = await _homeService.GetCurrentMonthTemperatureAsync(
                    selectedYear,
                    selectedMonth
                );

                return Json(new
                {
                    success = true,
                    data,
                    monthLabel = $"{selectedMonth:00}/{selectedYear}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lấy dữ liệu nhiệt độ trong tháng");

                var fallbackYear = year ?? DateTime.Now.Year;
                var fallbackMonth = month ?? DateTime.Now.Month;

                if (fallbackMonth < 1 || fallbackMonth > 12)
                {
                    fallbackYear = DateTime.Now.Year;
                    fallbackMonth = DateTime.Now.Month;
                }

                return Json(new
                {
                    success = false,
                    data = new List<object>(),
                    monthLabel = $"{fallbackMonth:00}/{fallbackYear}",
                    error = ex.Message
                });
            }
        }

        [HttpGet("/ExportMonthTemperatureToExcel")]
        public async Task<IActionResult> ExportMonthTemperatureToExcel(int? year, int? month)
        {
            try
            {
                var selectedYear = year ?? DateTime.Now.Year;
                var selectedMonth = month ?? DateTime.Now.Month;

                if (selectedMonth < 1 || selectedMonth > 12)
                {
                    selectedYear = DateTime.Now.Year;
                    selectedMonth = DateTime.Now.Month;
                }

                var firstDay = new DateTime(selectedYear, selectedMonth, 1);
                var lastDay = firstDay.AddMonths(1).AddDays(-1);

                var logs = await _homeService.GetMonthTemperatureLogsAsync(
                    selectedYear,
                    selectedMonth
                );

                using var workbook = new XLWorkbook();

                var worksheetName = $"Thang {selectedMonth:00}-{selectedYear}";
                var worksheet = workbook.Worksheets.Add(worksheetName);

                worksheet.Cell(1, 1).Value = $"BÁO CÁO NHIỆT ĐỘ TRONG THÁNG {selectedMonth:00}/{selectedYear}";
                worksheet.Range(1, 1, 1, 5).Merge();

                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Cell(3, 1).Value = "STT";
                worksheet.Cell(3, 2).Value = "Ngày";
                worksheet.Cell(3, 3).Value = "Nhiệt độ cao nhất (°C)";
                worksheet.Cell(3, 4).Value = "Độ ẩm (%)";
                worksheet.Cell(3, 5).Value = "Thời gian cập nhật";

                var headerRange = worksheet.Range(3, 1, 3, 5);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(226, 232, 240);
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                var row = 4;
                var index = 1;

                for (var date = firstDay; date <= lastDay; date = date.AddDays(1))
                {
                    var log = logs.FirstOrDefault(x => x.LogDate.Date == date.Date);

                    worksheet.Cell(row, 1).Value = index;
                    worksheet.Cell(row, 2).Value = date.ToString("dd/MM/yyyy");

                    if (log?.NhietDo != null)
                    {
                        worksheet.Cell(row, 3).Value = log.NhietDo.Value;
                    }
                    else
                    {
                        worksheet.Cell(row, 3).Value = "--";
                    }

                    if (log?.DoAm != null)
                    {
                        worksheet.Cell(row, 4).Value = log.DoAm.Value;
                    }
                    else
                    {
                        worksheet.Cell(row, 4).Value = "--";
                    }

                    worksheet.Cell(row, 5).Value = log != null
                        ? log.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss")
                        : "--";

                    var dataRange = worksheet.Range(row, 1, row, 5);
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    if (log?.NhietDo >= 38)
                    {
                        dataRange.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 240, 240);
                        worksheet.Cell(row, 3).Style.Font.FontColor = XLColor.FromArgb(225, 29, 29);
                        worksheet.Cell(row, 3).Style.Font.Bold = true;
                    }

                    row++;
                    index++;
                }

                worksheet.Columns().AdjustToContents();

                worksheet.Column(1).Width = 8;
                worksheet.Column(2).Width = 16;
                worksheet.Column(3).Width = 24;
                worksheet.Column(4).Width = 14;
                worksheet.Column(5).Width = 24;

                worksheet.SheetView.FreezeRows(3);

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);

                var content = stream.ToArray();
                var fileName = $"Nhiet_do_thang_{selectedMonth:00}_{selectedYear}.xlsx";

                return File(
                    content,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xuất Excel dữ liệu nhiệt độ trong tháng");

                return BadRequest(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        [HttpGet("/GetTodayIntradayTemperature")]
        public async Task<IActionResult> GetTodayIntradayTemperature()
        {
            try
            {
                var data = await _homeService.GetTodayIntradayTemperatureAsync();

                var peak = data
                    .Cast<dynamic>()
                    .OrderByDescending(x => (double)x.nhietDo)
                    .FirstOrDefault();

                return Json(new
                {
                    success = true,
                    data,
                    peakTime = peak != null ? (string)peak.time : "--",
                    peakValue = peak != null ? (double)peak.nhietDo : 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lấy dữ liệu nhiệt độ trong ngày");

                return Json(new
                {
                    success = false,
                    data = new List<object>(),
                    peakTime = "--",
                    peakValue = 0,
                    error = ex.Message
                });
            }
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
    }
}