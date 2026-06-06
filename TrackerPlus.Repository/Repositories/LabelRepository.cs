using Dapper;
using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Models;
using TrackerPlus.Repository.Infrastructure;

namespace TrackerPlus.Repository.Repositories;

public class LabelRepository : ILabelRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<LabelRepository> _logger;

    public LabelRepository(IDbConnectionFactory db, ILogger<LabelRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<UdLabel>> GetLabelsByMemberAsync(int memberTbKey)
    {
        const string sql = @"SELECT tbKey AS TbKey, Member_tbKey AS MemberTbKey, RTRIM(LabelName) AS LabelName
            FROM dbo.UDLabel WHERE Member_tbKey = @MemberTbKey ORDER BY LabelName";
        using var conn = _db.CreateMainConnection();
        return await conn.QueryAsync<UdLabel>(sql, new { MemberTbKey = memberTbKey });
    }

    public async Task<IEnumerable<UdLabel>> GetLabelsByTrackerAsync(int trackerTbKey, int memberTbKey)
    {
        const string sql = @"SELECT l.tbKey AS TbKey, l.Member_tbKey AS MemberTbKey, RTRIM(l.LabelName) AS LabelName
            FROM dbo.UDLabel l
            INNER JOIN dbo.UDLabelValue v ON v.UDLabel_tbKey = l.tbKey
            WHERE v.Tracker_tbKey = @TrackerTbKey AND l.Member_tbKey = @MemberTbKey
            ORDER BY l.LabelName";
        using var conn = _db.CreateMainConnection();
        return await conn.QueryAsync<UdLabel>(sql, new { TrackerTbKey = trackerTbKey, MemberTbKey = memberTbKey });
    }

    public async Task<IEnumerable<int>> GetAssignedLabelTbKeysAsync(int trackerTbKey)
    {
        const string sql = "SELECT UDLabel_tbKey FROM dbo.UDLabelValue WHERE Tracker_tbKey = @TrackerTbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.QueryAsync<int>(sql, new { TrackerTbKey = trackerTbKey });
    }

    public async Task<bool> SetTrackerLabelsAsync(int trackerTbKey, IEnumerable<int> labelTbKeys)
    {
        var keys = labelTbKeys.Distinct().ToList();
        using var conn = _db.CreateMainConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync(
                "DELETE FROM dbo.UDLabelValue WHERE Tracker_tbKey = @TrackerTbKey",
                new { TrackerTbKey = trackerTbKey }, tx);

            foreach (var labelKey in keys)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO dbo.UDLabelValue (Tracker_tbKey, UDLabel_tbKey) VALUES (@TrackerTbKey, @LabelTbKey)",
                    new { TrackerTbKey = trackerTbKey, LabelTbKey = labelKey }, tx);
            }

            tx.Commit();
            return true;
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "設定 Tracker 標籤失敗 TbKey={TbKey}", trackerTbKey);
            throw;
        }
    }

    public async Task<int> CreateLabelAsync(int memberTbKey, string labelName)
    {
        const string sql = @"INSERT INTO dbo.UDLabel (Member_tbKey, LabelName) VALUES (@MemberTbKey, @LabelName);
            SELECT CAST(SCOPE_IDENTITY() AS int);";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { MemberTbKey = memberTbKey, LabelName = labelName.Trim() });
    }

    public async Task<bool> DeleteLabelAsync(int labelTbKey, int memberTbKey)
    {
        using var conn = _db.CreateMainConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync(
                "DELETE FROM dbo.UDLabelValue WHERE UDLabel_tbKey = @LabelTbKey",
                new { LabelTbKey = labelTbKey }, tx);
            var rows = await conn.ExecuteAsync(
                "DELETE FROM dbo.UDLabel WHERE tbKey = @LabelTbKey AND Member_tbKey = @MemberTbKey",
                new { LabelTbKey = labelTbKey, MemberTbKey = memberTbKey }, tx);
            tx.Commit();
            return rows > 0;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateLabelNameAsync(int labelTbKey, int memberTbKey, string labelName)
    {
        const string sql = "UPDATE dbo.UDLabel SET LabelName = @LabelName WHERE tbKey = @TbKey AND Member_tbKey = @MemberTbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { TbKey = labelTbKey, MemberTbKey = memberTbKey, LabelName = labelName }) > 0;
    }

    public async Task<string> GetLabelNamesDisplayAsync(int trackerTbKey, int memberTbKey)
    {
        var labels = await GetLabelsByTrackerAsync(trackerTbKey, memberTbKey);
        return string.Join(", ", labels.Select(l => l.LabelName));
    }
}
