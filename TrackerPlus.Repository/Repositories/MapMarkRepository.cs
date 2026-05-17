using Dapper;
using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Models;
using TrackerPlus.Repository.Infrastructure;

namespace TrackerPlus.Repository.Repositories;

public class MapMarkRepository : IMapMarkRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<MapMarkRepository> _logger;

    public MapMarkRepository(IDbConnectionFactory db, ILogger<MapMarkRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<MapMark>> GetByMemberAsync(int memberTbKey)
    {
        const string sql = @"
            SELECT tbKey AS TbKey, member_tbkey AS MemberTbKey,
                   RTRIM(Address) AS Address, RTRIM(Memo) AS Memo,
                   LatPos, LngPos, CDATE AS CreatedAt
            FROM dbo.tb_MapMark
            WHERE member_tbkey = @MemberTbKey
            ORDER BY tbKey DESC";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.QueryAsync<MapMarkRow>(sql, new { MemberTbKey = memberTbKey });
        return rows.Select(ToModel);
    }

    public async Task<int> GetCountByMemberAsync(int memberTbKey)
    {
        const string sql = "SELECT COUNT(*) FROM dbo.tb_MapMark WHERE member_tbkey = @MemberTbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { MemberTbKey = memberTbKey });
    }

    public async Task<int> CreateAsync(int memberTbKey, string address, double lat, double lng, string memo)
    {
        const string sql = @"
            INSERT INTO dbo.tb_MapMark (member_tbkey, Address, CDATE, LatPos, LngPos, Memo)
            VALUES (@MemberTbKey, @Address, GETUTCDATE(), @LatPos, @LngPos, @Memo);
            UPDATE dbo.Member SET MapMarkUpdateTime = GETUTCDATE() WHERE tbKey = @MemberTbKey;
            SELECT CAST(SCOPE_IDENTITY() AS int);";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            MemberTbKey = memberTbKey,
            Address = address.Trim(),
            LatPos = lat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture),
            LngPos = lng.ToString("F6", System.Globalization.CultureInfo.InvariantCulture),
            Memo = memo.Trim()
        });
    }

    public async Task<bool> DeleteAsync(int tbKey, int memberTbKey)
    {
        const string sql = @"
            UPDATE dbo.Member SET MapMarkUpdateTime = GETUTCDATE()
            WHERE tbKey = @MemberTbKey;
            DELETE FROM dbo.tb_MapMark WHERE tbKey = @TbKey AND member_tbkey = @MemberTbKey";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.ExecuteAsync(sql, new { TbKey = tbKey, MemberTbKey = memberTbKey });
        return rows > 0;
    }

    private static MapMark ToModel(MapMarkRow row)
    {
        _ = double.TryParse((row.LatPos ?? "").Trim(), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var lat);
        _ = double.TryParse((row.LngPos ?? "").Trim(), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var lng);
        return new MapMark
        {
            TbKey = row.TbKey,
            MemberTbKey = row.MemberTbKey,
            Address = row.Address?.Trim() ?? "",
            Memo = row.Memo?.Trim() ?? "",
            Lat = lat,
            Lng = lng,
            CreatedAt = row.CreatedAt
        };
    }

    private sealed class MapMarkRow
    {
        public int TbKey { get; set; }
        public int MemberTbKey { get; set; }
        public string? Address { get; set; }
        public string? Memo { get; set; }
        public string? LatPos { get; set; }
        public string? LngPos { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
