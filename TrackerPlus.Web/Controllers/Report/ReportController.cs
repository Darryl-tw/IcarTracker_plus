using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;
using TrackerPlus.Web.Resources;

namespace TrackerPlus.Web.Controllers.Report;

[Authorize]
[Route("Report/[action]")]
public class ReportController : Controller
{
    private readonly IHistoryService _history;
    private readonly IStringLocalizer<SharedResources> _L;

    public ReportController(IHistoryService history, IStringLocalizer<SharedResources> localizer)
    {
        _history = history;
        _L = localizer;
    }

    private int MemberTimezone => int.TryParse(User.FindFirstValue("Timezoom"), out var tz) ? tz : 480;
    private string Today => DateTime.Today.ToString("yyyy-MM-dd");

    // ── 行蹤紀錄 ─────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Travel() { ViewBag.Today = Today; return View(); }

    [HttpGet]
    public async Task<IActionResult> DownloadTravel(
        string imei, string date,
        string gpsOnly = "all",
        string speedUnit = "km",
        string speedFilter = "all",
        double speedMin = 0, double speedMax = 999,
        string format = "csv")
    {
        if (string.IsNullOrWhiteSpace(imei)) return Content(_L["Report_Error_NoImei"]);

        var tz = MemberTimezone;
        var localDate = ParseDate(date);
        var logs = await CollectLogs(imei, localDate, localDate.AddDays(1).AddSeconds(-1), tz);

        if (gpsOnly == "gps") logs = logs.Where(l => l.GPSNo > 0).ToList();

        bool useKm = speedUnit != "mi";
        logs = speedFilter switch
        {
            "idle"  => logs.Where(l => l.Speed < 1).ToList(),
            "range" => logs.Where(l =>
            {
                var s = useKm ? l.Speed : l.Speed / 1.60934;
                return s >= speedMin && s <= speedMax;
            }).ToList(),
            _ => logs
        };

        var speedCol = useKm ? _L["Report_Col_SpeedKmh"].Value : _L["Report_Col_SpeedMph"].Value;
        var header = $"{_L["Report_Col_Time"]},{_L["Report_Col_Lat"]},{_L["Report_Col_Lng"]},{speedCol},{_L["Report_Col_Direction"]},{_L["Report_Col_DistKm"]},{_L["Report_Col_Satellites"]},{_L["Report_Col_Voltage"]}";
        var rows = logs.Select(l =>
        {
            var t = l.UTCTime.AddMinutes(tz);
            var spd = useKm ? l.Speed : l.Speed / 1.60934;
            return $"{t:yyyy-MM-dd HH:mm:ss},{l.Lat:F6},{l.Lng:F6},{spd:F1},{l.Direction},{l.Distance:F3},{l.GPSNo},{l.VoltageStr}";
        });

        return BuildFile(header, rows, $"{_L["Report_Travel"]}_{date}", format);
    }

    // ── 超速偵測 ─────────────────────────────────────────────────
    [HttpGet]
    public IActionResult SpeedDetection() { ViewBag.Today = Today; return View(); }

    [HttpGet]
    public async Task<IActionResult> DownloadSpeedDetection(
        string imei, string startDate, string endDate,
        string speedUnit = "km",
        string format = "csv")
    {
        if (string.IsNullOrWhiteSpace(imei)) return Content(_L["Report_Error_NoImei"]);

        var tz = MemberTimezone;
        var logs = await CollectLogs(imei, ParseDateTime(startDate), ParseDateTime(endDate), tz);
        logs = logs.Where(l => l.Speed > 0).ToList();

        bool useKm = speedUnit != "mi";
        var speedCol = useKm ? _L["Report_Col_SpeedKmh"].Value : _L["Report_Col_SpeedMph"].Value;
        var header = $"{_L["Report_Col_Time"]},{_L["Report_Col_Lat"]},{_L["Report_Col_Lng"]},{speedCol},{_L["Report_Col_Direction"]},{_L["Report_Col_Satellites"]}";
        var rows = logs.Select(l =>
        {
            var t = l.UTCTime.AddMinutes(tz);
            var spd = useKm ? l.Speed : l.Speed / 1.60934;
            return $"{t:yyyy-MM-dd HH:mm:ss},{l.Lat:F6},{l.Lng:F6},{spd:F1},{l.Direction},{l.GPSNo}";
        });

        var safeStart = startDate.Length >= 10 ? startDate[..10] : startDate;
        return BuildFile(header, rows, $"{_L["Report_SpeedDetection"]}_{safeStart}", format);
    }

    // ── 怠速紀錄 ─────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Idle() { ViewBag.Today = Today; return View(); }

