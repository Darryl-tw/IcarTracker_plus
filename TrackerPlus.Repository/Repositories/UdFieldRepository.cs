using Dapper;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Models;
using TrackerPlus.Repository.Infrastructure;

namespace TrackerPlus.Repository.Repositories;

public class UdFieldRepository : IUdFieldRepository
{
    private readonly IDbConnectionFactory _db;

    public UdFieldRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<UdField>> GetByMemberAsync(int memberTbKey)
    {
        const string sql = @"SELECT tbKey AS TbKey, Member_tbKey AS MemberTbKey,
            RTRIM(FName) AS FName, FSort FROM dbo.UDFields
            WHERE Member_tbKey = @MemberTbKey ORDER BY FSort";
        using var conn = _db.CreateMainConnection();
        return await conn.QueryAsync<UdField>(sql, new { MemberTbKey = memberTbKey });
    }

    public async Task EnsureDefaultsAsync(int memberTbKey)
    {
        const string countSql = "SELECT COUNT(*) FROM dbo.UDFields WHERE Member_tbKey = @MemberTbKey";
        using var conn = _db.CreateMainConnection();
        var count = await conn.ExecuteScalarAsync<int>(countSql, new { MemberTbKey = memberTbKey });
        if (count > 0) return;

        var defaults = new[] { ("名稱", 1), ("編號", 2), ("電話", 3) };
        foreach (var (name, sort) in defaults)
        {
            await conn.ExecuteAsync(
                "INSERT INTO dbo.UDFields (Member_tbKey, FName, FSort) VALUES (@MemberTbKey, @FName, @FSort)",
                new { MemberTbKey = memberTbKey, FName = name, FSort = sort });
        }
    }

    public async Task<bool> UpdateAsync(int tbKey, int memberTbKey, string fName)
    {
        const string sql = "UPDATE dbo.UDFields SET FName = @FName WHERE tbKey = @TbKey AND Member_tbKey = @MemberTbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { TbKey = tbKey, MemberTbKey = memberTbKey, FName = fName }) > 0;
    }
}
