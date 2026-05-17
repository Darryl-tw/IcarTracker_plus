using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Services.Services;

public class HistoryService : IHistoryService
{
    private readonly IHistoryRepository _historyRepo;
    private readonly ILogger<HistoryService> _logger;

    public HistoryService(IHistoryRepository historyRepo, ILogger<HistoryService> logger)
    {
        _historyRepo = historyRepo;
        _logger = logger;
    }

    public async Task<TrackingHistoryResult> GetGPSHistoryAsync(string imei, DateTime localStart, DateTime localEnd, int memberTimezone, int pageIndex, int pageSize)
    {
        var (utcStart, utcEnd) = ToUtc(localStart, localEnd, memberTimezone);
        return await _historyRepo.GetGPSHistoryAsync(imei, utcStart, utcEnd, pageIndex, pageSize);
    }

    public async Task<TrackingHistoryResult> GetLBSHistoryAsync(string imei, DateTime localStart, DateTime localEnd, int memberTimezone, int pageIndex, int pageSize)
    {
        var (utcStart, utcEnd) = ToUtc(localStart, localEnd, memberTimezone);
        return await _historyRepo.GetLBSHistoryAsync(imei, utcStart, utcEnd, pageIndex, pageSize);
    }

    public async Task<TrackingHistoryResult> GetWifiHistoryAsync(string imei, DateTime localStart, DateTime localEnd, int memberTimezone, int pageIndex, int pageSize)
    {
        var (utcStart, utcEnd) = ToUtc(localStart, localEnd, memberTimezone);
        return await _historyRepo.GetWifiHistoryAsync(imei, utcStart, utcEnd, pageIndex, pageSize);
    }

    public async Task<byte[]> ExportToExcelAsync(string imei, DateTime localStart, DateTime localEnd, int memberTimezone, string type = "GPS")
    {
        var (utcStart, utcEnd) = ToUtc(localStart, localEnd, memberTimezone);
        var logs = (await _historyRepo.GetGPSLogsForExportAsync(imei, utcStart, utcEnd))
            .Where(l => Math.Abs(l.Lat) > 0.0001 && Math.Abs(l.Lng) > 0.0001)
            .ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"{type}_{imei}");
        ws.Cell(1, 1).Value = "#";
        ws.Cell(1, 2).Value = "本地時間";
        ws.Cell(1, 3).Value = "緯度";
        ws.Cell(1, 4).Value = "經度";
        ws.Cell(1, 5).Value = "速度(km/h)";
        ws.Cell(1, 6).Value = "方向";
        ws.Cell(1, 7).Value = "距離(km)";

