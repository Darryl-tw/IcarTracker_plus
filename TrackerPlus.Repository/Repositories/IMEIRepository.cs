using Dapper;
using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Models;
using TrackerPlus.Repository.Infrastructure;
using TrackerPlus.Repository.Mapping;

namespace TrackerPlus.Repository.Repositories;

public class IMEIRepository : IIMEIRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<IMEIRepository> _logger;

    public IMEIRepository(IDbConnectionFactory db, ILogger<IMEIRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IMEIDevice?> GetByIdAsync(int tbKey)
    {
        var sql = $"{IMEIMapper.SelectSql} {IMEIMapper.FromSql} WHERE i.tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        var row = await conn.QuerySingleOrDefaultAsync<IMEIDbRow>(sql, new { TbKey = tbKey });
        return row == null ? null : IMEIMapper.ToModel(row);
    }

    public async Task<IMEIDevice?> GetByIMEIAsync(string imei)
    {
        var sql = $"{IMEIMapper.SelectSql} {IMEIMapper.FromSql} WHERE RTRIM(i.IMEICODE) = @IMEI";
        using var conn = _db.CreateMainConnection();
        var row = await conn.QuerySingleOrDefaultAsync<IMEIDbRow>(sql, new { IMEI = imei.Trim() });
        return row == null ? null : IMEIMapper.ToModel(row);
    }

    public async Task<PagedResult<IMEIDevice>> GetPagedAsync(QueryFilter filter)
    {
        var offset = (filter.PageIndex - 1) * filter.PageSize;
        var where = BuildWhereClause(filter);
        var orderBy = BuildOrderByClause(filter);

        var countSql = $"SELECT COUNT(*) {IMEIMapper.FromSql} {where}";
        var dataSql = $@"{IMEIMapper.SelectSql} {IMEIMapper.FromSql} {where}
            {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var param = new { filter.Keyword, filter.Status, Offset = offset, filter.PageSize };
        using var conn = _db.CreateMainConnection();
        var total = await conn.ExecuteScalarAsync<int>(countSql, param);
        var rows = await conn.QueryAsync<IMEIDbRow>(dataSql, param);

        return new PagedResult<IMEIDevice>
        {
            Items = rows.Select(IMEIMapper.ToModel),
            TotalCount = total,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize
        };
    }

    public async Task<IEnumerable<IMEIDevice>> GetByMemberAsync(int memberTbKey)
    {
        var sql = $@"{IMEIMapper.SelectSql} {IMEIMapper.FromSql}
            WHERE t.Member_tbKey = @MemberTbKey
            ORDER BY i.tbKey DESC";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.QueryAsync<IMEIDbRow>(sql, new { MemberTbKey = memberTbKey });
        return rows.Select(IMEIMapper.ToModel);
    }

    public async Task<int> CreateAsync(IMEIDevice device)
    {
        _logger.LogInformation("新增 IMEI {IMEI}", device.IMEICODE);
        const string sql = @"INSERT INTO dbo.IMEITable
            (OBM_tbKey, IMEICODE, STATUS, CDate, TK_Model, CMemo, Tracker_tbKey)
            VALUES
            (@OBM_TbKey, @IMEICODE, @STATUS, GETDATE(), @TK_Model, @CMemo, 0);
            SELECT CAST(SCOPE_IDENTITY() AS int);";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            device.OBM_TbKey,
            IMEICODE = device.IMEICODE,
            STATUS = string.IsNullOrWhiteSpace(device.Status) ? "N" : device.Status,
            TK_Model = device.ModelName,
            CMemo = device.Memo
        });
    }

    public async Task<bool> UpdateAsync(IMEIDevice device)
    {
        const string sql = @"UPDATE dbo.IMEITable SET
            TK_Model = @TK_Model, CMemo = @CMemo, STATUS = @STATUS
            WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new
        {
            device.TbKey,
            TK_Model = device.ModelName,
            CMemo = device.Memo,
            STATUS = device.Status
        }) > 0;
    }

    public async Task<bool> DeleteAsync(int tbKey)
    {
        _logger.LogWarning("刪除 IMEI TbKey={TbKey}", tbKey);
        const string sql = "DELETE FROM dbo.IMEITable WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { TbKey = tbKey }) > 0;
    }

    public async Task<bool> BatchDeleteAsync(IEnumerable<string> imeiList)
    {
        var list = imeiList.Select(i => i.Trim()).Where(i => i.Length > 0).ToList();
        if (list.Count == 0) return false;
        const string sql = "DELETE FROM dbo.IMEITable WHERE RTRIM(IMEICODE) IN @IMEIList";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { IMEIList = list }) > 0;
    }

    public async Task<bool> MoveToMemberAsync(string imei, int newMemberTbKey, int operatorTbKey)
    {
        _logger.LogInformation("移轉 IMEI {IMEI} 至會員 {Member}", imei, newMemberTbKey);
        const string sql = @"
            UPDATE t SET Member_tbKey = @MemberTbKey
            FROM dbo.Tracker t
            INNER JOIN dbo.IMEITable i ON i.Tracker_tbKey = t.tbKey
            WHERE RTRIM(i.IMEICODE) = @IMEI;
            UPDATE dbo.IMEITable SET STATUS = 'Y' WHERE RTRIM(IMEICODE) = @IMEI;";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { IMEI = imei.Trim(), MemberTbKey = newMemberTbKey }) > 0;
    }

    public async Task<bool> ResetAsync(string imei)
    {
        _logger.LogWarning("解除綁定 IMEI {IMEI}", imei);
        const string sql = @"
            UPDATE dbo.Tracker SET Member_tbKey = 0, TrackerEnabled = 'N'
            WHERE RTRIM(IMEICode) = @IMEI;
            UPDATE dbo.IMEITable SET STATUS = 'N'
            WHERE RTRIM(IMEICODE) = @IMEI;";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { IMEI = imei.Trim() }) >= 0;
    }

    public async Task<bool> FullResetAsync(string imei, IMEIFullResetOptions options)
    {
        imei = imei.Trim();
        _logger.LogWarning("完整重置 IMEI {IMEI} DeleteHistory={DelHist} ResetPayLog={Pay}",
            imei, options.DeleteHistory, options.ResetPayLog);

        using var mainConn = _db.CreateMainConnection();
        await mainConn.OpenAsync();

        var trackerTbKey = await mainConn.ExecuteScalarAsync<int?>(
            "SELECT Tracker_tbKey FROM dbo.IMEITable WHERE RTRIM(IMEICODE) = @IMEI",
            new { IMEI = imei });

        if (trackerTbKey is null or 0)
            trackerTbKey = await mainConn.ExecuteScalarAsync<int?>(
                "SELECT tbKey FROM dbo.Tracker WHERE RTRIM(IMEICode) = @IMEI",
                new { IMEI = imei });

        var oldTrackerKey = trackerTbKey ?? 0;

        if (options.DeleteHistory && oldTrackerKey > 0)
        {
            var exists = await mainConn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM dbo.DelHistoryTLGAll WHERE RTRIM(IMEICODE) = @IMEI",
                new { IMEI = imei });
            if (exists == 0)
            {
                await mainConn.ExecuteAsync(
                    @"INSERT INTO dbo.DelHistoryTLGAll (IMEICODE, TrackerEnableDate)
                      SELECT RTRIM(IMEICode), TrackerEnabledDate FROM dbo.Tracker WHERE RTRIM(IMEICode) = @IMEI",
                    new { IMEI = imei });
            }
            else
            {
                await mainConn.ExecuteAsync(
                    "UPDATE dbo.DelHistoryTLGAll SET DelDateTime = GETUTCDATE() WHERE RTRIM(IMEICODE) = @IMEI",
                    new { IMEI = imei });
            }
        }

        if (oldTrackerKey > 0)
        {
            await mainConn.ExecuteAsync(
                @"DELETE FROM dbo.AlertModule WHERE Tracker_tbKey = @Key;
                  DELETE FROM dbo.UDFieldsValue WHERE Tracker_tbKey = @Key;
                  DELETE FROM dbo.UDLabelValue WHERE Tracker_tbKey = @Key;",
                new { Key = oldTrackerKey });
        }

        await mainConn.ExecuteAsync(
            @"DELETE FROM dbo.SendGCM WHERE RTRIM(IMEICODE) = @IMEI;
              DELETE FROM dbo.SendGCMLog WHERE RTRIM(IMEICODE) = @IMEI;
              DELETE FROM dbo.ICARMEMBER WHERE RTRIM(IMEICODE) = @IMEI;
              DELETE FROM dbo.Tracker WHERE RTRIM(IMEICode) = @IMEI;
              DELETE FROM dbo.Tracker_Info WHERE RTRIM(IMEICode) = @IMEI;",
            new { IMEI = imei });

        await TruncatePerImeiLogTablesAsync(imei);

        var defaults = await mainConn.QuerySingleOrDefaultAsync<SystemOptionsRow>(
            "SELECT TOP 1 TimeZone_tbKey, GPSReportTime, SMSPassword, SOSCT FROM dbo.Options");
        var timeZone = defaults?.TimeZone_tbKey ?? 29;
        var gpsReportTime = defaults?.GPSReportTime ?? 30;
        var smsPassword = defaults?.SMSPassword?.Trim() ?? "0000";
        var sosCt = defaults?.SOSCT?.Trim() ?? "+886";

        const string insertTracker = @"
            INSERT INTO dbo.Tracker (
                TrackerEnabledDate, OBM_tbKey, UserLevel, Member_tbKey, IMEICode, CName,
                TrackerEnabled, CurrentStatus, CDate, TimeZone_tbKey, GPSReportTime, IconFile,
                SOSCT1, SOSTEL1, SOSCT2, SOSTEL2, SOSCT3, SOSTEL3, SOSMSG, SMSPassword, initParameter, FWVERSION)
            VALUES (
                GETUTCDATE(), @OBM, 'N', 0, @IMEI, @IMEI, 'Y', 'N',
                CONVERT(varchar(20), GETUTCDATE(), 112) + REPLACE(CONVERT(varchar(20), GETUTCDATE(), 108), ':', ''),
                @TZ, @GPS, 'A', @SOS, '', @SOS, '', @SOS, '', '', @SMS, 'N', '');
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        var newTrackerKey = await mainConn.ExecuteScalarAsync<int>(insertTracker, new
        {
            OBM = options.ObmTbKey,
            IMEI = imei,
            TZ = timeZone,
            GPS = gpsReportTime,
            SOS = sosCt,
            SMS = smsPassword
        });

        await mainConn.ExecuteAsync(
            @"UPDATE dbo.IMEITable SET STATUS = 'Y', Tracker_tbKey = @TrackerKey, CDate = GETUTCDATE(), OBM_tbKey = @OBM
              WHERE RTRIM(SERIALCODE) = @IMEI OR RTRIM(IMEICODE) = @IMEI;
              IF NOT EXISTS (SELECT 1 FROM dbo.Tracker_Info WHERE RTRIM(IMEICode) = @IMEI)
                  INSERT INTO dbo.Tracker_Info (IMEICode) VALUES (@IMEI);",
            new { TrackerKey = newTrackerKey, OBM = options.ObmTbKey, IMEI = imei });

        await EnsureAlertModuleDefaultAsync(mainConn, newTrackerKey, imei);

        if (options.ResetPayLog)
        {
            await mainConn.ExecuteAsync("DELETE FROM dbo.PayLog WHERE RTRIM(IMEICode) = @IMEI", new { IMEI = imei });
        }
        else
        {
            await mainConn.ExecuteAsync(
                @"UPDATE dbo.PayLog SET Tracker_tbKey = @TrackerKey, Member_tbKey = 0 WHERE RTRIM(IMEICode) = @IMEI",
                new { TrackerKey = newTrackerKey, IMEI = imei });
        }

        return true;
    }

    private async Task TruncatePerImeiLogTablesAsync(string imei)
    {
        var suffixes = new[] { "AlertLog_", "SYSLog_", "TL_", "TLS_" };
        using var logConn = _db.CreateLogConnection();
        await logConn.OpenAsync();
        foreach (var suffix in suffixes)
        {
            var tableName = suffix + imei;
            var exists = await logConn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM sys.tables WHERE name = @Name AND schema_id = SCHEMA_ID('dbo')",
                new { Name = tableName });
            if (exists > 0)
                await logConn.ExecuteAsync($"TRUNCATE TABLE dbo.[{tableName}]");
        }
    }

    private static async Task EnsureAlertModuleDefaultAsync(Microsoft.Data.SqlClient.SqlConnection conn, int trackerTbKey, string imei)
    {
        const string sql = @"
            IF NOT EXISTS (SELECT 1 FROM dbo.AlertModule WHERE Tracker_tbKey = @TrackerKey AND Member_tbKey = 0)
            INSERT INTO dbo.AlertModule (
                Member_tbKey, Tracker_tbKey, ATEnabled, ATWeekDay, ATWeekTime,
                ACEnabled, ACMinute, APEnabled, APMinute, ASEnabled, ASMinute, ABEnabled,
                ATMsg, ACMsg, APMsg, ASMsg, ABMsg, AR_EMail1, AR_EMail2, AR_EMail3, AR_SMS1, AR_SMS2, AR_SMS3)
            VALUES (0, @TrackerKey, 'N', '', '00:00', 'N', 0, 'N', 0, 'N', 0, 'N',
                @Msg, @Msg, @Msg, @Msg, @Msg, '', '', '', '', '', '')";
        await conn.ExecuteAsync(sql, new { TrackerKey = trackerTbKey, Msg = imei });
    }

    public async Task<bool> ExistsAsync(string imei)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.IMEITable WHERE RTRIM(IMEICODE) = @IMEI";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { IMEI = imei.Trim() }) > 0;
    }

    public async Task<int> GetCountAsync(QueryFilter filter)
    {
        var where = BuildWhereClause(filter);
        var sql = $"SELECT COUNT(*) {IMEIMapper.FromSql} {where}";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { filter.Keyword, filter.Status });
    }

    public async Task<IEnumerable<IMEIDevice>> GetPendingFirmwareUpdateAsync()
    {
        var sql = $@"{IMEIMapper.SelectSql} {IMEIMapper.FromSql}
            WHERE t.isUpdate = 1 OR RTRIM(t.update_FWVERSION) <> ''";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.QueryAsync<IMEIDbRow>(sql);
        return rows.Select(IMEIMapper.ToModel);
    }

    private sealed class SystemOptionsRow
    {
        public int TimeZone_tbKey { get; set; }
        public int GPSReportTime { get; set; }
        public string SMSPassword { get; set; } = "0000";
        public string SOSCT { get; set; } = "+886";
    }

    private static string BuildWhereClause(QueryFilter filter)
    {
        var conditions = new List<string>();
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            conditions.Add("(RTRIM(i.IMEICODE) LIKE '%' + @Keyword + '%' OR RTRIM(m.CName) LIKE '%' + @Keyword + '%' OR RTRIM(m.ID) LIKE '%' + @Keyword + '%')");
        if (!string.IsNullOrWhiteSpace(filter.Status))
            conditions.Add("RTRIM(i.STATUS) = @Status");
        return conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
    }

    private static string BuildOrderByClause(QueryFilter filter)
    {
        var dir = filter.SortDesc ? "DESC" : "ASC";
        var key = (filter.SortBy ?? "").Trim().ToLowerInvariant();
        return key switch
        {
            "tbkey" => $"ORDER BY i.tbKey {dir}",
            "imei" or "imeicode" => $"ORDER BY RTRIM(i.IMEICODE) {dir}",
            "model" or "modelname" => $"ORDER BY RTRIM(ISNULL(i.TK_Model,'')) {dir}",
            "firmware" or "firmwareversion" => $"ORDER BY RTRIM(ISNULL(t.FWVersion,'')) {dir}",
            "status" => $"ORDER BY RTRIM(i.STATUS) {dir}",
            "member" or "membername" => $"ORDER BY RTRIM(ISNULL(m.CName,'')) {dir}",
            "cdate" or "createdate" => $"ORDER BY i.CDate {dir}",
            _ => "ORDER BY i.tbKey DESC"
        };
    }
}
