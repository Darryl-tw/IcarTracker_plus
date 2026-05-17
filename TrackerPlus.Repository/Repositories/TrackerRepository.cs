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
        var sql = $@"{TrackerMapper.SelectColumns} {TrackerMapper.FromClause}
            WHERE t.Member_tbKey = @MemberTbKey AND t.TrackerEnabled = 'Y'
            ORDER BY t.CName";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.QueryAsync<TrackerDbRow>(sql, new { MemberTbKey = memberTbKey });
        return rows.Select(TrackerMapper.ToModel);
    }

    public async Task<PagedResult<Tracker>> GetPagedAsync(QueryFilter filter, int? memberTbKey = null)
    {
        var offset = (filter.PageIndex - 1) * filter.PageSize;
        var where = BuildWhereClause(filter, memberTbKey);

        var countSql = $"SELECT COUNT(*) FROM dbo.Tracker t LEFT JOIN dbo.Member m ON t.Member_tbKey = m.tbKey {where}";
        var dataSql = $@"{TrackerMapper.SelectColumns}, RTRIM(m.CName) AS MemberName
            {TrackerMapper.FromClause}
            LEFT JOIN dbo.Member m ON t.Member_tbKey = m.tbKey
            {where}
            ORDER BY t.tbKey DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var param = new { filter.Keyword, Status = filter.Status, MemberTbKey = memberTbKey, Offset = offset, filter.PageSize };
        using var conn = _db.CreateMainConnection();
        var total = await conn.ExecuteScalarAsync<int>(countSql, param);
        var rows = await conn.QueryAsync<TrackerDbRow>(dataSql, param);

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

    private static string BuildWhereClause(QueryFilter filter, int? memberTbKey)
    {
        var conditions = new List<string>();
        if (memberTbKey.HasValue)
            conditions.Add("t.Member_tbKey = @MemberTbKey");
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            conditions.Add("(RTRIM(t.IMEICode) LIKE '%' + @Keyword + '%' OR RTRIM(t.CName) LIKE '%' + @Keyword + '%')");
        if (!string.IsNullOrWhiteSpace(filter.Status))
            conditions.Add("t.TrackerEnabled = @Status");
        return conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
    }
}
