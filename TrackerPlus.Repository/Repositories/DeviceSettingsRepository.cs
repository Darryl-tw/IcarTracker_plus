using Dapper;
using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Models;
using TrackerPlus.Repository.Infrastructure;

namespace TrackerPlus.Repository.Repositories;

public class DeviceSettingsRepository : IDeviceSettingsRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<DeviceSettingsRepository> _logger;

    private static readonly string[] DefaultUdFieldNames = ["名稱", "編號", "電話"];

    public DeviceSettingsRepository(IDbConnectionFactory db, ILogger<DeviceSettingsRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DeviceSettingsDto?> GetSettingsAsync(int trackerTbKey, int memberTbKey)
    {
        using var conn = _db.CreateMainConnection();

        const string trackerSql = @"
            SELECT t.tbKey, RTRIM(t.IMEICode) AS Imei, RTRIM(t.CName) AS CName,
                   RTRIM(ISNULL(t.IconFile,'A')) AS IconFile,
                   RTRIM(CAST(ISNULL(t.CMemo, N'') AS NVARCHAR(MAX))) AS Memo,
                   ISNULL(t.GPSReportTime, 30) AS GpsReportTime,
                   ISNULL(t.Shock_Mode, 1) AS ShockMode,
                   ISNULL(t.Shock_Sensitive, 5) AS ShockSensitive,
                   RTRIM(ISNULL(t.PASSWORD,'12345678')) AS SharePassword,
                   ISNULL(CAST(RTRIM(CAST(t.alertmode AS NVARCHAR(10))) AS int), 0) AS AlertMode,
                   t.Member_tbKey AS MemberTbKey
            FROM dbo.Tracker t
            WHERE t.tbKey = @TbKey AND t.Member_tbKey = @MemberTbKey";

        var row = await conn.QuerySingleOrDefaultAsync<TrackerSettingsRow>(trackerSql,
            new { TbKey = trackerTbKey, MemberTbKey = memberTbKey });
        if (row == null) return null;

        await EnsureDefaultUdFieldsAsync(conn, memberTbKey);

        var udFields = (await conn.QueryAsync<UdFieldRow>(@"
            SELECT f.tbKey AS FieldTbKey, RTRIM(f.FName) AS FieldName,
                   RTRIM(ISNULL(v.FValue,'')) AS Value
            FROM dbo.UDFields f
            LEFT JOIN dbo.UDFieldsValue v ON v.UDFields_tbKey = f.tbKey AND v.Tracker_tbKey = @TrackerTbKey
            WHERE f.Member_tbKey = @MemberTbKey
            ORDER BY f.FSort, f.tbKey",
            new { TrackerTbKey = trackerTbKey, MemberTbKey = memberTbKey })).ToList();

        var labels = (await conn.QueryAsync<LabelRow>(@"
            SELECT l.tbKey, RTRIM(l.LabelName) AS Name,
                   CASE WHEN v.Tracker_tbKey IS NULL THEN 0 ELSE 1 END AS Assigned
            FROM dbo.UDLabel l
            LEFT JOIN dbo.UDLabelValue v ON v.UDLabel_tbKey = l.tbKey AND v.Tracker_tbKey = @TrackerTbKey
            WHERE l.Member_tbKey = @MemberTbKey
            ORDER BY l.LabelName",
            new { TrackerTbKey = trackerTbKey, MemberTbKey = memberTbKey })).ToList();

        var alert = await conn.QuerySingleOrDefaultAsync<AlertRow>(@"
            SELECT RTRIM(ISNULL(APMinute,'0')) AS ApMinute, RTRIM(ISNULL(APMsg,'')) AS ApMsg,
                   RTRIM(ISNULL(ASMinute,'0')) AS AsMinute, RTRIM(ISNULL(ASMsg,'')) AS AsMsg
            FROM dbo.AlertModule
            WHERE Tracker_tbKey = @TrackerTbKey",
            new { TrackerTbKey = trackerTbKey });

        var payLogs = (await conn.QueryAsync<PayRow>(@"
            SELECT p.SDate, p.EDate
            FROM dbo.PayLog p
            INNER JOIN dbo.Tracker t ON RTRIM(t.IMEICode) = RTRIM(p.IMEICode)
            WHERE t.tbKey = @TrackerTbKey
            ORDER BY p.CDate DESC",
            new { TrackerTbKey = trackerTbKey })).ToList();

        var sharePwd = (row.SharePassword ?? "12345678").Trim();
        var shareEnabled = sharePwd != "12345678" && sharePwd.Length == 4;

        return new DeviceSettingsDto
        {
            TbKey = row.TbKey,
            Imei = row.Imei,
            CName = row.CName,
            IconFile = NormalizeIcon(row.IconFile),
            Memo = row.Memo,
            UdFields = udFields.Select(f => new DeviceUdFieldDto
            {
                FieldTbKey = f.FieldTbKey,
                FieldName = f.FieldName,
                Value = f.Value
            }).ToList(),
            Labels = labels.Select(l => new DeviceLabelDto
            {
                TbKey = l.TbKey,
                Name = l.Name,
                Assigned = l.Assigned == 1
            }).ToList(),
            GpsReportTime = NormalizeGpsReportTime(row.GpsReportTime),
            SmsPassword = "12345678",
            ShockMode = row.ShockMode <= 0 ? 1 : row.ShockMode,
            ShockSensitive = row.ShockSensitive <= 0 ? 5 : row.ShockSensitive,
            ApMinute = int.TryParse(alert?.ApMinute, out var ap) ? ap : 0,
            ApMsg = alert?.ApMsg ?? row.Imei,
            AsMinute = int.TryParse(alert?.AsMinute, out var asm) ? asm : 0,
            AsMsg = alert?.AsMsg ?? row.Imei,
            ShareEnabled = shareEnabled || (sharePwd.Length == 4 && sharePwd != "12345678"),
            SharePassword = sharePwd.Length == 4 ? sharePwd : "0000",
            AlertMode = row.AlertMode,
            PayLogs = payLogs.Select(p => new DevicePayLogRowDto
            {
                ServiceStart = p.SDate,
                ServiceEnd = p.EDate
            }).ToList()
        };
    }

    public async Task<DeviceDetailDto?> GetDetailAsync(int trackerTbKey, int memberTbKey, int timezoneMinutes)
    {
        using var conn = _db.CreateMainConnection();
        const string sql = @"
            SELECT t.tbKey, RTRIM(t.IMEICode) AS Imei, RTRIM(t.CName) AS CName,
                   t.Lastlogintime, t.LastReportDate, t.LastConnectedDate,
                   t.FC_Lat, t.FC_Lng,
                   ti.Speed, ti.Direction, ti.Lat, ti.LatPos, ti.Lng, ti.LngPos,
                   ti.QTY_GPS, ti.SatelliteStatus, ti.OtherStatus, ti.GPSDateTime
            FROM dbo.Tracker t
            LEFT JOIN dbo.Tracker_Info ti ON RTRIM(t.IMEICode) = RTRIM(ti.IMEICode)
            WHERE t.tbKey = @TbKey AND t.Member_tbKey = @MemberTbKey";

        var row = await conn.QuerySingleOrDefaultAsync<DetailRow>(sql,
            new { TbKey = trackerTbKey, MemberTbKey = memberTbKey });
        if (row == null) return null;

        // 與 TrackerMapper 一致：Lat/Lng 為半球(N/S/E/W)，LatPos/LngPos 為座標值
        var lat = GpsHelper.ToDecimalDegrees(row.Lat, row.LatPos);
        var lng = GpsHelper.ToDecimalDegrees(row.Lng, row.LngPos);
        if (lat == 0 && lng == 0)
        {
            lat = ParseFenceCoord(row.FC_Lat);
            lng = ParseFenceCoord(row.FC_Lng);
        }
        var lastReport = row.LastReportDate ?? row.GPSDateTime;
        var lastConnected = row.LastConnectedDate ?? lastReport;
        var now = DateTime.UtcNow;
        var isOnline = lastReport.HasValue && (now - lastReport.Value).TotalMinutes < 10;

        string? offlineDuration = null;
        if (!isOnline && lastConnected.HasValue)
        {
            var span = now - lastConnected.Value;
            offlineDuration = FormatDuration(span);
        }

        var gpsUsed = GpsHelper.ParseGpsSatelliteCount(row.QTY_GPS);
        var satStatus = (row.SatelliteStatus ?? "").Trim();
        var gpsTotal = 12;
        if (satStatus.Contains('/'))
        {
            var parts = satStatus.Split('/');
            if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out var total))
                gpsTotal = total;
            if (parts.Length >= 1 && int.TryParse(parts[0].Trim(), out var used))
                gpsUsed = used;
        }

        return new DeviceDetailDto
        {
            TbKey = row.TbKey,
            Imei = row.Imei,
            CName = string.IsNullOrWhiteSpace(row.CName) ? row.Imei : row.CName,
            IsOnline = isOnline,
            LastLoginLocal = FormatLocal(row.Lastlogintime, timezoneMinutes),
            LastReportLocal = FormatLocal(lastReport, timezoneMinutes),
            LastConnectedLocal = FormatLocal(lastConnected, timezoneMinutes),
            OfflineDuration = offlineDuration,
            SpeedKmh = GpsHelper.ParseSpeedKmh(row.Speed),
            DistanceMeters = 0,
            Lat = lat,
            Lng = lng,
            DirectionText = GpsHelper.DirectionTextFromDegrees(int.TryParse((row.Direction ?? "0").Trim(), out var d) ? d : 0),
            Csq = 0,
            GpsUsed = gpsUsed,
            GpsTotal = gpsTotal,
            VoltagePercent = GpsHelper.ParseVoltagePercent(row.OtherStatus),
            VoltageV = ParseVoltageVolts(row.OtherStatus)
        };
    }

    public async Task<bool> SaveInfoAsync(int trackerTbKey, string cname, string iconFile,
        IEnumerable<(int FieldTbKey, string Value)> udValues, IEnumerable<int> labelTbKeys)
    {
        using var conn = _db.CreateMainConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync(@"
                UPDATE dbo.Tracker SET CName = @CName, IconFile = @IconFile
                WHERE tbKey = @TbKey",
                new { CName = cname.Trim(), IconFile = NormalizeIcon(iconFile), TbKey = trackerTbKey }, tx);

            await conn.ExecuteAsync(
                "DELETE FROM dbo.UDFieldsValue WHERE Tracker_tbKey = @TbKey",
                new { TbKey = trackerTbKey }, tx);

            foreach (var (fieldTbKey, value) in udValues)
            {
                if (fieldTbKey <= 0) continue;
                await conn.ExecuteAsync(@"
                    INSERT INTO dbo.UDFieldsValue (Tracker_tbKey, UDFields_tbKey, FValue)
                    VALUES (@TrackerTbKey, @FieldTbKey, @Value)",
                    new { TrackerTbKey = trackerTbKey, FieldTbKey = fieldTbKey, Value = (value ?? "").Trim() }, tx);
            }

            await conn.ExecuteAsync(
                "DELETE FROM dbo.UDLabelValue WHERE Tracker_tbKey = @TbKey",
                new { TbKey = trackerTbKey }, tx);

            foreach (var labelKey in labelTbKeys.Distinct())
            {
                if (labelKey <= 0) continue;
                await conn.ExecuteAsync(@"
                    INSERT INTO dbo.UDLabelValue (Tracker_tbKey, UDLabel_tbKey)
                    VALUES (@TrackerTbKey, @LabelTbKey)",
                    new { TrackerTbKey = trackerTbKey, LabelTbKey = labelKey }, tx);
            }

            tx.Commit();
            return true;
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "SaveInfo 失敗 TbKey={TbKey}", trackerTbKey);
            throw;
        }
    }

    public async Task<bool> SaveOptionAsync(int trackerTbKey, int gpsReportTime, int shockMode, int shockSensitive)
    {
        using var conn = _db.CreateMainConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            var imei = await conn.QuerySingleOrDefaultAsync<string>(
                "SELECT RTRIM(IMEICode) FROM dbo.Tracker WHERE tbKey = @TbKey",
                new { TbKey = trackerTbKey }, tx);
            if (string.IsNullOrEmpty(imei)) return false;

            await conn.ExecuteAsync(@"
                UPDATE dbo.Tracker SET GPSReportTime = @GpsReportTime,
                    Shock_Mode = @ShockMode, Shock_Sensitive = @ShockSensitive, initParameter = 'Y'
                WHERE tbKey = @TbKey",
                new { GpsReportTime = gpsReportTime, ShockMode = shockMode, ShockSensitive = shockSensitive, TbKey = trackerTbKey }, tx);

            var cmdStr = "000000000" + shockMode + shockSensitive + "000000000";
            await conn.ExecuteAsync(@"
                INSERT INTO dbo.QueueCommand (imeicode, CMDCode, CMDStr, CDate, Expdate, Status)
                VALUES (@Imei, 'XA', @CmdStr, GETUTCDATE(), DATEADD(day, 1, GETUTCDATE()), 'W')",
                new { Imei = imei, CmdStr = cmdStr }, tx);

            tx.Commit();
            return true;
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "SaveOption 失敗 TbKey={TbKey}", trackerTbKey);
            throw;
        }
    }

    public async Task<bool> SaveAlertAsync(int trackerTbKey, int memberTbKey, int apMinute, string apMsg,
        int asMinute, string asMsg, bool shareEnabled, string sharePassword, int alertMode)
    {
        var apEnabled = apMinute > 0 ? "Y" : "N";
        var asEnabled = asMinute > 0 ? "Y" : "N";
        var pwd = shareEnabled && sharePassword.Length == 4 ? sharePassword : "12345678";

        using var conn = _db.CreateMainConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            var updated = await conn.ExecuteAsync(@"
                UPDATE dbo.AlertModule SET
                    APMinute = @ApMinute, APMsg = @ApMsg, APEnabled = @ApEnabled,
                    ASMinute = @AsMinute, ASMsg = @AsMsg, ASEnabled = @AsEnabled,
                    initParameter = 'Y'
                WHERE Tracker_tbKey = @TrackerTbKey AND Member_tbKey = @MemberTbKey",
                new
                {
                    ApMinute = apMinute.ToString(),
                    ApMsg = apMsg.Trim(),
                    ApEnabled = apEnabled,
                    AsMinute = asMinute.ToString(),
                    AsMsg = asMsg.Trim(),
                    AsEnabled = asEnabled,
                    TrackerTbKey = trackerTbKey,
                    MemberTbKey = memberTbKey
                }, tx);

            if (updated == 0)
            {
                await conn.ExecuteAsync(@"
                    UPDATE dbo.AlertModule SET
                        APMinute = @ApMinute, APMsg = @ApMsg, APEnabled = @ApEnabled,
                        ASMinute = @AsMinute, ASMsg = @AsMsg, ASEnabled = @AsEnabled,
                        initParameter = 'Y'
                    WHERE Tracker_tbKey = @TrackerTbKey",
                    new
                    {
                        ApMinute = apMinute.ToString(),
                        ApMsg = apMsg.Trim(),
                        ApEnabled = apEnabled,
                        AsMinute = asMinute.ToString(),
                        AsMsg = asMsg.Trim(),
                        AsEnabled = asEnabled,
                        TrackerTbKey = trackerTbKey
                    }, tx);
            }

            await conn.ExecuteAsync(@"
                UPDATE dbo.Tracker SET PASSWORD = @Password, alertmode = @AlertMode, initParameter = 'Y'
                WHERE tbKey = @TbKey",
                new { Password = pwd, AlertMode = alertMode, TbKey = trackerTbKey }, tx);

            tx.Commit();
            return true;
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "SaveAlert 失敗 TbKey={TbKey}", trackerTbKey);
            throw;
        }
    }

    private static async Task EnsureDefaultUdFieldsAsync(System.Data.IDbConnection conn, int memberTbKey)
    {
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.UDFields WHERE Member_tbKey = @MemberTbKey",
            new { MemberTbKey = memberTbKey });
        if (count > 0) return;

        for (var i = 0; i < DefaultUdFieldNames.Length; i++)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO dbo.UDFields (Member_tbKey, FName, FSort) VALUES (@MemberTbKey, @FName, @FSort)",
                new { MemberTbKey = memberTbKey, FName = DefaultUdFieldNames[i], FSort = (i + 1).ToString() });
        }
    }

    private static int NormalizeGpsReportTime(int value) => value switch
    {
        15 or 30 or 600 or 1800 or 3600 or 7200 => value,
        _ => 30
    };

    private static double ParseFenceCoord(string? value)
    {
        if (double.TryParse((value ?? string.Empty).Trim(), out var v) && Math.Abs(v) > 0.0001)
            return v;
        return 0;
    }

    private static string NormalizeIcon(string? icon)
    {
        var v = (icon ?? "A").Trim().ToUpperInvariant();
        return v is "A" or "B" or "C" or "D" or "E" or "F" or "G" or "H" ? v : "A";
    }

    private static string? FormatLocal(DateTime? utc, int tzMinutes)
        => utc?.AddMinutes(tzMinutes).ToString("yyyy/MM/dd HH:mm:ss");

    private static string FormatDuration(TimeSpan span)
    {
        if (span.TotalDays >= 1)
            return $"{(int)span.TotalDays} 天 {span.Hours} 小時 {span.Minutes} 分鐘";
        if (span.TotalHours >= 1)
            return $"{(int)span.TotalHours} 小時 {span.Minutes} 分鐘 {span.Seconds} 秒";
        return $"{span.Minutes} 分鐘 {span.Seconds} 秒";
    }

    private static double ParseVoltageVolts(string? otherStatus)
    {
        var status = (otherStatus ?? "").Trim();
        if (status.Length < 6) return 0;
        if (!int.TryParse(status.Substring(2, 4).Trim(), out var raw)) return 0;
        if (raw <= 5) return 3.7 + raw * 0.1;
        return Math.Round(raw / 100.0, 2);
    }

    private sealed class TrackerSettingsRow
    {
        public int TbKey { get; set; }
        public string Imei { get; set; } = "";
        public string CName { get; set; } = "";
        public string IconFile { get; set; } = "A";
        public string Memo { get; set; } = "";
        public int GpsReportTime { get; set; }
        public int ShockMode { get; set; }
        public int ShockSensitive { get; set; }
        public string? SharePassword { get; set; }
        public int AlertMode { get; set; }
    }

    private sealed class UdFieldRow
    {
        public int FieldTbKey { get; set; }
        public string FieldName { get; set; } = "";
        public string Value { get; set; } = "";
    }

    private sealed class LabelRow
    {
        public int TbKey { get; set; }
        public string Name { get; set; } = "";
        public int Assigned { get; set; }
    }

    private sealed class AlertRow
    {
        public string? ApMinute { get; set; }
        public string? ApMsg { get; set; }
        public string? AsMinute { get; set; }
        public string? AsMsg { get; set; }
    }

    private sealed class PayRow
    {
        public DateTime? SDate { get; set; }
        public DateTime? EDate { get; set; }
    }

    private sealed class DetailRow
    {
        public int TbKey { get; set; }
        public string Imei { get; set; } = "";
        public string CName { get; set; } = "";
        public DateTime? Lastlogintime { get; set; }
        public DateTime? LastReportDate { get; set; }
        public DateTime? LastConnectedDate { get; set; }
        public string? FC_Lat { get; set; }
        public string? FC_Lng { get; set; }
        public string? Speed { get; set; }
        public string? Direction { get; set; }
        public string? Lat { get; set; }
        public string? LatPos { get; set; }
        public string? Lng { get; set; }
        public string? LngPos { get; set; }
        public string? QTY_GPS { get; set; }
        public string? SatelliteStatus { get; set; }
        public string? OtherStatus { get; set; }
        public DateTime? GPSDateTime { get; set; }
    }
}
