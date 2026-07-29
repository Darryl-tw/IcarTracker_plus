using Dapper;
using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Models;
using TrackerPlus.Repository.Infrastructure;
using TrackerPlus.Repository.Mapping;

namespace TrackerPlus.Repository.Repositories;

public class TrackerRepository : ITrackerRepository
{
    private const string TrackerMemberJoins = @"
        LEFT JOIN dbo.Member m ON t.Member_tbKey = m.tbKey
        LEFT JOIN dbo.Userdb u ON t.OBM_tbKey = u.tbKey";

    private readonly IDbConnectionFactory _db;
    private readonly ILogger<TrackerRepository> _logger;

    public TrackerRepository(IDbConnectionFactory db, ILogger<TrackerRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Tracker?> GetByIdAsync(int tbKey)
    {
        _logger.LogDebug("取得追蹤器 TbKey={TbKey}", tbKey);
        var sql = $"{TrackerMapper.SelectColumns} {TrackerMapper.FromClause} WHERE t.tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        var row = await conn.QuerySingleOrDefaultAsync<TrackerDbRow>(sql, new { TbKey = tbKey });
        return row == null ? null : TrackerMapper.ToModel(row);
    }

    public async Task<Tracker?> GetByIMEIAsync(string imei)
    {
        _logger.LogDebug("取得追蹤器 IMEI={IMEI}", imei);
        var sql = $"{TrackerMapper.SelectColumns} {TrackerMapper.FromClause} WHERE RTRIM(t.IMEICode) = @IMEI";
        using var conn = _db.CreateMainConnection();
        var row = await conn.QuerySingleOrDefaultAsync<TrackerDbRow>(sql, new { IMEI = imei.Trim() });
        return row == null ? null : TrackerMapper.ToModel(row);
    }

    public async Task<IEnumerable<Tracker>> GetByMemberAsync(int memberTbKey)
    {
        _logger.LogDebug("取得會員追蹤器列表 Member={Member}", memberTbKey);
        var sql = $"{TrackerMapper.SelectColumns} {TrackerMapper.FromClause} WHERE t.Member_tbKey = @MemberTbKey ORDER BY RTRIM(ISNULL(t.CName,''))";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.QueryAsync<TrackerDbRow>(sql, new { MemberTbKey = memberTbKey });
        return rows.Select(TrackerMapper.ToModel);
    }

    public async Task<PagedResult<Tracker>> GetPagedAsync(QueryFilter filter, int? memberTbKey = null)
    {
        var offset = (filter.PageIndex - 1) * filter.PageSize;
        var where = BuildWhereClause(filter, memberTbKey);

        var orderBy = BuildOrderByClause(filter);

        var countSql = $"SELECT COUNT(*) FROM dbo.Tracker t {TrackerMemberJoins} {where}";

        // 先用 CTE 取出分頁的 tbKey（不含昂貴的 PayLog 子查詢），
        // 再 INNER JOIN 回來只對這 PageSize 筆執行完整欄位查詢，避免全表掃描效能問題。
        var dataSql = $@"
WITH _PageKeys AS (
    SELECT t.tbKey
    FROM dbo.Tracker t
    {TrackerMemberJoins}
    {where}
    {orderBy}
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
){TrackerMapper.SelectColumns},
            RTRIM(ISNULL(m.CName,'')) AS MemberName,
            RTRIM(ISNULL(u.UserName,'')) AS OBMName,
            RTRIM(ISNULL(m.ID,'')) AS MemberAccount,
            ISNULL((SELECT COUNT(*) FROM dbo.GCM_EMAIL g WHERE g.EMAIL = m.ID), 0)
              + ISNULL((SELECT COUNT(*) FROM dbo.ClientIDRegistration c WHERE c.ID_tbkey = m.tbKey), 0) AS BindCount
{TrackerMapper.FromClause}
INNER JOIN _PageKeys pk ON t.tbKey = pk.tbKey
{TrackerMemberJoins}
{orderBy}";

        var param = new
        {
            filter.Obm,
            filter.Imei,
            filter.Account,
            filter.Keyword,
            Status = filter.Status,
            MemberTbKey = memberTbKey,
            Offset = offset,
            filter.PageSize
        };
        using var conn = _db.CreateMainConnection();
        var total = await conn.ExecuteScalarAsync<int>(countSql, param);
        var rows = await conn.QueryAsync<TrackerDbRow>(dataSql, param, commandTimeout: 60);

        return new PagedResult<Tracker>
        {
            Items = rows.Select(TrackerMapper.ToModel),
            TotalCount = total,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize
        };
    }

    public async Task<int> CreateAsync(Tracker tracker)
    {
        _logger.LogInformation("新增追蹤器 IMEI={IMEI}", tracker.IMEICODE);
        const string sql = @"INSERT INTO dbo.Tracker
            (IMEICode, Member_tbKey, CName, TrackerEnabled, PowerSavingMode, SOSTel1, CMemo, CDate, TrackerEnabledDate)
            VALUES
            (@IMEICode, @Member_TbKey, @CName, @TrackerEnabled, @PowerSavingMode, @SOSTel1, @CMemo, GETUTCDATE(), GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS int);";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            IMEICode = tracker.IMEICODE,
            tracker.Member_TbKey,
            tracker.CName,
            TrackerEnabled = tracker.TrackerStatus,
            tracker.PowerSavingMode,
            SOSTel1 = tracker.SosNumber,
            CMemo = tracker.Memo
        });
    }

    public async Task<bool> UpdateAsync(Tracker tracker)
    {
        _logger.LogInformation("更新追蹤器 TbKey={TbKey}", tracker.TbKey);
        const string sql = @"UPDATE dbo.Tracker SET
            CName = @CName,
            TrackerEnabled = @TrackerEnabled,
            PowerSavingMode = @PowerSavingMode,
            SOSTel1 = @SOSTel1,
            CMemo = @CMemo
            WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.ExecuteAsync(sql, new
        {
            tracker.TbKey,
            tracker.CName,
            TrackerEnabled = tracker.TrackerStatus,
            tracker.PowerSavingMode,
            SOSTel1 = tracker.SosNumber,
            CMemo = tracker.Memo
        });
        return rows > 0;
    }

    public async Task<bool> UpdateLiveSettingsAsync(int tbKey, string cname, string memo, string iconFile)
    {
        const string sql = @"UPDATE dbo.Tracker SET
            CName = @CName,
            CMemo = @CMemo,
            IconFile = @IconFile
            WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.ExecuteAsync(sql, new
        {
            TbKey = tbKey,
            CName = cname.Trim(),
            CMemo = memo?.Trim() ?? string.Empty,
            IconFile = iconFile.Trim().ToUpperInvariant()
        });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int tbKey)
    {
        _logger.LogWarning("刪除追蹤器 TbKey={TbKey}", tbKey);
        const string sql = "DELETE FROM dbo.Tracker WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.ExecuteAsync(sql, new { TbKey = tbKey });
        return rows > 0;
    }

    public async Task<bool> UpdateStatusAsync(int tbKey, string status)
    {
        const string sql = "UPDATE dbo.Tracker SET TrackerEnabled = @Status WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.ExecuteAsync(sql, new { TbKey = tbKey, Status = status });
        return rows > 0;
    }

    public async Task<bool> UpdateGeofenceAsync(int tbKey, Geofence geofence)
    {
        _logger.LogInformation("更新電子柵欄 TbKey={TbKey} Index={Index}", tbKey, geofence.FenceIndex);
        var idx = geofence.FenceIndex;
        var suffix = idx == 0 ? string.Empty : idx.ToString();
        var sql = $@"UPDATE dbo.Tracker SET
            FC_Lat{suffix} = @Lat,
            FC_Lng{suffix} = @Lng,
            FC_R{suffix} = @Radius,
            isFCEnable{suffix} = @Enable,
            initParameter = 'Y'
            WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.ExecuteAsync(sql, new
        {
            TbKey = tbKey,
            Lat = geofence.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Lng = geofence.Lng.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Radius = ((int)geofence.Radius).ToString(),
            Enable = geofence.Enable is "Y" or "1" ? "1" : "0"
        });
        return rows > 0;
    }

    public async Task<bool> UpdateLiveLocationAsync(string imei, double lat, double lng, double speed, int direction, DateTime utcTime)
    {
        // 即時位置由裝置寫入 Tracker_Info / TLG，Web 端僅讀取
        _logger.LogDebug("略過 UpdateLiveLocationAsync（由裝置回報） IMEI={IMEI}", imei);
        return await Task.FromResult(true);
    }

    public async Task<IEnumerable<Tracker>> GetByGroupAsync(int memberTbKey, string groupName)
    {
        // 舊版群組在 UDLabel，雛型階段回傳該會員全部啟用裝置
        return await GetByMemberAsync(memberTbKey);
    }

    public async Task<int> GetCountAsync(int? memberTbKey = null)
    {
        var sql = memberTbKey.HasValue
            ? "SELECT COUNT(*) FROM dbo.Tracker WHERE Member_tbKey = @MemberTbKey"
            : "SELECT COUNT(*) FROM dbo.Tracker";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { MemberTbKey = memberTbKey });
    }

    public async Task<OperationResult> BatchTransferToOBMAsync(IEnumerable<string> imeis, int obmTbKey)
    {
        var list = imeis.Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
        if (list.Count == 0) return OperationResult.Fail("無 IMEI");
        using var conn = _db.CreateMainConnection();
        int count = 0;
        foreach (var imei in list)
        {
            count += await conn.ExecuteAsync(
                "UPDATE dbo.Tracker SET OBM_tbKey=@OBM WHERE RTRIM(IMEICode)=@IMEI",
                new { OBM = obmTbKey, IMEI = imei });
            await conn.ExecuteAsync(
                "UPDATE dbo.IMEITable SET OBM_tbKey=@OBM WHERE RTRIM(IMEICODE)=@IMEI",
                new { OBM = obmTbKey, IMEI = imei });
        }
        return OperationResult.Ok($"已轉移 {count} 筆", count);
    }

    public async Task<OperationResult> FactoryResetAsync(int tbKey)
    {
        _logger.LogWarning("恢復出廠 TbKey={TbKey}", tbKey);
        using var conn = _db.CreateMainConnection();
        await conn.ExecuteAsync("DELETE FROM dbo.AlertModule WHERE Tracker_tbKey=@TbKey", new { TbKey = tbKey });
        await conn.ExecuteAsync("DELETE FROM dbo.UDFieldsValue WHERE Tracker_tbKey=@TbKey", new { TbKey = tbKey });
        await conn.ExecuteAsync("DELETE FROM dbo.UDLabelValue WHERE Tracker_tbKey=@TbKey", new { TbKey = tbKey });
        var rows = await conn.ExecuteAsync(
            "UPDATE dbo.Tracker SET Member_tbKey=0, CName='', Password='12345678' WHERE tbKey=@TbKey",
            new { TbKey = tbKey });
        return rows > 0 ? OperationResult.Ok("恢復出廠成功") : OperationResult.Fail("找不到裝置");
    }

    public async Task<int> BatchDeleteByKeysAsync(IEnumerable<int> tbKeys)
    {
        var list = tbKeys.ToList();
        if (list.Count == 0) return 0;
        using var conn = _db.CreateMainConnection();
        int deleted = 0;
        foreach (var key in list)
            deleted += await conn.ExecuteAsync("DELETE FROM dbo.Tracker WHERE tbKey=@TbKey", new { TbKey = key });
        return deleted;
    }

    public async Task<OperationResult> UnbindAsync(int tbKey)
    {
        _logger.LogWarning("解除裝置綁定 TbKey={TbKey}", tbKey);
        using var conn = _db.CreateMainConnection();
        await conn.ExecuteAsync(
            "UPDATE dbo.AlertModule SET Member_tbKey=0 WHERE Tracker_tbKey=@TbKey",
            new { TbKey = tbKey });
        var rows = await conn.ExecuteAsync(
            "UPDATE dbo.Tracker SET Member_tbKey=0, Password='12345678' WHERE tbKey=@TbKey",
            new { TbKey = tbKey });
        return rows > 0 ? OperationResult.Ok("解除裝置成功") : OperationResult.Fail("找不到裝置");
    }

    public async Task<OperationResult> UnbindAllByMemberAsync(int memberTbKey)
    {
        _logger.LogWarning("解除會員所有裝置綁定 MemberTbKey={Key}", memberTbKey);
        using var conn = _db.CreateMainConnection();
        await conn.ExecuteAsync(
            "UPDATE dbo.AlertModule SET Member_tbKey=0 WHERE Tracker_tbKey IN (SELECT tbKey FROM dbo.Tracker WHERE Member_tbKey=@MemberTbKey)",
            new { MemberTbKey = memberTbKey });
        var rows = await conn.ExecuteAsync(
            "UPDATE dbo.Tracker SET Member_tbKey=0, Password='12345678' WHERE Member_tbKey=@MemberTbKey",
            new { MemberTbKey = memberTbKey });
        return rows > 0 ? OperationResult.Ok($"已解除 {rows} 台裝置") : OperationResult.Fail("找不到裝置或該會員無綁定裝置");
    }

    private static string BuildWhereClause(QueryFilter filter, int? memberTbKey)
    {
        var conditions = new List<string>();
        if (memberTbKey.HasValue)
            conditions.Add("t.Member_tbKey = @MemberTbKey");
        if (!string.IsNullOrWhiteSpace(filter.Obm))
            conditions.Add("RTRIM(u.UserName) LIKE '%' + @Obm + '%'");
        if (!string.IsNullOrWhiteSpace(filter.Imei))
            conditions.Add(@"(RTRIM(t.IMEICode) LIKE '%' + @Imei + '%'
                OR RTRIM(t.CName) LIKE '%' + @Imei + '%')");
        if (!string.IsNullOrWhiteSpace(filter.Account))
            conditions.Add(@"(RTRIM(m.ID) LIKE '%' + @Account + '%'
                OR RTRIM(m.EMail) LIKE '%' + @Account + '%'
                OR RTRIM(m.CName) LIKE '%' + @Account + '%')");
        // 相容舊版單一關鍵字搜尋
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            conditions.Add(@"(RTRIM(t.IMEICode) LIKE '%' + @Keyword + '%'
                OR RTRIM(t.CName) LIKE '%' + @Keyword + '%'
                OR RTRIM(m.ID) LIKE '%' + @Keyword + '%'
                OR RTRIM(m.EMail) LIKE '%' + @Keyword + '%'
                OR RTRIM(m.CName) LIKE '%' + @Keyword + '%'
                OR RTRIM(u.UserName) LIKE '%' + @Keyword + '%')");
        if (!string.IsNullOrWhiteSpace(filter.Status))
            conditions.Add("t.TrackerEnabled = @Status");
        return conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
    }

    /// <summary>依 QueryFilter.SortBy 產生安全 ORDER BY（白名單，避免 SQL injection）。</summary>
    private static string BuildOrderByClause(QueryFilter filter)
    {
        var dir = filter.SortDesc ? "DESC" : "ASC";
        var key = (filter.SortBy ?? string.Empty).Trim().ToLowerInvariant();
        const string tie = "RTRIM(ISNULL(t.IMEICode,'')) ASC";

        return key switch
        {
            "obm" => $"ORDER BY RTRIM(ISNULL(u.UserName,'')) {dir}, {tie}",
            "imei" => $"ORDER BY RTRIM(ISNULL(t.IMEICode,'')) {dir}",
            "cname" or "name" => $"ORDER BY RTRIM(ISNULL(t.CName,'')) {dir}, {tie}",
            "bindcount" => $@"ORDER BY (
                ISNULL((SELECT COUNT(*) FROM dbo.GCM_EMAIL g WHERE g.EMAIL = m.ID), 0)
                + ISNULL((SELECT COUNT(*) FROM dbo.ClientIDRegistration c WHERE c.ID_tbkey = m.tbKey), 0)
            ) {dir}, {tie}",
            "createdate" => $"ORDER BY t.CDate {dir}, {tie}",
            "serviceenddate" or "serviceend" => $@"ORDER BY (
                SELECT TOP 1 pl.EDate
                FROM dbo.PayLog pl
                WHERE pl.Tracker_tbKey = t.tbKey
                  AND pl.SDate <= GETDATE()
                  AND pl.EDate >= DATEADD(DAY,-1,GETDATE())
                  AND pl.SDate <> pl.EDate
                ORDER BY pl.EDate DESC
            ) {dir}, {tie}",
            "account" => $"ORDER BY RTRIM(ISNULL(m.ID,'')) {dir}, {tie}",
            "currentstatus" or "status" =>
                $"ORDER BY ISNULL(t.CurrentStatus,'N') {dir}, ISNULL(t.issleep,'N') {dir}, {tie}",
            "firmware" or "firmwareversion" => $"ORDER BY RTRIM(ISNULL(t.FWVersion,'')) {dir}, {tie}",
            "iccid" => $"ORDER BY ISNULL(RTRIM(t.ICCID),'') {dir}, {tie}",
            "apn" => $"ORDER BY ISNULL(RTRIM(t.APN),'') {dir}, {tie}",
            "tbkey" => $"ORDER BY t.tbKey {dir}",
            _ => "ORDER BY RTRIM(ISNULL(t.IMEICode,'')) ASC"
        };
    }

}