        var row = 2;
        var idx = 1;
        foreach (var log in logs)
        {
            var local = log.UTCTime.AddMinutes(memberTimezone);
            ws.Cell(row, 1).Value = idx++;
            ws.Cell(row, 2).Value = local.ToString("yyyy-MM-dd HH:mm:ss");
            ws.Cell(row, 3).Value = log.Lat;
            ws.Cell(row, 4).Value = log.Lng;
            ws.Cell(row, 5).Value = log.Speed;
            ws.Cell(row, 6).Value = log.Direction;
            ws.Cell(row, 7).Value = log.Distance;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<string> ExportToGPXAsync(string imei, DateTime localStart, DateTime localEnd, int memberTimezone)
    {
        var (utcStart, utcEnd) = ToUtc(localStart, localEnd, memberTimezone);
        var logs = (await _historyRepo.GetGPSLogsForExportAsync(imei, utcStart, utcEnd))
            .Where(l => Math.Abs(l.Lat) > 0.0001 && Math.Abs(l.Lng) > 0.0001)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        sb.AppendLine(@"<gpx version=""1.1"" creator=""TrackerPlus"" xmlns=""http://www.topografix.com/GPX/1/1"">");
        sb.AppendLine("  <trk>");
        sb.AppendLine($"    <name>{imei}</name>");
        sb.AppendLine("    <trkseg>");

        foreach (var log in logs)
        {
            var local = log.UTCTime.AddMinutes(memberTimezone);
            sb.AppendLine($"      <trkpt lat=\"{log.Lat.ToString(CultureInfo.InvariantCulture)}\" lon=\"{log.Lng.ToString(CultureInfo.InvariantCulture)}\">");
            sb.AppendLine($"        <time>{log.UTCTime:yyyy-MM-ddTHH:mm:ssZ}</time>");
            sb.AppendLine($"        <desc>{local:yyyy-MM-dd HH:mm:ss} speed={log.Speed:F0}km/h</desc>");
            sb.AppendLine("      </trkpt>");
        }

        sb.AppendLine("    </trkseg>");
        sb.AppendLine("  </trk>");
        sb.AppendLine("</gpx>");
        return sb.ToString();
    }

    public async Task<string> ExportToKMLAsync(string imei, DateTime localStart, DateTime localEnd, int memberTimezone)
    {
        var (utcStart, utcEnd) = ToUtc(localStart, localEnd, memberTimezone);
        var logs = (await _historyRepo.GetGPSLogsForExportAsync(imei, utcStart, utcEnd))
            .Where(l => Math.Abs(l.Lat) > 0.0001 && Math.Abs(l.Lng) > 0.0001)
            .ToList();

        var coords = string.Join(" ",
            logs.Select(l => $"{l.Lng.ToString(CultureInfo.InvariantCulture)},{l.Lat.ToString(CultureInfo.InvariantCulture)},0"));

        var doc = new XDocument(
            new XElement(XName.Get("kml", "http://www.opengis.net/kml/2.2"),
                new XElement("Document",
                    new XElement("name", imei),
                    new XElement("Placemark",
                        new XElement("name", "軌跡"),
                        new XElement("LineString",
                            new XElement("tessellate", "1"),
                            new XElement("coordinates", coords))))));

        return doc.Declaration == null
            ? doc.ToString()
            : doc.Declaration + Environment.NewLine + doc;
    }

    public async Task<OperationResult> DeleteHistoryAsync(string imei, DateTime localStart, DateTime localEnd, int memberTimezone)
    {
        try
        {
            var (utcStart, utcEnd) = ToUtc(localStart, localEnd, memberTimezone);
            await _historyRepo.DeleteHistoryAsync(imei, utcStart, utcEnd);
            return OperationResult.Ok("刪除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除歷史記錄失敗 IMEI={IMEI}", imei);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<IEnumerable<AlertLog>> GetAlertHistoryAsync(string imei, DateTime localStart, DateTime localEnd, int memberTimezone)
    {
        var (utcStart, utcEnd) = ToUtc(localStart, localEnd, memberTimezone);
        return await _historyRepo.GetAlertLogsAsync(imei, utcStart, utcEnd);
    }

    public async Task<IEnumerable<DailyHistorySummary>> GetDailySummaryAsync(string imei, DateTime localEnd, int memberTimezone, int days = 7)
    {
        var offset = TimeSpan.FromMinutes(memberTimezone);
        var utcEnd = localEnd.Date.AddDays(1).AddSeconds(-1) - offset;
        var utcStart = localEnd.Date.AddDays(-(days - 1)) - offset;
        var rows = await _historyRepo.GetDailySummaryAsync(imei, utcStart, utcEnd);
        return rows.Select(r => new DailyHistorySummary
        {
            Date = r.Date,
            FirstGPS = r.FirstGPS.HasValue ? r.FirstGPS.Value + offset : null,
            LastGPS = r.LastGPS.HasValue ? r.LastGPS.Value + offset : null,
            RecordCount = r.RecordCount,
            TotalDistanceKm = r.TotalDistanceKm
        });
    }

    private static (DateTime utcStart, DateTime utcEnd) ToUtc(DateTime localStart, DateTime localEnd, int timezoneMinutes)
    {
        var offset = TimeSpan.FromMinutes(timezoneMinutes);
        return (localStart - offset, localEnd - offset);
    }
}
