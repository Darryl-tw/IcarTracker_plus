using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;
using TrackerPlus.Web.Resources;

namespace TrackerPlus.Web.Controllers;

[Authorize]
[Route("Map/[action]")]
public class MapController : Controller
{
    private readonly ITrackerService _trackerService;
    private readonly IHistoryService _historyService;
    private readonly IGoogleApiKeyService _googleApiKeyService;
    private readonly ILabelService _labelService;
    private readonly IMapMarkRepository _mapMarkRepository;
    private readonly IDeviceSettingsService _deviceSettingsService;
    private readonly AppSettings _appSettings;
    private readonly ILogger<MapController> _logger;
    private readonly IStringLocalizer<SharedResources> _L;

    public MapController(ITrackerService trackerService, IHistoryService historyService,
        IGoogleApiKeyService googleApiKeyService, ILabelService labelService,
        IMapMarkRepository mapMarkRepository, IDeviceSettingsService deviceSettingsService,
        IOptions<AppSettings> appSettings, ILogger<MapController> logger,
        IStringLocalizer<SharedResources> localizer)
    {
        _trackerService = trackerService;
        _historyService = historyService;
        _googleApiKeyService = googleApiKeyService;
        _labelService = labelService;
        _mapMarkRepository = mapMarkRepository;
        _deviceSettingsService = deviceSettingsService;
        _appSettings = appSettings.Value;
        _logger = logger;
        _L = localizer;
    }

    private int GetMemberTbKey()
    {
        var val = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(val, out var key) ? key : 0;
    }

    private int GetAdminPreviewTbKey()
    {
        var val = User.FindFirstValue("AdminPreviewTbKey");
        return int.TryParse(val, out var key) ? key : 0;
    }

    /// <summary>
    /// 前台裝置列表：管理員「前台(單機)」帶 AdminPreviewTbKey 時只回傳該台；
    /// 一般會員登入／會員預覽（preview=0）則回傳該會員全部裝置。
    /// </summary>
    private async Task<IEnumerable<Tracker>> GetLiveTrackersForCurrentUserAsync()
    {
        var memberTbKey = GetMemberTbKey();
        var previewTbKey = GetAdminPreviewTbKey();

        if (previewTbKey > 0)
        {
            var single = await _trackerService.GetTrackerAsync(previewTbKey);
            if (single == null)
                return Enumerable.Empty<Tracker>();
            // 以會員身份預覽時，確認裝置屬於該會員（或未綁定預覽 member=0）
            if (memberTbKey > 0 && single.Member_TbKey != memberTbKey)
                return Enumerable.Empty<Tracker>();
            return new[] { single };
        }

        if (memberTbKey <= 0)
            return Enumerable.Empty<Tracker>();

        return await _trackerService.GetLiveLocationsAsync(memberTbKey);
    }

    private int GetMemberTimezone()
    {
        var val = User.FindFirstValue("Timezoom");
        return int.TryParse(val, out var tz) ? tz : 480;
    }

    [HttpGet]
    public async Task<IActionResult> Live(int focus = 0)
    {
        var previewTbKey = GetAdminPreviewTbKey();
        var focusTbKey = focus > 0 ? focus : previewTbKey;

        var trackers = await GetLiveTrackersForCurrentUserAsync();

        ViewBag.GoogleApiJs = _googleApiKeyService.GetMapsJavaScriptUrl(Request.Host.Host);
        ViewBag.MemberName = User.FindFirstValue("CName") ?? User.Identity?.Name ?? "";
        ViewBag.MemberTimezone = GetMemberTimezone();
        ViewBag.MaxMultiWindows = _appSettings.MaxMultiWindows;
        ViewBag.FocusTbKey = focusTbKey;
        return View(trackers);
    }

