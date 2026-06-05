using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Runtime.CompilerServices;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Models;
using TrackerPlus.Repository.Infrastructure;
using TrackerPlus.Repository.Mapping;

namespace TrackerPlus.Repository.Repositories;

public class HistoryRepository : IHistoryRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<HistoryRepository> _logger;

    public HistoryRepository(IDbConnectionFactory db, ILogger<HistoryRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GPS history: TLG_YYYYMMDD tables in LogConnection DB
    public async Task<TrackingHistoryResult> GetGPSHistoryAsync(string imei, DateTime startUtc, DateTime endUtc, int pageIndex, int pageSize)
    {
        var offset = (pageIndex - 1) * pageSize;
        var param = new { IMEI = imei, Start = startUtc, End = endUtc, Offset = offset, PageSize = pageSize };

        using var conn = _db.CreateLogConnection();
        var existingTables = await GetExistingGpsTables(conn, startUtc, endUtc);

        if (existingTables.Count == 0)
            return EmptyResult(pageIndex, pageSize);

        var unionSql = BuildGpsUnionSql(existingTables);
        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM ({unionSql}) AS t", param);

        if (total == 0)
            return EmptyResult(pageIndex, pageSize);

        var rawLogs = (await conn.QueryAsync<TrackingLogDbRow>(
            $"SELECT * FROM ({unionSql}) AS t ORDER BY UTCTime ASC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
            param)).ToList();
        var logs = rawLogs.Select(TrackingLogMapper.ToModel).ToList();

        var statsRows = await conn.QueryAsync<TrackingLogDbRow>(
            $"SELECT * FROM ({unionSql}) AS t", param);
        var mapped = statsRows.Select(TrackingLogMapper.ToModel).ToList();

        return new TrackingHistoryResult
        {
            TotalCount = total,
            PageIndex = pageIndex,
            PageSize = pageSize,
            Logs = logs,
            TotalDistance = mapped.Sum(l => l.Distance),
            MaxSpeed = mapped.Count > 0 ? mapped.Max(l => l.Speed) : 0,
            AvgSpeed = mapped.Count > 0 ? mapped.Average(l => l.Speed) : 0
        };
    }

    public async Task<TrackingHistoryResult> GetLBSHistoryAsync(string imei, DateTime startUtc, DateTime endUtc, int pageIndex, int pageSize)
    {
        var offset = (pageIndex - 1) * pageSize;
        using var conn = _db.CreateLogConnection();

        // LBS tables follow same naming: TLG_YYYYMMDD, filter by GPSStatus='L' or type
        var existingTables = await GetExistingGpsTables(conn, startUtc, endUtc);

        if (existingTables.Count == 0)
            return EmptyResult(pageIndex, pageSize);

        // LBS data stored in same TLG tables with GPSStatus='L' or LAC/CID populated
        var lbsUnionParts = existingTables.Select(t =>
            $"SELECT RTRIM(IMEICode) AS IMEICode, GPSDateTime AS UTCTime, Lat, LatPos, Lng, LngPos, " +
            $"Speed, Direction, Distance, OtherStatus, QTY_GPS, 'L' AS Type " +
            $"FROM {t} WHERE RTRIM(IMEICode)=@IMEI AND GPSDateTime >= @Start AND GPSDateTime <= @End " +
            $"AND GPSStatus='L'");

        var unionSql = string.Join(" UNION ALL ", lbsUnionParts);
        var param = new { IMEI = imei, Start = startUtc, End = endUtc, Offset = offset, PageSize = pageSize };

        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM ({unionSql}) AS t", param);
        if (total == 0)
            return EmptyResult(pageIndex, pageSize);

        var rawLogs = (await conn.QueryAsync<TrackingLogDbRow>(
            $"SELECT * FROM ({unionSql}) AS t ORDER BY UTCTime ASC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
            param)).ToList();

        return new TrackingHistoryResult
        {
            TotalCount = total,
            PageIndex = pageIndex,
            PageSize = pageSize,
            Logs = rawLogs.Select(TrackingLogMapper.ToModel).ToList()
        };
    }

    public async Task<TrackingHistoryResult> GetWifiHistoryAsync(string imei, DateTime startUtc, DateTime endUtc, int pageIndex, int pageSize)
    {
        // Try WifiLog table first; fall back to empty if not available
        using var conn = _db.CreateLogConnection();
        var hasWifi = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.tables WHERE name = 'WifiLog'") > 0;

        if (!hasWifi)
            return EmptyResult(pageIndex, pageSize);

        var offset = (pageIndex - 1) * pageSize;
        var param = new { IMEI = imei, Start = startUtc, End = endUtc, Offset = offset, PageSize = pageSize };

        const string countSql = "SELECT COUNT(*) FROM WifiLog WHERE IMEICODE=@IMEI AND UTCTime >= @Start AND UTCTime <= @End";
        const string dataSql = @"SELECT TbKey, IMEICODE, UTCTime, Lat, Lng,
            0.0 AS Speed, 0 AS Direction, 0.0 AS Distance, 0 AS Voltage,
            0 AS CSQ, 0 AS GPSNo, 0 AS GPSSignal, 0 AS GlonassNo, '' AS Address, 'W' AS Type
            FROM WifiLog WHERE IMEICODE=@IMEI AND UTCTime >= @Start AND UTCTime <= @End
            ORDER BY UTCTime ASC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var total = await conn.ExecuteScalarAsync<int>(countSql, param);
        var logs = (await conn.QueryAsync<TrackingLog>(dataSql, param)).ToList();

        return new TrackingHistoryResult
        {
            TotalCount = total,
            PageIndex = pageIndex,
            PageSize = pageSize,
            Logs = logs
        };
    }

    public async Task<IEnumerable<TrackingLog>> GetGPSLogsForExportAsync(string imei, DateTime startUtc, DateTime endUtc)
    {
        using var conn = _db.CreateLogConnection();
        var tables = await GetExistingGpsTables(conn, startUtc, endUtc);
        if (tables.Count == 0) return Enumerable.Empty<TrackingLog>();

        var unionSql = BuildGpsUnionSql(tables);
        var param = new { IMEI = imei, Start = startUtc, End = endUtc };
        var rows = await conn.QueryAsync<TrackingLogDbRow>(
            $"SELECT * FROM ({unionSql}) AS t ORDER BY UTCTime ASC", param);
        return rows.Select(TrackingLogMapper.ToModel);
    }

    public async Task<IEnumerable<LBSLog>> GetLBSLogsForExportAsync(string imei, DateTime startUtc, DateTime endUtc)
    {
        const string sql = @"SELECT * FROM LBSLog WHERE IMEICODE=@IMEI AND UTCTime >= @Start AND UTCTime <= @End ORDER BY UTCTime ASC";
        using var conn = _db.CreateLogConnection();
        return await conn.QueryAsync<LBSLog>(sql, new { IMEI = imei, Start = startUtc, End = endUtc });
    }

    public async Task<bool> DeleteHistoryAsync(string imei, DateTime startUtc, DateTime endUtc)
    {
        _logger.LogWarning("刪除歷史記錄 IMEI={IMEI} {Start}~{End}", imei, startUtc, endUtc);
        using var conn = _db.CreateLogConnection();
        var tables = await GetExistingGpsTables(conn, startUtc, endUtc);
        int affected = 0;
        foreach (var t in tables)
        {
            affected += await conn.ExecuteAsync(
                $"DELETE FROM {t} WHERE RTRIM(IMEICode)=@IMEI AND GPSDateTime >= @Start AND GPSDateTime <= @End",
                new { IMEI = imei.Trim(), Start = startUtc, End = endUtc });
        }
        return affected >= 0;
    }

    public async Task<TrackingLog?> GetLatestPositionAsync(string imei)
    {
        // Check most recent tables (last 7 days)
        using var conn = _db.CreateLogConnection();
        var tables = await GetExistingGpsTables(conn, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
        if (tables.Count == 0) return null;

        // Search from most recent table backward
        foreach (var t in ((IEnumerable<string>)tables).Reverse())
        {
            var row = await conn.QuerySingleOrDefaultAsync<TrackingLogDbRow>(
                $"SELECT TOP 1 {GpsSelectColumns()} FROM {t} WHERE RTRIM(IMEICode)=@IMEI AND (GPSStatus='A' OR GPSStatus='V') ORDER BY GPSDateTime DESC",
                new { IMEI = imei.Trim() });
            if (row != null) return TrackingLogMapper.ToModel(row);
        }
        return null;
    }

    public async Task<IEnumerable<AlertLog>> GetAlertLogsAsync(string imei, DateTime startUtc, DateTime endUtc)
    {
        const string sql = @"SELECT * FROM AlertLog WHERE IMEICODE=@IMEI AND UTCTime >= @Start AND UTCTime <= @End ORDER BY UTCTime DESC";
        using var conn = _db.CreateLogConnection();
        try
        {
            return await conn.QueryAsync<AlertLog>(sql, new { IMEI = imei, Start = startUtc, End = endUtc });
        }
        catch
        {
            return Enumerable.Empty<AlertLog>();
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static IEnumerable<string> GetTableNamesForRange(DateTime startUtc, DateTime endUtc)
    {
        for (var d = startUtc.Date; d <= endUtc.Date; d = d.AddDays(1))
            yield return $"TLG_{d:yyyyMMdd}";
    }

    private static async Task<List<string>> GetExistingGpsTables(SqlConnection conn, DateTime startUtc, DateTime endUtc)
    {
        var candidates = GetTableNamesForRange(startUtc, endUtc).ToList();
        if (candidates.Count == 0) return new List<string>();

        var dp = new DynamicParameters();
        var inParams = candidates.Select((name, i) => { dp.Add($"p{i}", name); return $"@p{i}"; }).ToList();
        var inClause = string.Join(",", inParams);

        var existing = (await conn.QueryAsync<string>(
            $"SELECT name FROM sys.tables WHERE name IN ({inClause})", dp)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return candidates.Where(n => existing.Contains(n)).ToList();
    }

    private static string GpsSelectColumns() =>
        "RTRIM(IMEICode) AS IMEICode, GPSDateTime AS UTCTime, Lat, LatPos, Lng, LngPos, " +
        "Speed, Direction, Distance, OtherStatus, QTY_GPS, SatelliteStatus, ISNULL(CAST(HDOP AS NVARCHAR(20)),'0') AS HDOP, 'G' AS Type";

    private static string BuildGpsUnionSql(IEnumerable<string> tables) =>
        string.Join(" UNION ALL ", tables.Select(t =>
            $"SELECT {GpsSelectColumns()} FROM {t} " +
            $"WHERE RTRIM(IMEICode)=@IMEI AND GPSDateTime >= @Start AND GPSDateTime <= @End AND GPSStatus='A'"));

    public async Task<IEnumerable<DailyHistorySummary>> GetDailySummaryAsync(string imei, DateTime startUtc, DateTime endUtc, int timezoneMinutes)
    {
        using var conn = _db.CreateLogConnection();
        var tables = await GetExistingGpsTables(conn, startUtc, endUtc);

        if (tables.Count == 0)
            return Enumerable.Empty<DailyHistorySummary>();

        // Group by local date so Taiwan/Australia users see the correct day boundary
        var unionParts = tables.Select(t =>
            $"SELECT CAST(DATEADD(MINUTE, @TZOffset, GPSDateTime) AS DATE) AS [Date], " +
            $"COUNT(*) AS RecordCount, " +
            $"ISNULL(SUM(ISNULL(TRY_CAST(Distance AS FLOAT),0)),0) AS TotalDistanceM, " +
            $"MIN(GPSDateTime) AS FirstGPS, MAX(GPSDateTime) AS LastGPS " +
            $"FROM {t} WHERE RTRIM(IMEICode)=@IMEI AND GPSDateTime >= @Start AND GPSDateTime <= @End AND GPSStatus='A' " +
            $"GROUP BY CAST(DATEADD(MINUTE, @TZOffset, GPSDateTime) AS DATE)");

        var sql = $@"SELECT [Date], SUM(RecordCount) AS RecordCount,
            SUM(TotalDistanceM) AS TotalDistanceM,
            MIN(FirstGPS) AS FirstGPS, MAX(LastGPS) AS LastGPS
            FROM ({string.Join(" UNION ALL ", unionParts)}) AS t
            GROUP BY [Date]
            ORDER BY [Date] DESC";

        var rows = await conn.QueryAsync(sql, new { IMEI = imei, Start = startUtc, End = endUtc, TZOffset = timezoneMinutes });
        return rows.Select(r => new DailyHistorySummary
        {
            Date = (DateTime)r.Date,     // already a local date
            FirstGPS = (DateTime?)r.FirstGPS,  // UTC — service converts
            LastGPS = (DateTime?)r.LastGPS,
            RecordCount = (int)r.RecordCount,
            TotalDistanceKm = Math.Round((double)r.TotalDistanceM / 1000.0, 2)
        });
    }

    public async IAsyncEnumerable<DailyHistorySummary> StreamDailySummaryAsync(
        string imei, DateTime startUtc, DateTime endUtc, int timezoneMinutes,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var offset = TimeSpan.FromMinutes(timezoneMinutes);

        using var conn = _db.CreateLogConnection();
        await conn.OpenAsync(ct);
        var existing = (await GetExistingGpsTables(conn, startUtc, endUtc))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Iterate by local day (newest first) so results match what the user sees on-screen
        var localEnd   = (endUtc   + offset).Date;
        var localStart = (startUtc + offset).Date;

        for (var localDay = localEnd; localDay >= localStart; localDay = localDay.AddDays(-1))
        {
            ct.ThrowIfCancellationRequested();

            // UTC window for this local day
            var dayUtcStart = localDay - offset;                 // local 00:00 → UTC
            var dayUtcEnd   = localDay.AddDays(1) - offset;     // local 00:00 next day → UTC (exclusive)

            // Which UTC tables cover this local day?
            var utcTableEnd = dayUtcEnd.TimeOfDay == TimeSpan.Zero
                ? dayUtcEnd.Date.AddDays(-1)
                : dayUtcEnd.Date;

            var tables = new List<string>();
            for (var t = dayUtcStart.Date; t <= utcTableEnd; t = t.AddDays(1))
            {
                var tn = $"TLG_{t:yyyyMMdd}";
                if (existing.Contains(tn)) tables.Add(tn);
            }
            if (tables.Count == 0)
            {
                yield return new DailyHistorySummary
                {
                    Date = localDay,
                    RecordCount = 0,
                    TotalDistanceKm = 0,
                    FirstGPS = null,
                    LastGPS = null
                };
                continue;
            }

            // Aggregate across all relevant tables for this local day
            var unionParts = tables.Select(t =>
                $"SELECT COUNT(*) AS RecordCount, " +
                $"ISNULL(SUM(ISNULL(TRY_CAST(Distance AS FLOAT),0)),0) AS TotalDistanceM, " +
                $"MIN(GPSDateTime) AS FirstGPS, MAX(GPSDateTime) AS LastGPS " +
                $"FROM {t} " +
                $"WHERE RTRIM(IMEICode)=@IMEI AND GPSDateTime >= @Start AND GPSDateTime < @End AND GPSStatus='A'");

            var aggSql =
                $"SELECT SUM(RecordCount) AS RecordCount, SUM(TotalDistanceM) AS TotalDistanceM, " +
                $"MIN(FirstGPS) AS FirstGPS, MAX(LastGPS) AS LastGPS " +
                $"FROM ({string.Join(" UNION ALL ", unionParts)}) AS t";

            var row = await conn.QuerySingleAsync(aggSql, new { IMEI = imei, Start = dayUtcStart, End = dayUtcEnd });

            var count = (int)row.RecordCount;
            yield return new DailyHistorySummary
            {
                Date = localDay,   // local date — correct for any timezone
                RecordCount = count,
                TotalDistanceKm = count > 0 ? Math.Round(Convert.ToDouble(row.TotalDistanceM) / 1000.0, 2) : 0,
                FirstGPS = count > 0 && row.FirstGPS != null ? (DateTime?)row.FirstGPS : null,  // UTC — service converts
                LastGPS  = count > 0 && row.LastGPS  != null ? (DateTime?)row.LastGPS  : null
            };
        }
    }

    public async IAsyncEnumerable<TrackingLog> StreamGPSHistoryAsync(
        string imei, DateTime startUtc, DateTime endUtc,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var conn = _db.CreateLogConnection();
        await conn.OpenAsync(ct);
        var tables = await GetExistingGpsTables(conn, startUtc, endUtc);
        if (tables.Count == 0) yield break;

        var sql = $"SELECT * FROM ({BuildGpsUnionSql(tables)}) AS t ORDER BY UTCTime ASC";
        var param = new { IMEI = imei, Start = startUtc, End = endUtc };

        await foreach (var row in conn.QueryUnbufferedAsync<TrackingLogDbRow>(sql, param).WithCancellation(ct))
            yield return TrackingLogMapper.ToModel(row);
    }

    public async IAsyncEnumerable<TrackingLog> StreamLBSHistoryAsync(
        string imei, DateTime startUtc, DateTime endUtc,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var conn = _db.CreateLogConnection();
        await conn.OpenAsync(ct);
        var tables = await GetExistingGpsTables(conn, startUtc, endUtc);
        if (tables.Count == 0) yield break;

        var lbsUnion = string.Join(" UNION ALL ", tables.Select(t =>
            $"SELECT RTRIM(IMEICode) AS IMEICode, GPSDateTime AS UTCTime, Lat, LatPos, Lng, LngPos, " +
            $"Speed, Direction, Distance, OtherStatus, QTY_GPS, NULL AS SatelliteStatus, '0' AS HDOP, 'L' AS Type " +
            $"FROM {t} WHERE RTRIM(IMEICode)=@IMEI AND GPSDateTime >= @Start AND GPSDateTime <= @End AND GPSStatus='L'"));

        var sql = $"SELECT * FROM ({lbsUnion}) AS t ORDER BY UTCTime ASC";
        var param = new { IMEI = imei, Start = startUtc, End = endUtc };

        await foreach (var row in conn.QueryUnbufferedAsync<TrackingLogDbRow>(sql, param).WithCancellation(ct))
            yield return TrackingLogMapper.ToModel(row);
    }

    public async IAsyncEnumerable<TrackingLog> StreamCombinedHistoryAsync(
        string imei, DateTime startUtc, DateTime endUtc,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var conn = _db.CreateLogConnection();
        await conn.OpenAsync(ct);
        var tables = await GetExistingGpsTables(conn, startUtc, endUtc);
        if (tables.Count == 0) yield break;

        var combinedUnion = string.Join(" UNION ALL ", tables.Select(t =>
            $"SELECT RTRIM(IMEICode) AS IMEICode, GPSDateTime AS UTCTime, Lat, LatPos, Lng, LngPos, " +
            $"Speed, Direction, Distance, OtherStatus, QTY_GPS, " +
            $"CASE WHEN GPSStatus='A' THEN SatelliteStatus ELSE NULL END AS SatelliteStatus, " +
            $"CASE WHEN GPSStatus='A' THEN ISNULL(CAST(HDOP AS NVARCHAR(20)),'0') ELSE '0' END AS HDOP, " +
            $"CASE WHEN GPSStatus='A' THEN 'G' ELSE 'L' END AS Type " +
            $"FROM {t} WHERE RTRIM(IMEICode)=@IMEI AND GPSDateTime >= @Start AND GPSDateTime <= @End " +
            $"AND GPSStatus IN ('A','V','L')"));

        var sql = $"SELECT * FROM ({combinedUnion}) AS t ORDER BY UTCTime ASC";
        var param = new { IMEI = imei, Start = startUtc, End = endUtc };

        await foreach (var row in conn.QueryUnbufferedAsync<TrackingLogDbRow>(sql, param).WithCancellation(ct))
            yield return TrackingLogMapper.ToModel(row);
    }

    public async IAsyncEnumerable<DailyHistorySummary> StreamCombinedDailySummaryAsync(
        string imei, DateTime startUtc, DateTime endUtc, int timezoneMinutes,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var offset = TimeSpan.FromMinutes(timezoneMinutes);

        using var conn = _db.CreateLogConnection();
        await conn.OpenAsync(ct);
        var existing = (await GetExistingGpsTables(conn, startUtc, endUtc))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var localEnd   = (endUtc   + offset).Date;
        var localStart = (startUtc + offset).Date;

        for (var localDay = localEnd; localDay >= localStart; localDay = localDay.AddDays(-1))
        {
            ct.ThrowIfCancellationRequested();

            var dayUtcStart = localDay - offset;
            var dayUtcEnd   = localDay.AddDays(1) - offset;

            var utcTableEnd = dayUtcEnd.TimeOfDay == TimeSpan.Zero
                ? dayUtcEnd.Date.AddDays(-1)
                : dayUtcEnd.Date;

            var tables = new List<string>();
            for (var t = dayUtcStart.Date; t <= utcTableEnd; t = t.AddDays(1))
            {
                var tn = $"TLG_{t:yyyyMMdd}";
                if (existing.Contains(tn)) tables.Add(tn);
            }

            if (tables.Count == 0)
            {
                yield return new DailyHistorySummary { Date = localDay, RecordCount = 0, TotalDistanceKm = 0 };
                continue;
            }

            var unionParts = tables.Select(t =>
                $"SELECT COUNT(*) AS RecordCount, " +
                $"ISNULL(SUM(ISNULL(TRY_CAST(Distance AS FLOAT),0)),0) AS TotalDistanceM, " +
                $"MIN(GPSDateTime) AS FirstGPS, MAX(GPSDateTime) AS LastGPS " +
                $"FROM {t} " +
                $"WHERE RTRIM(IMEICode)=@IMEI AND GPSDateTime >= @Start AND GPSDateTime < @End " +
                $"AND GPSStatus IN ('A','V','L')");

            var aggSql =
                $"SELECT SUM(RecordCount) AS RecordCount, SUM(TotalDistanceM) AS TotalDistanceM, " +
                $"MIN(FirstGPS) AS FirstGPS, MAX(LastGPS) AS LastGPS " +
                $"FROM ({string.Join(" UNION ALL ", unionParts)}) AS t";

            var row = await conn.QuerySingleAsync(aggSql, new { IMEI = imei, Start = dayUtcStart, End = dayUtcEnd });

            var count = (int)row.RecordCount;
            yield return new DailyHistorySummary
            {
                Date = localDay,
                RecordCount = count,
                TotalDistanceKm = count > 0 ? Math.Round(Convert.ToDouble(row.TotalDistanceM) / 1000.0, 2) : 0,
                FirstGPS = count > 0 && row.FirstGPS != null ? (DateTime?)row.FirstGPS : null,
                LastGPS  = count > 0 && row.LastGPS  != null ? (DateTime?)row.LastGPS  : null
            };
        }
    }

    public async Task<bool> QueueMoveHistoryAsync(string sourceImei, string destImei)
    {
        _logger.LogInformation("排入歷史軌跡轉移佇列 {Src} -> {Dest}", sourceImei, destImei);
        const string sql = "INSERT INTO dbo.MoveHistoryData(sourceimeicode, destinationimeicode) VALUES(@Src, @Dest)";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { Src = sourceImei.Trim(), Dest = destImei.Trim() }) > 0;
    }

    private static TrackingHistoryResult EmptyResult(int pageIndex, int pageSize) =>
        new() { PageIndex = pageIndex, PageSize = pageSize };
}
