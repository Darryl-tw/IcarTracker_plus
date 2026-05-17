using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using System.Text.Json;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Web.Resources;

namespace TrackerPlus.Web.Controllers;

[Authorize]
[Route("History/[action]")]
public class HistoryController : Controller
{
    private readonly IHistoryService _historyService;
    private readonly ITrackerService _trackerService;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly IGoogleApiKeyService _googleApiKeyService;

    public HistoryController(IHistoryService historyService, ITrackerService trackerService,
        IStringLocalizer<SharedResources> localizer, IGoogleApiKeyService googleApiKeyService)
    {
        _historyService = historyService;
        _trackerService = trackerService;
        _localizer = localizer;
        _googleApiKeyService = googleApiKeyService;
    }

    private int GetMemberTbKey()
    {
        var val = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(val, out var key) ? key : 0;
    }

    private int GetMemberTimezone()
    {
        var val = User.FindFirstValue("Timezoom");
        return int.TryParse(val, out var tz) ? tz : 480;
    }

    /// <summary>將查詢參數日期（通常為 00:00）轉為含當日全天的區間。</summary>
    private static (DateTime localStart, DateTime localEnd) ResolveLocalDateRange(DateTime? startDate, DateTime? endDate)
    {
        var start = (startDate ?? DateTime.Today).Date;
        var endDay = (endDate ?? start).Date;
        if (endDay < start) endDay = start;
        var end = endDay.AddDays(1).AddSeconds(-1);
        return (start, end);
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? imei, DateTime? startDate, DateTime? endDate,
        string type = "GPS", int page = 1)
    {
        var memberTbKey = GetMemberTbKey();
        var memberTimezone = GetMemberTimezone();
        var trackers = await _trackerService.GetMemberTrackersAsync(memberTbKey);
        ViewBag.Trackers = trackers;
        ViewBag.Imei = imei;
        ViewBag.Type = type;
        ViewBag.Page = page;

        if (string.IsNullOrEmpty(imei))
            return View();

        var (localStart, localEnd) = ResolveLocalDateRange(startDate, endDate);
        ViewBag.StartDate = localStart.ToString("yyyy-MM-dd");
        ViewBag.EndDate = localEnd.Date.ToString("yyyy-MM-dd");

        const int pageSize = 50;
        var result = type switch
        {
            "LBS" => await _historyService.GetLBSHistoryAsync(imei, localStart, localEnd, memberTimezone, page, pageSize),
            "WiFi" => await _historyService.GetWifiHistoryAsync(imei, localStart, localEnd, memberTimezone, page, pageSize),
            _ => await _historyService.GetGPSHistoryAsync(imei, localStart, localEnd, memberTimezone, page, pageSize)
        };

        ViewBag.MemberTimezone = memberTimezone;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Map(string imei, DateTime? startDate, DateTime? endDate,
        string type = "GPS")
    {
        var memberTbKey = GetMemberTbKey();
        var memberTimezone = GetMemberTimezone();

        var (localStart, localEnd) = ResolveLocalDateRange(startDate, endDate);

        var result = type switch
        {
            "LBS" => await _historyService.GetLBSHistoryAsync(imei, localStart, localEnd, memberTimezone, 1, 9999),
            "WiFi" => await _historyService.GetWifiHistoryAsync(imei, localStart, localEnd, memberTimezone, 1, 9999),
            _ => await _historyService.GetGPSHistoryAsync(imei, localStart, localEnd, memberTimezone, 1, 9999)
        };

        ViewBag.Imei = imei;
        ViewBag.Type = type;
        ViewBag.StartDate = localStart.ToString("yyyy-MM-dd");
        ViewBag.EndDate = localEnd.Date.ToString("yyyy-MM-dd");
        ViewBag.MemberTimezone = memberTimezone;
        ViewBag.GoogleApiJs = _googleApiKeyService.GetMapsJavaScriptUrl(Request.Host.Host);
        var mapLogs = result.Logs
            .Where(l => Math.Abs(l.Lat) > 0.0001 && Math.Abs(l.Lng) > 0.0001)
            .Select(l => new
            {
                lat = l.Lat,
                lng = l.Lng,
                speed = l.Speed,
                direction = l.Direction,
                utcTime = l.UTCTime.ToString("o"),
                localTime = l.UTCTime.AddMinutes(memberTimezone).ToString("yyyy-MM-dd HH:mm:ss"),
                distance = l.Distance
            })
            .ToList();

        ViewBag.LogsJson = JsonSerializer.Serialize(mapLogs);
        result.Logs = result.Logs.Where(l => Math.Abs(l.Lat) > 0.0001 && Math.Abs(l.Lng) > 0.0001).ToList();
        result.TotalCount = result.Logs.Count;

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(string imei, DateTime? startDate, DateTime? endDate, string type = "GPS")
    {
        var memberTimezone = GetMemberTimezone();
        var (localStart, localEnd) = ResolveLocalDateRange(startDate, endDate);
        var bytes = await _historyService.ExportToExcelAsync(imei, localStart, localEnd, memberTimezone, type);
        var fileName = $"History_{imei}_{localStart:yyyyMMdd}_{localEnd:yyyyMMdd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> ExportGpx(string imei, DateTime? startDate, DateTime? endDate)
    {
        var memberTimezone = GetMemberTimezone();
        var (localStart, localEnd) = ResolveLocalDateRange(startDate, endDate);
        var gpx = await _historyService.ExportToGPXAsync(imei, localStart, localEnd, memberTimezone);
        var fileName = $"History_{imei}_{localStart:yyyyMMdd}.gpx";
        return File(System.Text.Encoding.UTF8.GetBytes(gpx), "application/gpx+xml", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> ExportKml(string imei, DateTime? startDate, DateTime? endDate)
    {
        var memberTimezone = GetMemberTimezone();
        var (localStart, localEnd) = ResolveLocalDateRange(startDate, endDate);
        var kml = await _historyService.ExportToKMLAsync(imei, localStart, localEnd, memberTimezone);
        var fileName = $"History_{imei}_{localStart:yyyyMMdd}.kml";
        return File(System.Text.Encoding.UTF8.GetBytes(kml), "application/vnd.google-earth.kml+xml", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string imei, DateTime startDate, DateTime endDate)
    {
        var memberTimezone = GetMemberTimezone();
        var result = await _historyService.DeleteHistoryAsync(imei, startDate, endDate, memberTimezone);
        if (!result.Success)
            return BadRequest(result.Message);
        TempData["Success"] = _localizer["History_DeleteSuccess"].Value;
        return RedirectToAction(nameof(Index), new { imei });
    }
}