    [HttpGet]
    public async Task<IActionResult> MultiWindow(string? tbKeys)
    {
        var memberTbKey = GetMemberTbKey();
        var validKeys = await FilterOwnedTrackerKeysAsync(tbKeys, memberTbKey);
        if (validKeys.Count == 0)
            return RedirectToAction(nameof(Live));

        ViewBag.TbKeys = validKeys;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> LivePane(int tbKey)
    {
        var memberTbKey = GetMemberTbKey();
        var tracker = await _trackerService.GetTrackerAsync(tbKey);
        if (tracker == null) return NotFound();
        if (tracker.Member_TbKey != memberTbKey) return Forbid();

        ViewBag.GoogleApiJs = _googleApiKeyService.GetMapsJavaScriptUrl(Request.Host.Host);
        ViewBag.MemberTimezone = GetMemberTimezone();
        return View(tracker);
    }

    [HttpGet]
    public async Task<IActionResult> LivePaneData(int tbKey)
    {
        var memberTbKey = GetMemberTbKey();
        var memberTimezone = GetMemberTimezone();
        var tracker = await _trackerService.GetTrackerAsync(tbKey);
        if (tracker == null) return NotFound();
        if (tracker.Member_TbKey != memberTbKey) return Forbid();

        var now = DateTime.UtcNow;
        return Json(new
        {
            tbKey = tracker.TbKey,
            imei = tracker.IMEICODE,
            cname = string.IsNullOrEmpty(tracker.CName) ? tracker.IMEICODE : tracker.CName,
            lat = tracker.Lat,
            lng = tracker.Lng,
            speed = tracker.Speed,
            direction = tracker.Direction,
            voltage = tracker.Voltage,
            csq = tracker.CSQ,
            gpsNo = tracker.GPSNo,
            isOnline = tracker.LastReportTime.HasValue && (now - tracker.LastReportTime.Value).TotalMinutes < 10,
            lastReportLocal = tracker.LastReportTime.HasValue
                ? tracker.LastReportTime.Value.AddMinutes(memberTimezone).ToString("yyyy-MM-dd HH:mm:ss")
                : "",
            iconFile = string.IsNullOrWhiteSpace(tracker.IconFile) ? "A" : tracker.IconFile.Trim().ToUpperInvariant()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Landmarks()
    {
        var memberTbKey = GetMemberTbKey();
        var marks = await _mapMarkRepository.GetByMemberAsync(memberTbKey);
        return Json(marks.Select(m => new
        {
            tbKey = m.TbKey,
            memo = m.Memo,
            address = m.Address,
            lat = m.Lat,
            lng = m.Lng
        }));
    }

    [HttpPost]
    public async Task<IActionResult> AddLandmark([FromBody] AddLandmarkRequest request)
    {
        var memberTbKey = GetMemberTbKey();
        if (string.IsNullOrWhiteSpace(request.Memo))
            return Json(new { success = false, message = _L["Live_LandmarkMemoRequired"].Value });

        if (Math.Abs(request.Lat) < 0.0001 && Math.Abs(request.Lng) < 0.0001)
            return Json(new { success = false, message = _L["Live_LandmarkCoordsInvalid"].Value });

        var count = await _mapMarkRepository.GetCountByMemberAsync(memberTbKey);
        if (count >= 500)
            return Json(new { success = false, message = _L["Live_LandmarkLimit"].Value });

        var id = await _mapMarkRepository.CreateAsync(
            memberTbKey,
            request.Address ?? string.Empty,
            request.Lat,
            request.Lng,
            request.Memo);

        return Json(new { success = true, tbKey = id });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteLandmark([FromBody] DeleteLandmarkRequest request)
    {
        var memberTbKey = GetMemberTbKey();
        var ok = await _mapMarkRepository.DeleteAsync(request.TbKey, memberTbKey);
        return Json(new { success = ok });
    }

    private static string FormatElapsed(TimeSpan t) =>
        t.TotalMinutes >= 1
            ? $"{(int)t.TotalMinutes}m{t.Seconds:D2}s{t.Milliseconds:D3}ms"
            : $"{t.Seconds}s{t.Milliseconds:D3}ms";

    private async Task<List<int>> FilterOwnedTrackerKeysAsync(string? tbKeys, int memberTbKey)
    {
        var result = new List<int>();
        if (string.IsNullOrWhiteSpace(tbKeys)) return result;

        foreach (var part in tbKeys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, out var key)) continue;
            var tracker = await _trackerService.GetTrackerAsync(key);
            if (tracker != null && tracker.Member_TbKey == memberTbKey && !result.Contains(key))
                result.Add(key);
        }
        return result;
    }

    [HttpGet]
    public async Task<IActionResult> LiveData()
    {
        var memberTimezone = GetMemberTimezone();
        var trackers = await GetLiveTrackersForCurrentUserAsync();
        var now = DateTime.UtcNow;
        var data = trackers.Select(t => new
        {
            tbKey = t.TbKey,
            imei = t.IMEICODE,
            cname = string.IsNullOrEmpty(t.CName) ? t.IMEICODE : t.CName,
            lat = t.Lat,
            lng = t.Lng,
            speed = t.Speed,
            direction = t.Direction,
            lastReportTime = t.LastReportTime?.ToString("o"),
            lastReportLocal = t.LastReportTime.HasValue
                ? t.LastReportTime.Value.AddMinutes(memberTimezone).ToString("yyyy-MM-dd HH:mm:ss")
                : "",
            voltage = t.Voltage,
            voltageDisplay = t.VoltageDisplay,
            csq = t.CSQ,
            gpsNo = t.GPSNo,
            gpsSatellitesUsed = t.GpsSatellitesUsed,
            gpsSatellitesTotal = t.GpsSatellitesTotal,
            powerSaving = t.PowerSavingMode,
            isOnline = t.LastReportTime.HasValue && (now - t.LastReportTime.Value).TotalMinutes < 10,
            groupName = t.GroupName,
            label = t.Label,
            serviceEndDate = t.ServiceEndDate?.ToString("yyyy-MM-dd"),
            serviceDays = t.EffectiveDays,
            firmwareVersion = t.FirmwareVersion,
            iconFile = string.IsNullOrWhiteSpace(t.IconFile) ? "A" : t.IconFile.Trim().ToUpperInvariant(),
        });
        return Json(data);
    }

    [HttpGet]
    public async Task<IActionResult> DeviceSettings(int tbKey)
    {
        var memberTbKey = GetMemberTbKey();
        var dto = await _deviceSettingsService.GetSettingsAsync(tbKey, memberTbKey);
        if (dto == null) return NotFound();
        return Json(dto);
    }

    [HttpGet]
    public async Task<IActionResult> DeviceDetail(int tbKey)
    {
        var memberTbKey = GetMemberTbKey();
        var dto = await _deviceSettingsService.GetDetailAsync(tbKey, memberTbKey, GetMemberTimezone());
        if (dto == null) return NotFound();
        return Json(dto);
    }

    [HttpPost]
    public async Task<IActionResult> SaveDeviceSettings([FromBody] SaveDeviceSettingsRequest request)
    {
        var memberTbKey = GetMemberTbKey();
        if (request.TbKey <= 0) return BadRequest();

        var saveRequest = new DeviceSettingsSaveRequest
        {
            Cname = request.Cname ?? string.Empty,
            IconFile = request.IconFile ?? "A",
            LabelTbKeys = request.LabelTbKeys?.ToList() ?? [],
            UdFields = (request.UdFields ?? []).Select(f => new DeviceUdFieldSave
            {
                FieldTbKey = f.FieldTbKey,
                Value = f.Value ?? string.Empty
            }).ToList(),
            GpsReportTime = request.GpsReportTime,
            ShockMode = request.ShockMode,
            ShockSensitive = request.ShockSensitive,
            ApMinute = request.ApMinute,
            ApMsg = request.ApMsg ?? string.Empty,
            AsMinute = request.AsMinute,
            AsMsg = request.AsMsg ?? string.Empty,
            ShareEnabled = request.ShareEnabled,
            SharePassword = request.SharePassword ?? "0000",
            AlertMode = request.AlertMode
        };

        var result = await _deviceSettingsService.SaveSettingsAsync(request.TbKey, memberTbKey, saveRequest);

        if (!result.Success)
            return Json(new { success = false, message = result.Message });

        return Json(new { success = true, message = result.Message });
    }

    [HttpGet]
    public async Task HistoryDataStream(string imei, string? date, string type = "GPS")
    {
        var memberTbKey = GetMemberTbKey();
        var memberTimezone = GetMemberTimezone();

        var trackers = await _trackerService.GetLiveLocationsAsync(memberTbKey);
        if (!trackers.Any(t => t.IMEICODE == imei))
        {
            Response.StatusCode = 403;
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        var ct = HttpContext.RequestAborted;
        var queryDate = DateTime.TryParse(date, out var d) ? d.Date : DateTime.Today;
        var localStart = queryDate;
        var localEnd = queryDate.AddDays(1).AddSeconds(-1);

        const int batchSize = 150;
        var batch = new List<object>(batchSize);
        int seq = 0;
        double totalDistance = 0, maxSpeed = 0, totalSpeedSum = 0;

        var stream = type == "LBS"
            ? _historyService.StreamLBSHistoryAsync(imei, localStart, localEnd, memberTimezone, ct)
            : type == "Combined"
            ? _historyService.StreamCombinedHistoryAsync(imei, localStart, localEnd, memberTimezone, ct)
            : _historyService.StreamGPSHistoryAsync(imei, localStart, localEnd, memberTimezone, ct);

        async Task FlushBatch()
        {
            if (batch.Count == 0) return;
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(new { type = "batch", logs = batch })}\n\n", ct);
            await Response.Body.FlushAsync(ct);
            batch.Clear();
        }

        try
        {
            await foreach (var l in stream.WithCancellation(ct))
            {
                if (Math.Abs(l.Lat) < 0.0001 && Math.Abs(l.Lng) < 0.0001) continue;
                seq++;
                batch.Add(new
                {
                    seq,
                    lat = l.Lat,
                    lng = l.Lng,
                    speed = Math.Round(l.Speed, 1),
                    direction = l.Direction,
                    time = l.UTCTime.AddMinutes(memberTimezone).ToString("yyyy-MM-dd HH:mm:ss"),
                    distanceM = (int)Math.Round(l.Distance, 0),
                    distanceKm = Math.Round(l.Distance / 1000.0, 3),
                    gpsNo = l.GPSNo,
                    hdop = Math.Round(l.HDOP, 2),
                    csq = l.CSQ,
                    voltage = l.Voltage,
                    voltageV = l.VoltageStr,
                    logType = l.Type
                });
                totalDistance += l.Distance;
                if (l.Speed > maxSpeed) maxSpeed = l.Speed;
                totalSpeedSum += l.Speed;

                if (batch.Count >= batchSize) await FlushBatch();
            }
            await FlushBatch();

            await Response.WriteAsync($"data: {JsonSerializer.Serialize(new
            {
                type = "done",
                totalDistance = Math.Round(totalDistance / 1000.0, 2),
                maxSpeed = Math.Round(maxSpeed, 1),
                avgSpeed = seq > 0 ? Math.Round(totalSpeedSum / seq, 1) : 0.0,
                totalCount = seq
            })}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (OperationCanceledException) { /* client disconnected */ }
    }

    [HttpGet]
    public async Task<IActionResult> HistoryData(string imei, string? date, string type = "GPS")
    {
        var sw = Stopwatch.StartNew();
        var memberTbKey = GetMemberTbKey();
        var memberTimezone = GetMemberTimezone();

        var trackers = await _trackerService.GetLiveLocationsAsync(memberTbKey);
        if (!trackers.Any(t => t.IMEICODE == imei))
            return Forbid();

        var queryDate = DateTime.TryParse(date, out var d) ? d.Date : DateTime.Today;
        var localStart = queryDate;
        var localEnd = queryDate.AddDays(1).AddSeconds(-1);

        var result = type == "LBS"
            ? await _historyService.GetLBSHistoryAsync(imei, localStart, localEnd, memberTimezone, 1, 5000)
            : await _historyService.GetGPSHistoryAsync(imei, localStart, localEnd, memberTimezone, 1, 5000);

        var logs = result.Logs
            .Where(l => Math.Abs(l.Lat) > 0.0001 && Math.Abs(l.Lng) > 0.0001)
            .Select((l, i) => new
            {
                seq = i + 1,
                lat = l.Lat,
                lng = l.Lng,
                speed = Math.Round(l.Speed, 1),
                direction = l.Direction,
                time = l.UTCTime.AddMinutes(memberTimezone).ToString("yyyy-MM-dd HH:mm:ss"),
                distanceM = Math.Round(l.Distance, 0),
                distanceKm = Math.Round(l.Distance / 1000.0, 3),
                gpsNo = l.GPSNo,
                hdop = Math.Round(l.HDOP, 2),
                csq = l.CSQ,
                voltage = l.Voltage,
                voltageV = l.VoltageStr
            })
            .ToList();

        sw.Stop();
        _logger.LogInformation("[HistoryData] IMEI={IMEI} Date={Date} Type={Type} Logs={Count} Elapsed={Elapsed}",
            imei, date, type, logs.Count, FormatElapsed(sw.Elapsed));

        return Json(new
        {
            logs,
            totalDistance = Math.Round(result.TotalDistance / 1000.0, 2),
            maxSpeed = Math.Round(result.MaxSpeed, 1),
            avgSpeed = Math.Round(result.AvgSpeed, 1),
            totalCount = logs.Count
        });
    }

    [HttpGet]
    public async Task HistoryDailySummaryStream(string imei, string? endDate, string type = "GPS")
    {
        var memberTbKey = GetMemberTbKey();
        var memberTimezone = GetMemberTimezone();

        var trackers = await _trackerService.GetLiveLocationsAsync(memberTbKey);
        if (!trackers.Any(t => t.IMEICODE == imei))
        {
            Response.StatusCode = 403;
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        var ct = HttpContext.RequestAborted;
        var end = DateTime.TryParse(endDate, out var ed) ? ed.Date : DateTime.Today.AddMinutes(memberTimezone);

        var summaryStream = type == "Combined"
            ? _historyService.StreamCombinedDailySummaryAsync(imei, end, memberTimezone, ct: ct)
            : _historyService.StreamDailySummaryAsync(imei, end, memberTimezone, ct: ct);

        try
        {
            await foreach (var s in summaryStream.WithCancellation(ct))
            {
                var row = FormatDailySummaryRow(s);
                await Response.WriteAsync($"data: {JsonSerializer.Serialize(row)}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
            await Response.WriteAsync("data: {\"done\":true}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (OperationCanceledException) { }
    }

    [HttpGet]
    public async Task<IActionResult> HistoryDailySummary(string imei, string? endDate)
    {
        var sw = Stopwatch.StartNew();
        var memberTbKey = GetMemberTbKey();
        var memberTimezone = GetMemberTimezone();

        var trackers = await _trackerService.GetLiveLocationsAsync(memberTbKey);
        if (!trackers.Any(t => t.IMEICODE == imei))
            return Forbid();

        var end = DateTime.TryParse(endDate, out var ed) ? ed.Date : DateTime.Today.AddMinutes(memberTimezone);
        var summaries = await _historyService.GetDailySummaryAsync(imei, end, memberTimezone);

        var result = summaries.Select(FormatDailySummaryRow).ToList();

        sw.Stop();
        _logger.LogInformation("[HistoryDailySummary] IMEI={IMEI} EndDate={EndDate} Days={Days} Elapsed={Elapsed}",
            imei, endDate, result.Count, FormatElapsed(sw.Elapsed));

        return Json(result);
    }

    private object FormatDailySummaryRow(DailyHistorySummary s)
    {
        var dayMidnight = s.Date.ToString("yyyy/MM/dd 00:00:00");
        TimeSpan? dur = s.FirstGPS.HasValue && s.LastGPS.HasValue ? s.LastGPS.Value - s.FirstGPS.Value : null;
        var d = dur ?? TimeSpan.Zero;
        return new
        {
            date = s.Date.ToString("yyyy-MM-dd"),
            firstGPS = s.RecordCount > 0 && s.FirstGPS.HasValue
                ? s.FirstGPS.Value.ToString("yyyy/MM/dd HH:mm:ss")
                : dayMidnight,
            lastGPS = s.RecordCount > 0 && s.LastGPS.HasValue
                ? s.LastGPS.Value.ToString("yyyy/MM/dd HH:mm:ss")
                : dayMidnight,
            duration = s.RecordCount > 0
                ? $"{(int)d.TotalDays}days / {d.Hours:D2}:{d.Minutes:D2}:{d.Seconds:D2}"
                : "0days / 00:00:00",
            recordCount = s.RecordCount,
            totalKm = s.TotalDistanceKm > 0
                ? string.Format(_L["Live_Unit_KmValue"].Value, s.TotalDistanceKm.ToString("F2"))
                : _L["Map_Hist_ZeroKm"].Value
        };
    }
}