    [HttpGet]
    public async Task<IActionResult> DownloadIdle(
        string imei, string startDate, string endDate,
        string format = "csv")
    {
        if (string.IsNullOrWhiteSpace(imei)) return Content(_L["Report_Error_NoImei"]);

        var tz = MemberTimezone;
        var logs = await CollectLogs(imei, ParseDateTime(startDate), ParseDateTime(endDate), tz);
        var periods = FindIdlePeriods(logs, tz, idleKmh: 5, minMinutes: 3);

        var header = $"{_L["Report_Col_StartTime"]},{_L["Report_Col_EndTime"]},{_L["Report_Col_Duration"]},{_L["Report_Col_Lat"]},{_L["Report_Col_Lng"]}";
        var rows = periods.Select(p =>
            $"{p.Start:yyyy-MM-dd HH:mm:ss},{p.End:yyyy-MM-dd HH:mm:ss},{p.MinDuration:F1},{p.Lat:F6},{p.Lng:F6}");

        var safeStart = startDate.Length >= 10 ? startDate[..10] : startDate;
        return BuildFile(header, rows, $"{_L["Report_Idle"]}_{safeStart}", format);
    }

    // ── 里程紀錄 ─────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Mileage() { ViewBag.Today = Today; return View(); }

    [HttpGet]
    public async Task<IActionResult> DownloadMileage(
        string imei, string date,
        string gpsFilter = "all",
        string format = "csv")
    {
        if (string.IsNullOrWhiteSpace(imei)) return Content(_L["Report_Error_NoImei"]);

        var tz = MemberTimezone;
        var localDate = ParseDate(date);
        var monthStart = new DateTime(localDate.Year, localDate.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddSeconds(-1);

        var logs = await CollectLogs(imei, monthStart, monthEnd, tz);
        if (gpsFilter == "gps")      logs = logs.Where(l => l.GPSNo > 0).ToList();
        else if (gpsFilter == "nogps") logs = logs.Where(l => l.GPSNo == 0).ToList();

        var daily = logs
            .GroupBy(l => l.UTCTime.AddMinutes(tz).Date)
            .OrderBy(g => g.Key)
            .Select(g => (
                Date: g.Key,
                Count: g.Count(),
                DistKm: g.Sum(l => l.Distance),
                First: g.Min(l => l.UTCTime).AddMinutes(tz),
                Last: g.Max(l => l.UTCTime).AddMinutes(tz)
            ));

        var header = $"{_L["Report_Col_Date"]},{_L["Report_Col_InitTime"]},{_L["Report_Col_EndTime"]},{_L["Report_Col_MileageKm"]},{_L["Report_Col_RecordCount"]}";
        var rows = daily.Select(d =>
            $"{d.Date:yyyy-MM-dd},{d.First:HH:mm:ss},{d.Last:HH:mm:ss},{d.DistKm:F3},{d.Count}");

        return BuildFile(header, rows, $"{_L["Report_Mileage"]}_{localDate:yyyy-MM}", format);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private async Task<List<TrackingLog>> CollectLogs(string imei, DateTime localStart, DateTime localEnd, int tz)
    {
        var list = new List<TrackingLog>();
        await foreach (var log in _history.StreamGPSHistoryAsync(imei, localStart, localEnd, tz))
            list.Add(log);
        return list;
    }

    private record IdlePeriod(DateTime Start, DateTime End, double MinDuration, double Lat, double Lng);

    private static List<IdlePeriod> FindIdlePeriods(List<TrackingLog> logs, int tz, double idleKmh, double minMinutes)
    {
        var result = new List<IdlePeriod>();
        TrackingLog? start = null, last = null;
        foreach (var log in logs)
        {
            if (log.Speed < idleKmh) { start ??= log; last = log; }
            else { Flush(ref start, ref last, tz, minMinutes, result); }
        }
        Flush(ref start, ref last, tz, minMinutes, result);
        return result;

        static void Flush(ref TrackingLog? s, ref TrackingLog? l, int tz, double minMin, List<IdlePeriod> res)
        {
            if (s != null && l != null)
            {
                var dur = (l.UTCTime - s.UTCTime).TotalMinutes;
                if (dur >= minMin)
                    res.Add(new IdlePeriod(s.UTCTime.AddMinutes(tz), l.UTCTime.AddMinutes(tz), dur, s.Lat, s.Lng));
            }
            s = null; l = null;
        }
    }

    private static DateTime ParseDate(string s)
        => DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d : DateTime.Today;

    private static DateTime ParseDateTime(string s)
        => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d : DateTime.Today;

    private IActionResult BuildFile(string header, IEnumerable<string> rows, string baseName, string format)
    {
        if (format == "xls") return ExcelHtml(header, rows, baseName + ".xls");
        var csv = header + "\r\n" + string.Join("\r\n", rows);
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        return File(bom.Concat(Encoding.UTF8.GetBytes(csv)).ToArray(), "text/csv", baseName + ".csv");
    }

    private IActionResult ExcelHtml(string header, IEnumerable<string> rows, string filename)
    {
        var sb = new StringBuilder("<html><head><meta charset='UTF-8'></head><body><table border='1'><tr>");
        foreach (var h in header.Split(',')) sb.Append($"<th>{h}</th>");
        sb.Append("</tr>");
        foreach (var r in rows)
        {
            sb.Append("<tr>");
            foreach (var c in r.Split(',')) sb.Append($"<td>{c}</td>");
            sb.Append("</tr>");
        }
        sb.Append("</table></body></html>");
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        return File(bom.Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(), "application/vnd.ms-excel", filename);
    }
}
