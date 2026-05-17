using Dapper;
using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Models;
using TrackerPlus.Repository.Infrastructure;
using TrackerPlus.Repository.Mapping;

namespace TrackerPlus.Repository.Repositories;

public class MemberRepository : IMemberRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<MemberRepository> _logger;

    public MemberRepository(IDbConnectionFactory db, ILogger<MemberRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Member?> GetByIdAsync(int tbKey)
    {
        var sql = $"{MemberMapper.SelectSql} WHERE m.tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        var row = await conn.QuerySingleOrDefaultAsync<MemberDbRow>(sql, new { TbKey = tbKey });
        return row == null ? null : MemberMapper.ToModel(row);
    }

    public async Task<Member?> GetByLoginIdAsync(string loginId)
    {
        var sql = $"{MemberMapper.SelectSql} WHERE RTRIM(m.ID) = @ID";
        using var conn = _db.CreateMainConnection();
        var row = await conn.QuerySingleOrDefaultAsync<MemberDbRow>(sql, new { ID = loginId.Trim() });
        return row == null ? null : MemberMapper.ToModel(row);
    }

    public async Task<Member?> GetByEmailAsync(string email)
    {
        var sql = $"{MemberMapper.SelectSql} WHERE RTRIM(m.EMail) = @Email";
        using var conn = _db.CreateMainConnection();
        var row = await conn.QuerySingleOrDefaultAsync<MemberDbRow>(sql, new { Email = email.Trim() });
        return row == null ? null : MemberMapper.ToModel(row);
    }

    public async Task<PagedResult<Member>> GetPagedAsync(QueryFilter filter)
    {
        var offset = (filter.PageIndex - 1) * filter.PageSize;
        var where = BuildWhereClause(filter);
        var countSql = $"SELECT COUNT(*) FROM dbo.Member m {where}";
        var dataSql = $@"{MemberMapper.SelectSql} {where}
            ORDER BY m.tbKey DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var param = new { filter.Keyword, filter.Status, Offset = offset, filter.PageSize };
        using var conn = _db.CreateMainConnection();
        var total = await conn.ExecuteScalarAsync<int>(countSql, param);
        var rows = await conn.QueryAsync<MemberDbRow>(dataSql, param);

        return new PagedResult<Member>
        {
            Items = rows.Select(MemberMapper.ToModel),
            TotalCount = total,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize
        };
    }

    public async Task<IEnumerable<Member>> GetAllAsync()
    {
        var sql = $"{MemberMapper.SelectSql} ORDER BY m.CDate DESC";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.QueryAsync<MemberDbRow>(sql);
        return rows.Select(MemberMapper.ToModel);
    }

    public async Task<int> CreateAsync(Member member)
    {
        const string sql = @"INSERT INTO dbo.Member
            (OBM_tbKey, ID, EMail, Password, CName, MemberStatus, UserLanguage, Unit, TimeZone_tbkey, Tel, Addr, issendemail, ispush, CDate)
            VALUES
            (@OBM_TbKey, @ID, @Email, @Password, @CName, @MemberStatus, @UserLanguage, @UserUnit, @TimeZone_tbkey, @Tel, @Addr, @IsSendEmail, @IsPush, @CDate);
            SELECT CAST(SCOPE_IDENTITY() AS int);";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            member.OBM_TbKey,
            member.ID,
            Email = member.Email,
            member.Password,
            member.CName,
            member.MemberStatus,
            member.UserLanguage,
            UserUnit = member.UserUnit,
            TimeZone_tbkey = 29,
            member.Tel,
            member.Addr,
            IsSendEmail = member.IsSendEmail,
            IsPush = member.IsPush,
            member.CDate
        });
    }

    public async Task<bool> UpdateAsync(Member member)
    {
        const string sql = @"UPDATE dbo.Member SET
            CName = @CName, EMail = @Email, Tel = @Tel, Addr = @Addr,
            UserLanguage = @UserLanguage, Unit = @UserUnit,
            issendemail = @IsSendEmail, ispush = @IsPush
            WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.ExecuteAsync(sql, new
        {
            member.TbKey,
            member.CName,
            Email = member.Email,
            member.Tel,
            member.Addr,
            member.UserLanguage,
            UserUnit = member.UserUnit,
            IsSendEmail = member.IsSendEmail,
            IsPush = member.IsPush
        });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int tbKey)
    {
        const string sql = "DELETE FROM dbo.Member WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { TbKey = tbKey }) > 0;
    }

    public async Task<bool> UpdateStatusAsync(int tbKey, string status)
    {
        const string sql = "UPDATE dbo.Member SET MemberStatus = @Status WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { TbKey = tbKey, Status = status }) > 0;
    }

    public async Task<bool> UpdateLanguageAsync(int tbKey, string language)
    {
        const string sql = "UPDATE dbo.Member SET UserLanguage = @Language WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { TbKey = tbKey, Language = language }) > 0;
    }

    public async Task<bool> UpdatePasswordAsync(int tbKey, string hashedPassword)
    {
        const string sql = "UPDATE dbo.Member SET Password = @Password WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { TbKey = tbKey, Password = hashedPassword }) > 0;
    }

    public async Task<int> GetCountAsync(QueryFilter filter)
    {
        var where = BuildWhereClause(filter);
        var sql = $"SELECT COUNT(*) FROM dbo.Member m {where}";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { filter.Keyword, filter.Status });
    }

    public async Task<bool> ExistsAsync(string loginId)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.Member WHERE RTRIM(ID) = @ID";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { ID = loginId.Trim() }) > 0;
    }

    private static string BuildWhereClause(QueryFilter filter)
    {
        var conditions = new List<string>();
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            conditions.Add("(RTRIM(m.ID) LIKE '%' + @Keyword + '%' OR RTRIM(m.CName) LIKE '%' + @Keyword + '%' OR RTRIM(m.EMail) LIKE '%' + @Keyword + '%')");
        if (!string.IsNullOrWhiteSpace(filter.Status))
            conditions.Add("m.MemberStatus = @Status");
        return conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
    }
}
